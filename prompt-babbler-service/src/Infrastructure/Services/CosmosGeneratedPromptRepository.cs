using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class CosmosGeneratedPromptRepository : IGeneratedPromptRepository
{
    public const string DatabaseName = "prompt-babbler";
    public const string ContainerName = "generated-prompts";

    private readonly Container _container;
    private readonly ILogger<CosmosGeneratedPromptRepository> _logger;

    public CosmosGeneratedPromptRepository(CosmosClient cosmosClient, ILogger<CosmosGeneratedPromptRepository> logger)
    {
        _container = cosmosClient.GetContainer(DatabaseName, ContainerName);
        _logger = logger;
    }

    public async Task<(IReadOnlyList<GeneratedPrompt> Items, string? ContinuationToken)> GetByBabbleAsync(
        string babbleId,
        string? continuationToken = null,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.babbleId = @babbleId ORDER BY c.generatedAt DESC")
            .WithParameter("@babbleId", babbleId);

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(babbleId),
            MaxItemCount = pageSize,
        };

        var results = new List<GeneratedPrompt>();
        using var iterator = _container.GetItemQueryIterator<GeneratedPrompt>(query, continuationToken, options);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
            return (results.AsReadOnly(), response.ContinuationToken);
        }

        return (results.AsReadOnly(), null);
    }

    public async Task<GeneratedPrompt?> GetByIdAsync(string babbleId, string promptId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<GeneratedPrompt>(
                promptId,
                new PartitionKey(babbleId),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<GeneratedPrompt> CreateAsync(GeneratedPrompt prompt, CancellationToken cancellationToken = default)
    {
        var response = await _container.CreateItemAsync(
            prompt,
            new PartitionKey(prompt.BabbleId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created generated prompt {PromptId} for babble {BabbleId}", prompt.Id, prompt.BabbleId);

        return response.Resource;
    }

    public async Task DeleteAsync(string babbleId, string promptId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(babbleId, promptId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"Generated prompt '{promptId}' not found for babble '{babbleId}'.");
        }

        await _container.DeleteItemAsync<GeneratedPrompt>(
            promptId,
            new PartitionKey(babbleId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Deleted generated prompt {PromptId} for babble {BabbleId}", promptId, babbleId);
    }

    public async Task DeleteByBabbleAsync(string babbleId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT c.id FROM c WHERE c.babbleId = @babbleId")
            .WithParameter("@babbleId", babbleId);

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(babbleId),
        };

        using var iterator = _container.GetItemQueryIterator<GeneratedPrompt>(query, requestOptions: options);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                await _container.DeleteItemAsync<GeneratedPrompt>(
                    item.Id,
                    new PartitionKey(babbleId),
                    cancellationToken: cancellationToken);
            }
        }

        _logger.LogInformation("Deleted all generated prompts for babble {BabbleId}", babbleId);
    }
}
