using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class CosmosUserRepository : IUserRepository
{
    public const string DatabaseName = "prompt-babbler";
    public const string ContainerName = "users";

    private readonly Container _container;
    private readonly ILogger<CosmosUserRepository> _logger;

    public CosmosUserRepository(CosmosClient cosmosClient, ILogger<CosmosUserRepository> logger)
    {
        _container = cosmosClient.GetContainer(DatabaseName, ContainerName);
        _logger = logger;
    }

    public async Task<UserProfile?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<UserProfile>(
                userId,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<UserProfile> UpsertAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        var response = await _container.UpsertItemAsync(
            profile,
            new PartitionKey(profile.UserId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Upserted user profile for user {UserId}", profile.UserId);

        return response.Resource;
    }
}
