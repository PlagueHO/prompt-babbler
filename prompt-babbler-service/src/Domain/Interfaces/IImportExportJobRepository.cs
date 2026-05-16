using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IImportExportJobRepository
{
    Task<ImportExportJob> CreateAsync(ImportExportJob job, CancellationToken cancellationToken = default);

    Task<ImportExportJob?> GetByIdAsync(string userId, string jobId, CancellationToken cancellationToken = default);

    Task<ImportExportJob> UpdateAsync(ImportExportJob job, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportExportJob>> ListActiveByUserAsync(string userId, CancellationToken cancellationToken = default);
}
