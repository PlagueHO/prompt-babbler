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
public sealed class CosmosGeneratedPromptRepositoryTests
{
    private readonly Container _container = Substitute.For<Container>();
    private readonly CosmosClient _cosmosClient = Substitute.For<CosmosClient>();
    private readonly ILogger<CosmosGeneratedPromptRepository> _logger = Substitute.For<ILogger<CosmosGeneratedPromptRepository>>();
    private readonly CosmosGeneratedPromptRepository _repository;

    public CosmosGeneratedPromptRepositoryTests()
    {
        _cosmosClient.GetContainer(
            CosmosGeneratedPromptRepository.DatabaseName,
            CosmosGeneratedPromptRepository.ContainerName).Returns(_container);

        _repository = new CosmosGeneratedPromptRepository(_cosmosClient, _logger);
    }

    private static GeneratedPrompt CreatePrompt(
        string id = "test-prompt-id",
        string babbleId = "test-babble-id",
        string userId = "test-user-id") => new()
        {
            Id = id,
            BabbleId = babbleId,
            UserId = userId,
            TemplateId = "template-1",
            TemplateName = "Test Template",
            PromptText = "Generated prompt text.",
            GeneratedAt = DateTimeOffset.UtcNow,
        };

    // ---- GetByIdAsync ----

    [TestMethod]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _container.ReadItemAsync<GeneratedPrompt>(
            "missing",
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        var result = await _repository.GetByIdAsync("test-babble-id", "missing");

        result.Should().BeNull();
    }

    // ---- CreateAsync ----

    [TestMethod]
    public async Task CreateAsync_ValidPrompt_CallsCreateItemAsync()
    {
        var prompt = CreatePrompt();
        var response = Substitute.For<ItemResponse<GeneratedPrompt>>();
        response.Resource.Returns(prompt);

        _container.CreateItemAsync(
            prompt,
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _repository.CreateAsync(prompt);

        result.Should().Be(prompt);
        await _container.Received(1).CreateItemAsync(
            prompt,
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    // ---- DeleteAsync ----

    [TestMethod]
    public async Task DeleteAsync_NonExistentPrompt_ThrowsInvalidOperationException()
    {
        _container.ReadItemAsync<GeneratedPrompt>(
            "missing",
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        var act = () => _repository.DeleteAsync("test-babble-id", "missing");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [TestMethod]
    public async Task DeleteAsync_NonExistentPrompt_DoesNotCallDeleteItemAsync()
    {
        _container.ReadItemAsync<GeneratedPrompt>(
            "missing",
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        try { await _repository.DeleteAsync("test-babble-id", "missing"); }
        catch (InvalidOperationException) { }

        await _container.DidNotReceive().DeleteItemAsync<GeneratedPrompt>(
            Arg.Any<string>(),
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    // ---- Constants ----

    [TestMethod]
    public void DatabaseName_HasExpectedValue()
    {
        CosmosGeneratedPromptRepository.DatabaseName.Should().Be("prompt-babbler");
    }

    [TestMethod]
    public void ContainerName_HasExpectedValue()
    {
        CosmosGeneratedPromptRepository.ContainerName.Should().Be("generated-prompts");
    }
}
