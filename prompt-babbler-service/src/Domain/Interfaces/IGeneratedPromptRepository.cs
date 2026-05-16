using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IGeneratedPromptRepository
{
    Task<int> CountByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GeneratedPrompt> Items, string? ContinuationToken)> GetByUserAsync(
        string userId,
        string? continuationToken = null,
        int pageSize = 200,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GeneratedPrompt> Items, string? ContinuationToken)> GetByBabbleAsync(
        string babbleId,
        string? continuationToken = null,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<GeneratedPrompt?> GetByIdAsync(string babbleId, string promptId, CancellationToken cancellationToken = default);

    Task<GeneratedPrompt> CreateAsync(GeneratedPrompt prompt, CancellationToken cancellationToken = default);

    Task DeleteAsync(string babbleId, string promptId, CancellationToken cancellationToken = default);

    Task DeleteByBabbleAsync(string babbleId, CancellationToken cancellationToken = default);
}
