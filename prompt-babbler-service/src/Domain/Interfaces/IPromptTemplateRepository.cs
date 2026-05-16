using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IPromptTemplateRepository
{
    Task<int> CountByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PromptTemplate> Items, string? ContinuationToken)> ListTemplatesAsync(
        string userId,
        string? continuationToken = null,
        int pageSize = 20,
        string? search = null,
        string? tag = null,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromptTemplate>> GetBuiltInTemplatesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromptTemplate>> GetUserTemplatesAsync(string userId, CancellationToken cancellationToken = default);

    Task<PromptTemplate?> GetByIdAsync(string userId, string templateId, CancellationToken cancellationToken = default);

    Task<PromptTemplate> CreateAsync(PromptTemplate template, CancellationToken cancellationToken = default);

    Task<PromptTemplate> UpdateAsync(PromptTemplate template, CancellationToken cancellationToken = default);

    Task DeleteAsync(string userId, string templateId, CancellationToken cancellationToken = default);

    Task UpsertAsync(PromptTemplate template, CancellationToken cancellationToken = default);
}
