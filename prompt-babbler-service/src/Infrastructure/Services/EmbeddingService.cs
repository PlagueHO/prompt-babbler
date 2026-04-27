using Microsoft.Extensions.AI;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Infrastructure.Services;

public sealed class EmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : IEmbeddingService
{
    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var embeddings = await embeddingGenerator.GenerateAsync(
            [text],
            cancellationToken: cancellationToken);

        return embeddings[0].Vector;
    }
}
