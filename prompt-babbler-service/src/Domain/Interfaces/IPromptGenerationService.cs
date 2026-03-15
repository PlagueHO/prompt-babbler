using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IPromptGenerationService
{
    IAsyncEnumerable<string> GeneratePromptStreamAsync(
        string babbleText,
        string systemPrompt,
        string promptFormat = "text",
        bool allowEmojis = false,
        CancellationToken cancellationToken = default);

    Task<StructuredPromptResult> GenerateStructuredPromptAsync(
        string babbleText,
        string systemPrompt,
        string templateName,
        string promptFormat = "text",
        bool allowEmojis = false,
        CancellationToken cancellationToken = default);
}
