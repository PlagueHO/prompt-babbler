using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IExportService
{
    Task<string> StartExportAsync(string userId, ExportSelection selection, CancellationToken cancellationToken = default);
}
