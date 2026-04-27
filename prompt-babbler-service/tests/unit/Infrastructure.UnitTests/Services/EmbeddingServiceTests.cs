using FluentAssertions;
using Microsoft.Extensions.AI;
using NSubstitute;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class EmbeddingServiceTests
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator =
        Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();

    private readonly EmbeddingService _service;

    public EmbeddingServiceTests()
    {
        _service = new EmbeddingService(_embeddingGenerator);
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

        result.ToArray().Should().BeEquivalentTo(testVector);
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
}
