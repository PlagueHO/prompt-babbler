namespace PromptBabbler.Domain.Interfaces;

public interface IPromptGenerationService
{
    IAsyncEnumerable<string> GeneratePromptStreamAsync(
        string babbleText,
        string systemPrompt,
        string promptFormat = "text",
        bool allowEmojis = false,
        CancellationToken cancellationToken = default);

    Task<string> GenerateTitleAsync(
        string babbleText,
        CancellationToken cancellationToken = default);
}
