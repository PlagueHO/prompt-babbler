using System.Collections.ObjectModel;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PromptBabbler.Infrastructure.Services;

// Workaround: The Aspire Cosmos DB hosting integration's AddContainer() API does not support
// configuring vector embedding policies or vector indexes on containers. This means the
// local emulator creates the 'babbles' container with a default indexing policy, which does
// not include the DiskANN vector index supported by the vNext emulator.
//
// This Development-only service creates the vector-enabled container or recreates an
// incompatible empty container. Existing documents are never deleted automatically.
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

        if (existingProperties is not null && HasCompatibleVectorPolicy(existingProperties))
        {
            if (existingProperties.VectorEmbeddingPolicy is null)
            {
                logger.LogWarning(
                    "Container '{Container}' has the expected DiskANN index and partition key, but the vNext emulator " +
                    "did not return embedding policy metadata. Embedding dimensions, data type, and distance function cannot be verified from this response.",
                    CosmosBabbleRepository.ContainerName);
            }

            logger.LogInformation(
                "Container '{Container}' already has the required DiskANN vector index. Skipping initialization.",
                CosmosBabbleRepository.ContainerName);
            return;
        }

        if (existingProperties is not null)
        {
            if (await ContainsDocumentsAsync(container, cancellationToken))
            {
                const string recoveryMessage =
                    "Local Cosmos DB container 'prompt-babbler/babbles' contains documents and has an incompatible vector policy. " +
                    "No documents were deleted. Stop application writers, back up and verify the documents through the local " +
                    "emulator's Data Explorer, then explicitly delete only this local container if you accept the data loss. " +
                    "Restart the app to create the DiskANN container and restore the backup. " +
                    "See docs/quickstart-local.md#recover-an-incompatible-local-babbles-container. " +
                    "The vNext emulator does not apply in-place container policy updates.";
                logger.LogError("{RecoveryMessage}", recoveryMessage);
                throw new InvalidOperationException(recoveryMessage);
            }

            logger.LogWarning(
                "Container '{Container}' has an incompatible vector policy and is empty. " +
                "Recreating with DiskANN vector search configuration for local development. " +
                "Workaround for: https://github.com/microsoft/aspire/issues/14384",
                CosmosBabbleRepository.ContainerName);

            cancellationToken.ThrowIfCancellationRequested();
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
            "Container '{Container}' created with DiskANN vector index on '{Path}' ({Dimensions}d).",
            CosmosBabbleRepository.ContainerName,
            VectorPath,
            VectorDimensions);
    }

    private static bool HasCompatibleVectorPolicy(ContainerProperties properties) =>
        properties.PartitionKeyPath == "/userId" &&
        properties.IndexingPolicy.VectorIndexes is { Count: 1 } indexes &&
        indexes[0].Path == VectorPath &&
        indexes[0].Type == VectorIndexType.DiskANN &&
        // vNext EN20261008 omits vectorEmbeddingPolicy on reads, even after accepting it on create.
        (properties.VectorEmbeddingPolicy is null ||
         (properties.VectorEmbeddingPolicy.Embeddings is { Count: 1 } embeddings &&
          embeddings[0].Path == VectorPath &&
          embeddings[0].Dimensions == VectorDimensions &&
          embeddings[0].DataType == VectorDataType.Float32 &&
          embeddings[0].DistanceFunction == DistanceFunction.Cosine));

    private static async Task<bool> ContainsDocumentsAsync(Container container, CancellationToken cancellationToken)
    {
        using var iterator = container.GetItemQueryIterator<int>(
            new QueryDefinition("SELECT TOP 1 VALUE 1 FROM c"));

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            if (response.Count > 0)
            {
                return true;
            }
        }

        return false;
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
            Type = VectorIndexType.DiskANN,
        });

        return properties;
    }
}
