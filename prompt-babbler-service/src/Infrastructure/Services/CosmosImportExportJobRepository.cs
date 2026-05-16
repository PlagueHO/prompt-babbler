using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class CosmosImportExportJobRepository : IImportExportJobRepository
{
    public const string DatabaseName = "prompt-babbler";
    public const string ContainerName = "import-export-jobs";

    private readonly Container _container;
    private readonly ILogger<CosmosImportExportJobRepository> _logger;

    public CosmosImportExportJobRepository(CosmosClient cosmosClient, ILogger<CosmosImportExportJobRepository> logger)
    {
        _container = cosmosClient.GetContainer(DatabaseName, ContainerName);
        _logger = logger;
    }

    public async Task<ImportExportJob> CreateAsync(ImportExportJob job, CancellationToken cancellationToken = default)
    {
        var response = await _container.CreateItemAsync(
            job,
            new PartitionKey(job.UserId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created {JobType} job {JobId} for user {UserId}", job.JobType, job.Id, job.UserId);
        return response.Resource;
    }

    public async Task<ImportExportJob?> GetByIdAsync(string userId, string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<ImportExportJob>(
                jobId,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ImportExportJob> UpdateAsync(ImportExportJob job, CancellationToken cancellationToken = default)
    {
        var response = await _container.UpsertItemAsync(
            job,
            new PartitionKey(job.UserId),
            cancellationToken: cancellationToken);

        _logger.LogDebug("Updated job {JobId} for user {UserId} to status {Status}", job.Id, job.UserId, job.Status);
        return response.Resource;
    }

    public async Task<IReadOnlyList<ImportExportJob>> ListActiveByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.userId = @userId AND (c.status = @queued OR c.status = @running) ORDER BY c.createdAt DESC")
            .WithParameter("@userId", userId)
            .WithParameter("@queued", (int)JobStatus.Queued)
            .WithParameter("@running", (int)JobStatus.Running);

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
        };

        var results = new List<ImportExportJob>();
        using var iterator = _container.GetItemQueryIterator<ImportExportJob>(query, requestOptions: options);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results.AsReadOnly();
    }
}
