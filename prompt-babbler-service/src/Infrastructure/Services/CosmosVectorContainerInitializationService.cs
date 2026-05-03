using System.Collections.ObjectModel;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PromptBabbler.Infrastructure.Services;

// Workaround: The Aspire Cosmos DB hosting integration's AddContainer() API does not support
// configuring vector embedding policies or vector indexes on containers. This means the
// local emulator creates the 'babbles' container with a default indexing policy, which does
// not include the quantizedFlat vector index required for VectorDistance() queries.
//
// This hosted service runs only in Development and recreates the 'babbles' container with
// the correct vector search configuration if it is missing.
//
// Upstream Aspire issue: https://github.com/microsoft/aspire/issues/14384
// Tracking issue:        https://github.com/PlagueHO/prompt-babbler/issues/122
public sealed class CosmosVectorContainerInitializationService(
    CosmosClient cosmosClient,
    IHostEnvironment environment,
    ILogger<CosmosVectorContainerInitializationService> logger) : IHostedService
{
    private const string VectorPath = "/contentVector";
    private const int VectorDimensions = 1536;

    // Delays in seconds between successive retry attempts when Cosmos DB returns 503.
    // Total back-off budget: 2 + 4 + 8 + 16 + 30 = 60 seconds.
    private static readonly int[] RetryDelaySeconds = [2, 4, 8, 16, 30];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        logger.LogInformation(
            "Checking vector index configuration on Cosmos DB container '{Container}'...",
            CosmosBabbleRepository.ContainerName);

        for (var attempt = 0; attempt <= RetryDelaySeconds.Length; attempt++)
        {
            try
            {
                await EnsureVectorContainerAsync(cancellationToken);
                return;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < RetryDelaySeconds.Length)
            {
                var delay = RetryDelaySeconds[attempt];
                logger.LogWarning(
                    "Cosmos DB returned 503 on attempt {Attempt}. Retrying in {Delay}s...",
                    attempt + 1,
                    delay);
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureVectorContainerAsync(CancellationToken cancellationToken)
    {
        var database = cosmosClient.GetDatabase(CosmosBabbleRepository.DatabaseName);
        var container = database.GetContainer(CosmosBabbleRepository.ContainerName);

        ContainerProperties? existingProperties = null;
        try
        {
            var response = await container.ReadContainerAsync(cancellationToken: cancellationToken);
            existingProperties = response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Container does not exist yet — will be created below.
        }

        if (existingProperties is not null &&
            existingProperties.VectorEmbeddingPolicy?.Embeddings is { Count: > 0 })
        {
            logger.LogInformation(
                "Container '{Container}' already has a vector embedding policy configured. Skipping initialization.",
                CosmosBabbleRepository.ContainerName);
            return;
        }

        if (existingProperties is not null)
        {
            logger.LogWarning(
                "Container '{Container}' exists without a vector embedding policy. " +
                "Deleting and recreating with vector search configuration for local development. " +
                "Workaround for: https://github.com/microsoft/aspire/issues/14384",
                CosmosBabbleRepository.ContainerName);

            await container.DeleteContainerAsync(cancellationToken: cancellationToken);
        }
        else
        {
            logger.LogInformation(
                "Container '{Container}' does not exist. Creating with vector search configuration.",
                CosmosBabbleRepository.ContainerName);
        }

        var properties = BuildVectorContainerProperties();
        await database.CreateContainerAsync(properties, cancellationToken: cancellationToken);

        logger.LogInformation(
            "Container '{Container}' created with quantizedFlat vector index on '{Path}' ({Dimensions}d).",
            CosmosBabbleRepository.ContainerName,
            VectorPath,
            VectorDimensions);
    }

    private static ContainerProperties BuildVectorContainerProperties()
    {
        var properties = new ContainerProperties(
            CosmosBabbleRepository.ContainerName,
            partitionKeyPath: "/userId")
        {
            VectorEmbeddingPolicy = new VectorEmbeddingPolicy(
                new Collection<Embedding>
                {
                    new()
                    {
                        Path = VectorPath,
                        DataType = VectorDataType.Float32,
                        DistanceFunction = DistanceFunction.Cosine,
                        Dimensions = VectorDimensions,
                    },
                }),
        };

        properties.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
        properties.IndexingPolicy.Automatic = true;
        properties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
        properties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = $"{VectorPath}/*" });
        properties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/\"_etag\"/?" });
        properties.IndexingPolicy.VectorIndexes.Add(new VectorIndexPath
        {
            Path = VectorPath,
            Type = VectorIndexType.QuantizedFlat,
        });

        return properties;
    }
}
