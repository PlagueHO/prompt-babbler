using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class CosmosUserRepositoryTests
{
    private readonly Container _container = Substitute.For<Container>();
    private readonly CosmosClient _cosmosClient = Substitute.For<CosmosClient>();
    private readonly ILogger<CosmosUserRepository> _logger = Substitute.For<ILogger<CosmosUserRepository>>();
    private readonly CosmosUserRepository _repository;

    public CosmosUserRepositoryTests()
    {
        _cosmosClient.GetContainer(
            CosmosUserRepository.DatabaseName,
            CosmosUserRepository.ContainerName).Returns(_container);

        _repository = new CosmosUserRepository(_cosmosClient, _logger);
    }

    private static UserProfile CreateProfile(
        string userId = "test-user-id") => new()
        {
            Id = userId,
            UserId = userId,
            DisplayName = "Test User",
            Email = "test@example.com",
            Settings = new UserSettings { Theme = "system", SpeechLanguage = "" },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ---- GetByIdAsync ----

    [TestMethod]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _container.ReadItemAsync<UserProfile>(
            "missing",
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        var result = await _repository.GetByIdAsync("missing");

        result.Should().BeNull();
    }

    // ---- UpsertAsync ----

    [TestMethod]
    public async Task UpsertAsync_ValidProfile_CallsUpsertItemAsync()
    {
        var profile = CreateProfile();
        var response = Substitute.For<ItemResponse<UserProfile>>();
        response.Resource.Returns(profile);

        _container.UpsertItemAsync(
            profile,
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _repository.UpsertAsync(profile);

        result.Should().Be(profile);
        await _container.Received(1).UpsertItemAsync(
            profile,
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }
}
