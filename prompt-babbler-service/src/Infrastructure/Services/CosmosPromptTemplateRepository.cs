using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Constants;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class CosmosPromptTemplateRepository : IPromptTemplateRepository
{
    public const string BuiltInUserId = "_builtin";
    public const string DatabaseName = "prompt-babbler";
    public const string ContainerName = "prompt-templates";

    private readonly Container _container;
    private readonly ILogger<CosmosPromptTemplateRepository> _logger;

    public CosmosPromptTemplateRepository(CosmosClient cosmosClient, ILogger<CosmosPromptTemplateRepository> logger)
    {
        _container = cosmosClient.GetContainer(DatabaseName, ContainerName);
        _logger = logger;
    }

    public async Task<int> CountByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.userId = @userId")
            .WithParameter("@userId", userId);

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
        };

        using var iterator = _container.GetItemQueryIterator<int>(query, requestOptions: options);
        if (!iterator.HasMoreResults)
        {
            return 0;
        }

        var response = await iterator.ReadNextAsync(cancellationToken);
        return response.Resource.FirstOrDefault();
    }

    public async Task<IReadOnlyList<PromptTemplate>> GetBuiltInTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await QueryByPartitionKeyAsync(BuiltInUserId, cancellationToken);
    }

    public async Task<(IReadOnlyList<PromptTemplate> Items, string? ContinuationToken)> ListTemplatesAsync(
        string userId,
        string? continuationToken = null,
        int pageSize = 20,
        string? search = null,
        string? tag = null,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var isAnonymous = string.Equals(userId, UserIds.Anonymous, StringComparison.Ordinal);
        var userFilter = isAnonymous
            ? "c.userId = @builtInUserId"
            : "(c.userId = @builtInUserId OR c.userId = @userId)";

        var queryText = new StringBuilder($"SELECT * FROM c WHERE {userFilter}");

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryText.Append(" AND CONTAINS(LOWER(c.name), @search)");
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            queryText.Append(" AND EXISTS(SELECT VALUE t FROM t IN c.tags WHERE LOWER(t) = @tag)");
        }

        var orderByField = sortBy == "name" ? "c.name" : "c.updatedAt";
        var orderByDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        queryText.Append($" ORDER BY {orderByField} {orderByDirection}");

        var queryDefinition = new QueryDefinition(queryText.ToString())
            .WithParameter("@builtInUserId", BuiltInUserId);

        if (!isAnonymous)
        {
            queryDefinition = queryDefinition.WithParameter("@userId", userId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryDefinition = queryDefinition.WithParameter("@search", search.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            queryDefinition = queryDefinition.WithParameter("@tag", tag.Trim().ToLowerInvariant());
        }

        var options = new QueryRequestOptions
        {
            MaxItemCount = pageSize,
        };

        var results = new List<PromptTemplate>();
        using var iterator = _container.GetItemQueryIterator<PromptTemplate>(queryDefinition, continuationToken, options);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
            return (results.AsReadOnly(), response.ContinuationToken);
        }

        return (results.AsReadOnly(), null);
    }

    public async Task<IReadOnlyList<PromptTemplate>> GetUserTemplatesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await QueryByPartitionKeyAsync(userId, cancellationToken);
    }

    public async Task<PromptTemplate?> GetByIdAsync(string userId, string templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<PromptTemplate>(
                templateId,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<PromptTemplate> CreateAsync(PromptTemplate template, CancellationToken cancellationToken = default)
    {
        var response = await _container.CreateItemAsync(
            template,
            new PartitionKey(template.UserId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created prompt template {TemplateId} for user {UserId}", template.Id, template.UserId);

        return response.Resource;
    }

    public async Task<PromptTemplate> UpdateAsync(PromptTemplate template, CancellationToken cancellationToken = default)
    {
        if (template.IsBuiltIn)
        {
            throw new InvalidOperationException("Built-in templates cannot be modified.");
        }

        var response = await _container.ReplaceItemAsync(
            template,
            template.Id,
            new PartitionKey(template.UserId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Updated prompt template {TemplateId} for user {UserId}", template.Id, template.UserId);

        return response.Resource;
    }

    public async Task DeleteAsync(string userId, string templateId, CancellationToken cancellationToken = default)
    {
        // Verify the template exists and is not built-in before deleting
        var existing = await GetByIdAsync(userId, templateId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"Template '{templateId}' not found for user '{userId}'.");
        }

        if (existing.IsBuiltIn)
        {
            throw new InvalidOperationException("Built-in templates cannot be deleted.");
        }

        await _container.DeleteItemAsync<PromptTemplate>(
            templateId,
            new PartitionKey(userId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Deleted prompt template {TemplateId} for user {UserId}", templateId, userId);
    }

    public async Task UpsertAsync(PromptTemplate template, CancellationToken cancellationToken = default)
    {
        await _container.UpsertItemAsync(
            template,
            new PartitionKey(template.UserId),
            cancellationToken: cancellationToken);

        _logger.LogDebug("Upserted prompt template {TemplateId} for user {UserId}", template.Id, template.UserId);
    }

    private async Task<IReadOnlyList<PromptTemplate>> QueryByPartitionKeyAsync(string userId, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
            .WithParameter("@userId", userId);

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
        };

        var results = new List<PromptTemplate>();
        using var iterator = _container.GetItemQueryIterator<PromptTemplate>(query, requestOptions: options);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }
}
