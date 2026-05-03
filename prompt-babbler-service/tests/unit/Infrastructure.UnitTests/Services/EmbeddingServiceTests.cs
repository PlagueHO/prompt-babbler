using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class EmbeddingServiceTests
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator =
        Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();

    private readonly ILogger<EmbeddingService> _logger = Substitute.For<ILogger<EmbeddingService>>();

    private readonly EmbeddingService _service;

    public EmbeddingServiceTests()
    {
        _service = new EmbeddingService(_embeddingGenerator, _logger);
    }

    [TestMethod]
    public async Task GenerateEmbeddingAsync_ReturnsVector_WhenTextProvided()
    {
        var testVector = new float[] { 0.1f, 0.2f, 0.3f };
        var embedding = new Embedding<float>(testVector);
        var embeddings = new GeneratedEmbeddings<Embedding<float>>([embedding]);

        _embeddingGenerator.GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(embeddings);

        var result = await _service.GenerateEmbeddingAsync("test text");

        result.ToArray().Should().Equal(testVector);
    }

    [TestMethod]
    public async Task GenerateEmbeddingAsync_PassesCancellationToken()
    {
        var testVector = new float[] { 0.5f };
        var embedding = new Embedding<float>(testVector);
        var embeddings = new GeneratedEmbeddings<Embedding<float>>([embedding]);
        using var cts = new CancellationTokenSource();

        _embeddingGenerator.GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(embeddings);

        await _service.GenerateEmbeddingAsync("test text", cts.Token);

        await _embeddingGenerator.Received(1).GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            cts.Token);
    }

    [TestMethod]
    public async Task GenerateEmbeddingAsync_TextExceedsLimit_TruncatesAndLogsWarning()
    {
        var longText = new string('a', 33_000);
        var testVector = new float[] { 0.1f, 0.2f };
        var embedding = new Embedding<float>(testVector);
        var embeddings = new GeneratedEmbeddings<Embedding<float>>([embedding]);

        _embeddingGenerator.GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(embeddings);

        var result = await _service.GenerateEmbeddingAsync(longText);

        result.ToArray().Should().Equal(testVector);
        await _embeddingGenerator.Received(1).GenerateAsync(
            Arg.Is<IEnumerable<string>>(texts => texts.Single().Length == 32_000),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>());
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [TestMethod]
    public async Task GenerateEmbeddingAsync_TextAtLimit_DoesNotTruncateOrWarn()
    {
        var exactText = new string('b', 32_000);
        var testVector = new float[] { 0.3f };
        var embedding = new Embedding<float>(testVector);
        var embeddings = new GeneratedEmbeddings<Embedding<float>>([embedding]);

        _embeddingGenerator.GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(embeddings);

        var result = await _service.GenerateEmbeddingAsync(exactText);

        result.ToArray().Should().Equal(testVector);
        await _embeddingGenerator.Received(1).GenerateAsync(
            Arg.Is<IEnumerable<string>>(texts => texts.Single().Length == 32_000),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>());
        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
