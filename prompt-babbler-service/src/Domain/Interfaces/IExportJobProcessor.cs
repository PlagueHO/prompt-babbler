using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IExportJobProcessor
{
    Task ProcessAsync(ImportExportJob job, CancellationToken cancellationToken = default);
}
