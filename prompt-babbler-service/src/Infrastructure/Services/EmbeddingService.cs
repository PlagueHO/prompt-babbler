using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Infrastructure.Services;

public sealed class EmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    ILogger<EmbeddingService> logger) : IEmbeddingService
{
    // text-embedding-3-small has an 8,191-token context window.
    // Using a conservative 4 characters per token gives ~32,764 chars; cap at 32,000.
    private const int MaxCharacters = 32_000;

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (text.Length > MaxCharacters)
        {
            logger.LogWarning(
                "Text length {Length} exceeds embedding context window limit ({Limit} characters). Truncating before generating embedding.",
                text.Length,
                MaxCharacters);
            text = text[..MaxCharacters];
        }

        var embeddings = await embeddingGenerator.GenerateAsync(
            [text],
            cancellationToken: cancellationToken);

        if (embeddings.Count == 0)
        {
            throw new InvalidOperationException("Embedding generator returned no results for the provided text.");
        }

        return embeddings[0].Vector;
    }
}
