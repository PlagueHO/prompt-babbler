using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class CosmosBabbleRepository : IBabbleRepository
{
    public const string DatabaseName = "prompt-babbler";
    public const string ContainerName = "babbles";

    private readonly Container _container;
    private readonly ILogger<CosmosBabbleRepository> _logger;

    public CosmosBabbleRepository(CosmosClient cosmosClient, ILogger<CosmosBabbleRepository> logger)
    {
        _container = cosmosClient.GetContainer(DatabaseName, ContainerName);
        _logger = logger;
    }

    public async Task<(IReadOnlyList<Babble> Items, string? ContinuationToken)> GetByUserAsync(
        string userId,
        string? continuationToken = null,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId ORDER BY c.createdAt DESC")
            .WithParameter("@userId", userId);

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
            MaxItemCount = pageSize,
        };

        var results = new List<Babble>();
        using var iterator = _container.GetItemQueryIterator<Babble>(query, continuationToken, options);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
            return (results.AsReadOnly(), response.ContinuationToken);
        }

        return (results.AsReadOnly(), null);
    }

    public async Task<Babble?> GetByIdAsync(string userId, string babbleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<Babble>(
                babbleId,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Babble> CreateAsync(Babble babble, CancellationToken cancellationToken = default)
    {
        var response = await _container.CreateItemAsync(
            babble,
            new PartitionKey(babble.UserId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created babble {BabbleId} for user {UserId}", babble.Id, babble.UserId);

        return response.Resource;
    }

    public async Task<Babble> UpdateAsync(Babble babble, CancellationToken cancellationToken = default)
    {
        var response = await _container.ReplaceItemAsync(
            babble,
            babble.Id,
            new PartitionKey(babble.UserId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Updated babble {BabbleId} for user {UserId}", babble.Id, babble.UserId);

        return response.Resource;
    }

    public async Task DeleteAsync(string userId, string babbleId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(userId, babbleId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"Babble '{babbleId}' not found for user '{userId}'.");
        }

        await _container.DeleteItemAsync<Babble>(
            babbleId,
            new PartitionKey(userId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Deleted babble {BabbleId} for user {UserId}", babbleId, userId);
    }
}
