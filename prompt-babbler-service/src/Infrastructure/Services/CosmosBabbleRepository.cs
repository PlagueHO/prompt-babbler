using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class CosmosBabbleRepository : IBabbleRepository
{
    public const string DatabaseName = "prompt-babbler";
    public const string ContainerName = "babbles";

    /// <summary>
    /// Minimum cosine similarity score (VectorDistance) a result must meet to be included in
    /// semantic search results. Range: 0.0 (no similarity) to 1.0 (identical).
    /// Raise this value to tighten relevance; lower it to broaden results.
    /// </summary>
    public const double MinimumSimilarityScore = 0.65;

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
        string[]? searchWords = null;

        if (!string.IsNullOrEmpty(search))
        {
            searchWords = search.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (var i = 0; i < searchWords.Length; i++)
            {
                queryText.Append($" AND (CONTAINS(LOWER(c.title), @s{i}) OR CONTAINS(LOWER(c.text), @s{i}))");
            }
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

        if (searchWords is not null)
        {
            for (var i = 0; i < searchWords.Length; i++)
            {
                queryDefinition = queryDefinition.WithParameter($"@s{i}", searchWords[i]);
            }
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

    public async Task<IReadOnlyList<BabbleSearchResult>> SearchByVectorAsync(
        string userId,
        ReadOnlyMemory<float> vector,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var queryText = @"
            SELECT TOP @topN c.id, c.userId, c.title, c.text, c.createdAt, c.tags, c.updatedAt, c.isPinned,
                VectorDistance(c.contentVector, @embedding) AS SimilarityScore
            FROM c WHERE c.userId = @userId
            ORDER BY VectorDistance(c.contentVector, @embedding)";

        var queryDefinition = new QueryDefinition(queryText)
            .WithParameter("@topN", topN)
            .WithParameter("@userId", userId)
            .WithParameter("@embedding", vector.ToArray());

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
        };

        var results = new List<BabbleSearchResult>();
        using var iterator = _container.GetItemQueryIterator<VectorSearchResultItem>(queryDefinition, null, options);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                // VectorDistance returns cosine similarity (higher = more similar).
                // Filtering post-query avoids Cosmos DB brute-force requirement for VectorDistance in WHERE.
                if (item.SimilarityScore < MinimumSimilarityScore)
                {
                    continue;
                }

                results.Add(new BabbleSearchResult(
                    new Babble
                    {
                        Id = item.Id,
                        UserId = item.UserId,
                        Title = item.Title,
                        Text = item.Text,
                        CreatedAt = item.CreatedAt,
                        Tags = item.Tags,
                        UpdatedAt = item.UpdatedAt,
                        IsPinned = item.IsPinned,
                    },
                    item.SimilarityScore));
            }
        }

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<BabbleSearchResult>> SearchByKeywordAsync(
        string userId,
        string query,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var words = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length == 0)
        {
            return Array.Empty<BabbleSearchResult>();
        }

        var wordClauses = words.Select((_, i) =>
            $"(CONTAINS(LOWER(c.title), @w{i}) OR CONTAINS(LOWER(c.text), @w{i}))");

        var queryText =
            $"SELECT TOP @topN * FROM c " +
            $"WHERE c.userId = @userId AND {string.Join(" AND ", wordClauses)}";

        var queryDefinition = new QueryDefinition(queryText)
            .WithParameter("@topN", topN)
            .WithParameter("@userId", userId);

        for (var i = 0; i < words.Length; i++)
        {
            queryDefinition = queryDefinition.WithParameter($"@w{i}", words[i]);
        }

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
        };

        var results = new List<BabbleSearchResult>();
        using var iterator = _container.GetItemQueryIterator<Babble>(queryDefinition, null, options);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                results.Add(new BabbleSearchResult(item, SimilarityScore: 1.0));
            }
        }

        return results.AsReadOnly();
    }

    // NOTE: VectorSearchResultItem is a Cosmos DB projection DTO used to deserialize
    // VectorDistance query results. Its properties mirror those of the Babble domain model
    // (excluding ContentVector). If Babble gains or renames properties that are stored in
    // Cosmos, this record must be updated to stay in sync.
    private sealed record VectorSearchResultItem
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = "";

        [JsonPropertyName("userId")]
        public string UserId { get; init; } = "";

        [JsonPropertyName("title")]
        public string Title { get; init; } = "";

        [JsonPropertyName("text")]
        public string Text { get; init; } = "";

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; init; }

        [JsonPropertyName("tags")]
        public IReadOnlyList<string>? Tags { get; init; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; init; }

        [JsonPropertyName("isPinned")]
        public bool IsPinned { get; init; }

        [JsonPropertyName("SimilarityScore")]
        public double SimilarityScore { get; init; }
    }
}
