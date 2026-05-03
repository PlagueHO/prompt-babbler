using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IBabbleRepository
{
    Task<(IReadOnlyList<Babble> Items, string? ContinuationToken)> GetByUserAsync(
        string userId,
        string? continuationToken = null,
        int pageSize = 20,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null,
        bool? isPinned = null,
        CancellationToken cancellationToken = default);

    Task<Babble?> GetByIdAsync(string userId, string babbleId, CancellationToken cancellationToken = default);

    Task<Babble> CreateAsync(Babble babble, CancellationToken cancellationToken = default);

    Task<Babble> UpdateAsync(Babble babble, CancellationToken cancellationToken = default);

    Task DeleteAsync(string userId, string babbleId, CancellationToken cancellationToken = default);

    Task<Babble> SetPinAsync(string userId, string babbleId, bool isPinned, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BabbleSearchResult>> SearchByVectorAsync(string userId, ReadOnlyMemory<float> vector, int topN, CancellationToken cancellationToken = default);
}
