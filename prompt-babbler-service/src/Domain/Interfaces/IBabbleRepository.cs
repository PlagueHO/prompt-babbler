using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IBabbleRepository
{
    Task<(IReadOnlyList<Babble> Items, string? ContinuationToken)> GetByUserAsync(
        string userId,
        string? continuationToken = null,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<Babble?> GetByIdAsync(string userId, string babbleId, CancellationToken cancellationToken = default);

    Task<Babble> CreateAsync(Babble babble, CancellationToken cancellationToken = default);

    Task<Babble> UpdateAsync(Babble babble, CancellationToken cancellationToken = default);

    Task DeleteAsync(string userId, string babbleId, CancellationToken cancellationToken = default);
}
