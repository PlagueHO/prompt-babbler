using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IImportExportJobQueue
{
    ValueTask EnqueueAsync(ImportExportJobQueueItem queueItem, CancellationToken cancellationToken = default);

    ValueTask<ImportExportJobQueueItem> DequeueAsync(CancellationToken cancellationToken = default);
}
