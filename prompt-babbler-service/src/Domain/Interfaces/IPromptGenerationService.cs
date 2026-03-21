using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IPromptGenerationService
{
    IAsyncEnumerable<string> GeneratePromptStreamAsync(
        string babbleText,
        PromptTemplate template,
        string? promptFormat = null,
        bool? allowEmojis = null,
        CancellationToken cancellationToken = default);

    Task<string> GenerateTitleAsync(
        string babbleText,
        CancellationToken cancellationToken = default);
}
