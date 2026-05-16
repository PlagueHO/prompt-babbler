using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class BabbleServiceTests
{
    private readonly IBabbleRepository _babbleRepository = Substitute.For<IBabbleRepository>();
    private readonly IGeneratedPromptRepository _generatedPromptRepository = Substitute.For<IGeneratedPromptRepository>();
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly ILogger<BabbleService> _logger = Substitute.For<ILogger<BabbleService>>();
    private readonly BabbleService _service;

    public BabbleServiceTests()
    {
        _service = new BabbleService(_babbleRepository, _generatedPromptRepository, _embeddingService, _logger);
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

    // ---- GetByUserAsync ----

    [TestMethod]
    public async Task GetByUserAsync_DelegatesToRepository()
    {
        var expected = new List<Babble> { CreateBabble() };
        _babbleRepository.GetByUserAsync("user-1", null, 20, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((expected.AsReadOnly(), (string?)null));

        var (items, token) = await _service.GetByUserAsync("user-1");

        items.Should().HaveCount(1);
        token.Should().BeNull();
    }

    // ---- GetByIdAsync ----

    [TestMethod]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        var babble = CreateBabble();
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(babble);

        var result = await _service.GetByIdAsync("test-user-id", "test-babble-id");

        result.Should().Be(babble);
    }

    // ---- CreateAsync ----

    [TestMethod]
    public async Task CreateAsync_DelegatesToRepository()
    {
        var babble = CreateBabble();
        _babbleRepository.CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var result = await _service.CreateAsync(babble);

        result.Id.Should().Be(babble.Id);
        result.Text.Should().Be(babble.Text);
        await _babbleRepository.Received(1).CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpsertAsync_DelegatesToRepository()
    {
        var babble = CreateBabble();
        _babbleRepository.UpsertAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var result = await _service.UpsertAsync(babble);

        result.Id.Should().Be(babble.Id);
        result.Text.Should().Be(babble.Text);
        await _babbleRepository.Received(1).UpsertAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>());
    }

    // ---- UpdateAsync ----

    [TestMethod]
    public async Task UpdateAsync_ExistingBabble_DelegatesToRepository()
    {
        var babble = CreateBabble();
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(babble);
        _babbleRepository.UpdateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var result = await _service.UpdateAsync("test-user-id", babble);

        result.Id.Should().Be(babble.Id);
        result.Text.Should().Be(babble.Text);
        await _babbleRepository.Received(1).UpdateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateAsync_NonExistentBabble_ThrowsInvalidOperationException()
    {
        var babble = CreateBabble();
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var act = () => _service.UpdateAsync("test-user-id", babble);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ---- DeleteAsync ----

    [TestMethod]
    public async Task DeleteAsync_CascadeDeletesGeneratedPrompts()
    {
        var babble = CreateBabble();
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(babble);

        await _service.DeleteAsync("test-user-id", "test-babble-id");

        await _generatedPromptRepository.Received(1).DeleteByBabbleAsync("test-babble-id", Arg.Any<CancellationToken>());
        await _babbleRepository.Received(1).DeleteAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteAsync_DeletesGeneratedPromptsBeforeBabble()
    {
        var callOrder = new List<string>();
        _generatedPromptRepository.DeleteByBabbleAsync("test-babble-id", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("prompts"));
        _babbleRepository.DeleteAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("babble"));

        await _service.DeleteAsync("test-user-id", "test-babble-id");

        callOrder.Should().ContainInOrder("prompts", "babble");
    }

    // ---- SetPinAsync ----

    [TestMethod]
    public async Task SetPinAsync_WithValidBabble_CallsRepositoryAndReturnsResult()
    {
        // Arrange
        var babble = CreateBabble() with { IsPinned = true };
        _babbleRepository.SetPinAsync("test-user-id", "test-babble-id", true, Arg.Any<CancellationToken>())
            .Returns(babble);

        // Act
        var result = await _service.SetPinAsync("test-user-id", "test-babble-id", true);

        // Assert
        result.Should().Be(babble);
        result.IsPinned.Should().BeTrue();
        await _babbleRepository.Received(1).SetPinAsync("test-user-id", "test-babble-id", true, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SetPinAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        _babbleRepository.SetPinAsync("test-user-id", "missing-id", true, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Babble not found"));

        // Act
        var act = () => _service.SetPinAsync("test-user-id", "missing-id", true);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ---- CreateAsync with embedding ----

    [TestMethod]
    public async Task CreateAsync_WithText_GeneratesEmbeddingAndStoresVector()
    {
        var babble = CreateBabble();
        var expectedVector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
        _embeddingService.GenerateEmbeddingAsync(babble.Text, Arg.Any<CancellationToken>())
            .Returns(expectedVector);
        _babbleRepository.CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var result = await _service.CreateAsync(babble);

        result.ContentVector.Should().NotBeNull();
        result.ContentVector.Should().BeEquivalentTo(new float[] { 0.1f, 0.2f, 0.3f });
        await _embeddingService.Received(1).GenerateEmbeddingAsync(babble.Text, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task CreateAsync_EmbeddingServiceFails_SavesBabbleWithoutVector()
    {
        var babble = CreateBabble();
        _embeddingService.GenerateEmbeddingAsync(babble.Text, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Embedding service unavailable"));
        _babbleRepository.CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var result = await _service.CreateAsync(babble);

        result.ContentVector.Should().BeNull();
        await _babbleRepository.Received(1).CreateAsync(Arg.Is<Babble>(b => b.ContentVector == null), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpsertAsync_EmbeddingServiceFails_UpsertsBabbleWithoutVector()
    {
        var babble = CreateBabble();
        _embeddingService.GenerateEmbeddingAsync(babble.Text, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Embedding service unavailable"));
        _babbleRepository.UpsertAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var result = await _service.UpsertAsync(babble);

        result.ContentVector.Should().BeNull();
        await _babbleRepository.Received(1).UpsertAsync(Arg.Is<Babble>(b => b.ContentVector == null), Arg.Any<CancellationToken>());
    }

    // ---- UpdateAsync with embedding ----

    [TestMethod]
    public async Task UpdateAsync_WithTextChange_RegeneratesEmbedding()
    {
        var babble = CreateBabble();
        var expectedVector = new ReadOnlyMemory<float>(new float[] { 0.4f, 0.5f, 0.6f });
        _babbleRepository.GetByIdAsync("test-user-id", "test-babble-id", Arg.Any<CancellationToken>())
            .Returns(babble);
        _embeddingService.GenerateEmbeddingAsync(babble.Text, Arg.Any<CancellationToken>())
            .Returns(expectedVector);
        _babbleRepository.UpdateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var result = await _service.UpdateAsync("test-user-id", babble);

        result.ContentVector.Should().NotBeNull();
        result.ContentVector.Should().BeEquivalentTo(new float[] { 0.4f, 0.5f, 0.6f });
        await _embeddingService.Received(1).GenerateEmbeddingAsync(babble.Text, Arg.Any<CancellationToken>());
    }

    // ---- SearchAsync ----

    [TestMethod]
    public async Task SearchAsync_WithQuery_ReturnsRankedResults()
    {
        // "test query string" — 3 words, routes to vector + keyword
        var query = "test query string";
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
        var vectorResults = new List<BabbleSearchResult>
        {
            new(CreateBabble(id: "result-1"), 0.95),
            new(CreateBabble(id: "result-2"), 0.80),
        };

        _embeddingService.GenerateEmbeddingAsync(query, Arg.Any<CancellationToken>())
            .Returns(vector);
        _babbleRepository.SearchByKeywordAsync("test-user-id", query, 10, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult>().AsReadOnly());
        _babbleRepository.SearchByVectorAsync("test-user-id", vector, 10, Arg.Any<CancellationToken>())
            .Returns(vectorResults.AsReadOnly());

        var results = await _service.SearchAsync("test-user-id", query, 10);

        results.Should().HaveCount(2);
        results[0].SimilarityScore.Should().Be(0.95);
        results[1].SimilarityScore.Should().Be(0.80);
        await _embeddingService.Received(1).GenerateEmbeddingAsync(query, Arg.Any<CancellationToken>());
        await _babbleRepository.Received(1).SearchByVectorAsync("test-user-id", vector, 10, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SearchAsync_SingleWordShortQuery_SkipsEmbeddingAndReturnsKeywordResults()
    {
        // 1 word, < 10 chars — routes to keyword search only
        var query = "idea";
        var keywordResults = new List<BabbleSearchResult>
        {
            new(CreateBabble(id: "keyword-match"), 1.0),
        };

        _babbleRepository.SearchByKeywordAsync("test-user-id", query, 10, Arg.Any<CancellationToken>())
            .Returns(keywordResults.AsReadOnly());

        var results = await _service.SearchAsync("test-user-id", query, 10);

        results.Should().HaveCount(1);
        results[0].Babble.Id.Should().Be("keyword-match");
        await _embeddingService.DidNotReceiveWithAnyArgs().GenerateEmbeddingAsync(default!, default);
        await _babbleRepository.DidNotReceiveWithAnyArgs().SearchByVectorAsync(default!, default, default, default);
    }

    [TestMethod]
    public async Task SearchAsync_TwoWordQuery_TriggersSemanticSearch()
    {
        // 2 words — meets the new threshold, routes to keyword + vector
        var query = "my idea";
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });

        _embeddingService.GenerateEmbeddingAsync(query, Arg.Any<CancellationToken>())
            .Returns(vector);
        _babbleRepository.SearchByKeywordAsync("test-user-id", query, 10, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult>().AsReadOnly());
        _babbleRepository.SearchByVectorAsync("test-user-id", vector, 10, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult>().AsReadOnly());

        await _service.SearchAsync("test-user-id", query, 10);

        await _embeddingService.Received(1).GenerateEmbeddingAsync(query, Arg.Any<CancellationToken>());
        await _babbleRepository.Received(1).SearchByVectorAsync("test-user-id", vector, 10, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SearchAsync_KeywordMatchWithNoVectorMatch_IncludesResult()
    {
        // 3-word query routes to vector; keyword match comes through even with no vector hit
        var query = "unique title match";
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
        var keywordResult = new BabbleSearchResult(CreateBabble(id: "keyword-match"), 1.0);

        _embeddingService.GenerateEmbeddingAsync(query, Arg.Any<CancellationToken>()).Returns(vector);
        _babbleRepository.SearchByKeywordAsync("test-user-id", query, 10, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult> { keywordResult }.AsReadOnly());
        _babbleRepository.SearchByVectorAsync("test-user-id", vector, 10, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult>().AsReadOnly());

        var results = await _service.SearchAsync("test-user-id", query, 10);

        results.Should().HaveCount(1);
        results[0].Babble.Id.Should().Be("keyword-match");
        results[0].SimilarityScore.Should().Be(1.0);
    }

    [TestMethod]
    public async Task SearchAsync_BabbleMatchesBothKeywordAndVector_KeepsHigherScore()
    {
        // Same babble returned by both — deduplication keeps the higher keyword score
        var query = "overlap query here";
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
        var sharedBabble = CreateBabble(id: "shared");

        _embeddingService.GenerateEmbeddingAsync(query, Arg.Any<CancellationToken>()).Returns(vector);
        _babbleRepository.SearchByKeywordAsync("test-user-id", query, 10, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult> { new(sharedBabble, 1.0) }.AsReadOnly());
        _babbleRepository.SearchByVectorAsync("test-user-id", vector, 10, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult> { new(sharedBabble, 0.82) }.AsReadOnly());

        var results = await _service.SearchAsync("test-user-id", query, 10);

        results.Should().HaveCount(1);
        results[0].SimilarityScore.Should().Be(1.0);
    }

    [TestMethod]
    public async Task SearchAsync_EmbeddingServiceFails_PropagatesException()
    {
        // 3-word query triggers vector path; embedding failure propagates
        var query = "test query here";
        _embeddingService.GenerateEmbeddingAsync(query, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Embedding service unavailable"));
        _babbleRepository.SearchByKeywordAsync("test-user-id", query, 10, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult>().AsReadOnly());

        await _service.Invoking(s => s.SearchAsync("test-user-id", query, 10))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
