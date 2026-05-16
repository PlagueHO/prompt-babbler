using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IImportJobProcessor
{
    Task ProcessAsync(ImportExportJob job, CancellationToken cancellationToken = default);
}
