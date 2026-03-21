using System.Net;
using System.Text;
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
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null,
        bool? isPinned = null,
        CancellationToken cancellationToken = default)
    {
        var queryText = new StringBuilder("SELECT * FROM c WHERE c.userId = @userId");

        if (!string.IsNullOrEmpty(search))
        {
            queryText.Append(" AND CONTAINS(LOWER(c.title), @search)");
        }

        if (isPinned.HasValue)
        {
            queryText.Append(" AND c.isPinned = @isPinned");
        }

        var orderByField = sortBy == "title" ? "c.title" : "c.createdAt";
        var orderByDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        queryText.Append($" ORDER BY {orderByField} {orderByDirection}");

        var queryDefinition = new QueryDefinition(queryText.ToString())
            .WithParameter("@userId", userId);

        if (!string.IsNullOrEmpty(search))
        {
            queryDefinition = queryDefinition.WithParameter("@search", search.ToLowerInvariant());
        }

        if (isPinned.HasValue)
        {
            queryDefinition = queryDefinition.WithParameter("@isPinned", isPinned.Value);
        }

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
            MaxItemCount = pageSize,
        };

        var results = new List<Babble>();
        using var iterator = _container.GetItemQueryIterator<Babble>(queryDefinition, continuationToken, options);

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

    public async Task<Babble> SetPinAsync(string userId, string babbleId, bool isPinned, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(userId, babbleId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"Babble '{babbleId}' not found for user '{userId}'.");
        }

        var updated = existing with
        {
            IsPinned = isPinned,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var response = await _container.ReplaceItemAsync(
            updated,
            updated.Id,
            new PartitionKey(userId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Set pin {IsPinned} on babble {BabbleId} for user {UserId}", isPinned, babbleId, userId);

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
