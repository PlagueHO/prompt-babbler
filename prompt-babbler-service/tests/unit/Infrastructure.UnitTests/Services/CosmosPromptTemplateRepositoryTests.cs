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
public sealed class CosmosPromptTemplateRepositoryTests
{
    private readonly Container _container = Substitute.For<Container>();
    private readonly CosmosClient _cosmosClient = Substitute.For<CosmosClient>();
    private readonly ILogger<CosmosPromptTemplateRepository> _logger = Substitute.For<ILogger<CosmosPromptTemplateRepository>>();
    private readonly CosmosPromptTemplateRepository _repository;

    public CosmosPromptTemplateRepositoryTests()
    {
        _cosmosClient.GetContainer(
            CosmosPromptTemplateRepository.DatabaseName,
            CosmosPromptTemplateRepository.ContainerName).Returns(_container);

        _repository = new CosmosPromptTemplateRepository(_cosmosClient, _logger);
    }

    private static PromptTemplate CreateTemplate(
        string id = "test-id",
        string userId = "_anonymous",
        bool isBuiltIn = false) => new()
        {
            Id = id,
            UserId = userId,
            Name = "Test Template",
            Description = "Test description",
            Instructions = "You are a test assistant.",
            IsBuiltIn = isBuiltIn,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ---- GetByIdAsync ----

    [TestMethod]
    [DataRow("_anonymous")]
    [DataRow("user-1")]
    public async Task ListTemplatesAsync_IncludesBuiltInAndOwnerPartitionsWithFiltersAndPaging(string userId)
    {
        var templates = new List<PromptTemplate>
        {
            CreateTemplate("builtin", "_builtin", isBuiltIn: true),
            CreateTemplate("custom", userId),
        };
        var response = Substitute.For<FeedResponse<PromptTemplate>>();
        response.GetEnumerator().Returns(_ => templates.GetEnumerator());
        response.ContinuationToken.Returns("next-page");
        var iterator = Substitute.For<FeedIterator<PromptTemplate>>();
        iterator.HasMoreResults.Returns(true);
        iterator.ReadNextAsync(Arg.Any<CancellationToken>()).Returns(response);
        QueryDefinition? capturedQuery = null;
        _container.GetItemQueryIterator<PromptTemplate>(
                Arg.Do<QueryDefinition>(query => capturedQuery = query),
                "previous-page",
                Arg.Is<QueryRequestOptions>(options => options.MaxItemCount == 10))
            .Returns(iterator);

        var (items, continuationToken) = await _repository.ListTemplatesAsync(
            userId, "previous-page", 10, " Writer ", " Creative ", "name", "asc");

        items.Should().BeEquivalentTo(templates);
        continuationToken.Should().Be("next-page");
        capturedQuery.Should().NotBeNull();
        capturedQuery!.QueryText.Should().Be(
            "SELECT * FROM c WHERE (c.userId = @builtInUserId OR c.userId = @userId)" +
            " AND CONTAINS(LOWER(c.name), @search)" +
            " AND EXISTS(SELECT VALUE t FROM t IN c.tags WHERE LOWER(t) = @tag)" +
            " ORDER BY c.name ASC");
        capturedQuery.GetQueryParameters().Should().BeEquivalentTo(new (string Name, object Value)[]
        {
            ("@builtInUserId", "_builtin"),
            ("@userId", userId),
            ("@search", "writer"),
            ("@tag", "creative"),
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _container.ReadItemAsync<PromptTemplate>(
            "missing",
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        var result = await _repository.GetByIdAsync("_anonymous", "missing");

        result.Should().BeNull();
    }

    // ---- UpdateAsync ----

    [TestMethod]
    public async Task UpdateAsync_BuiltInTemplate_ThrowsInvalidOperationException()
    {
        var builtIn = CreateTemplate(isBuiltIn: true, userId: "_builtin");

        var act = () => _repository.UpdateAsync(builtIn);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Built-in*");
    }

    [TestMethod]
    public async Task UpdateAsync_BuiltInTemplate_DoesNotCallCosmos()
    {
        var builtIn = CreateTemplate(isBuiltIn: true, userId: "_builtin");

        try { await _repository.UpdateAsync(builtIn); }
        catch (InvalidOperationException) { }

        await _container.DidNotReceive().ReplaceItemAsync(
            Arg.Any<PromptTemplate>(),
            Arg.Any<string>(),
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    // ---- DeleteAsync ----

    [TestMethod]
    public async Task DeleteAsync_NonExistentTemplate_ThrowsInvalidOperationException()
    {
        _container.ReadItemAsync<PromptTemplate>(
            "missing",
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        var act = () => _repository.DeleteAsync("_anonymous", "missing");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ---- Constants ----

    [TestMethod]
    public void BuiltInUserId_HasExpectedValue()
    {
        CosmosPromptTemplateRepository.BuiltInUserId.Should().Be("_builtin");
    }

    [TestMethod]
    public void DatabaseName_HasExpectedValue()
    {
        CosmosPromptTemplateRepository.DatabaseName.Should().Be("prompt-babbler");
    }

    [TestMethod]
    public void ContainerName_HasExpectedValue()
    {
        CosmosPromptTemplateRepository.ContainerName.Should().Be("prompt-templates");
    }
}
