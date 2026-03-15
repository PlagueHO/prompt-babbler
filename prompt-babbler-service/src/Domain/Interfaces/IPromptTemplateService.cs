using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IPromptTemplateService
{
    Task<IReadOnlyList<PromptTemplate>> GetTemplatesAsync(string? userId, bool forceRefresh = false, CancellationToken cancellationToken = default);

    Task<PromptTemplate?> GetByIdAsync(string? userId, string templateId, CancellationToken cancellationToken = default);

    Task<PromptTemplate> CreateAsync(PromptTemplate template, CancellationToken cancellationToken = default);

    Task<PromptTemplate> UpdateAsync(PromptTemplate template, CancellationToken cancellationToken = default);

    Task DeleteAsync(string userId, string templateId, CancellationToken cancellationToken = default);
}
