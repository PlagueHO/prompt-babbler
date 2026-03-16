using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IGeneratedPromptService
{
    Task<(IReadOnlyList<GeneratedPrompt> Items, string? ContinuationToken)> GetByBabbleAsync(
        string userId,
        string babbleId,
        string? continuationToken = null,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<GeneratedPrompt?> GetByIdAsync(
        string userId,
        string babbleId,
        string promptId,
        CancellationToken cancellationToken = default);

    Task<GeneratedPrompt> CreateAsync(string userId, GeneratedPrompt prompt, CancellationToken cancellationToken = default);

    Task DeleteAsync(string userId, string babbleId, string promptId, CancellationToken cancellationToken = default);
}
