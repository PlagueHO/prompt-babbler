namespace PromptBabbler.Domain.Interfaces;

public interface IPromptGenerationService
{
    IAsyncEnumerable<string> GeneratePromptStreamAsync(
        string babbleText,
        string systemPrompt,
        CancellationToken cancellationToken = default);
}
