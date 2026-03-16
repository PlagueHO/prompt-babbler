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
public sealed class CosmosBabbleRepositoryTests
{
    private readonly Container _container = Substitute.For<Container>();
    private readonly CosmosClient _cosmosClient = Substitute.For<CosmosClient>();
    private readonly ILogger<CosmosBabbleRepository> _logger = Substitute.For<ILogger<CosmosBabbleRepository>>();
    private readonly CosmosBabbleRepository _repository;

    public CosmosBabbleRepositoryTests()
    {
        _cosmosClient.GetContainer(
            CosmosBabbleRepository.DatabaseName,
            CosmosBabbleRepository.ContainerName).Returns(_container);

        _repository = new CosmosBabbleRepository(_cosmosClient, _logger);
    }

    private static Babble CreateBabble(
        string id = "test-babble-id",
        string userId = "test-user-id") => new()
        {
            Id = id,
            UserId = userId,
            Title = "Test Babble",
            Text = "This is a test babble transcription.",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ---- GetByIdAsync ----

    [TestMethod]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _container.ReadItemAsync<Babble>(
            "missing",
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        var result = await _repository.GetByIdAsync("test-user-id", "missing");

        result.Should().BeNull();
    }

    // ---- CreateAsync ----

    [TestMethod]
    public async Task CreateAsync_ValidBabble_CallsCreateItemAsync()
    {
        var babble = CreateBabble();
        var response = Substitute.For<ItemResponse<Babble>>();
        response.Resource.Returns(babble);

        _container.CreateItemAsync(
            babble,
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _repository.CreateAsync(babble);

        result.Should().Be(babble);
        await _container.Received(1).CreateItemAsync(
            babble,
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    // ---- UpdateAsync ----

    [TestMethod]
    public async Task UpdateAsync_ValidBabble_CallsReplaceItemAsync()
    {
        var babble = CreateBabble();
        var response = Substitute.For<ItemResponse<Babble>>();
        response.Resource.Returns(babble);

        _container.ReplaceItemAsync(
            babble,
            babble.Id,
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _repository.UpdateAsync(babble);

        result.Should().Be(babble);
    }

    // ---- DeleteAsync ----

    [TestMethod]
    public async Task DeleteAsync_NonExistentBabble_ThrowsInvalidOperationException()
    {
        _container.ReadItemAsync<Babble>(
            "missing",
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        var act = () => _repository.DeleteAsync("test-user-id", "missing");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [TestMethod]
    public async Task DeleteAsync_NonExistentBabble_DoesNotCallDeleteItemAsync()
    {
        _container.ReadItemAsync<Babble>(
            "missing",
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        try { await _repository.DeleteAsync("test-user-id", "missing"); }
        catch (InvalidOperationException) { }

        await _container.DidNotReceive().DeleteItemAsync<Babble>(
            Arg.Any<string>(),
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    // ---- Constants ----

    [TestMethod]
    public void DatabaseName_HasExpectedValue()
    {
        CosmosBabbleRepository.DatabaseName.Should().Be("prompt-babbler");
    }

    [TestMethod]
    public void ContainerName_HasExpectedValue()
    {
        CosmosBabbleRepository.ContainerName.Should().Be("babbles");
    }
}
