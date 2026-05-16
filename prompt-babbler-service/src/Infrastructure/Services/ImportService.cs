using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class ImportService : IImportService
{
    private readonly IImportExportJobRepository _jobRepository;
    private readonly IImportExportJobQueue _jobQueue;

    public ImportService(IImportExportJobRepository jobRepository, IImportExportJobQueue jobQueue)
    {
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
    }

    public async Task<string> StartImportAsync(string userId, string sourceFilePath, bool overwriteExisting, CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        var job = new ImportExportJob
        {
            Id = jobId,
            UserId = userId,
            JobType = ImportExportJobType.Import,
            Status = JobStatus.Queued,
            CreatedAt = now,
            ProgressPercentage = 0,
            CurrentStage = "Queued",
            TotalItems = 0,
            ProcessedItems = 0,
            OverwriteExisting = overwriteExisting,
            SourceFilePath = sourceFilePath,
            Counts = new ImportExportCounts(),
        };

        await _jobRepository.CreateAsync(job, cancellationToken);
        await _jobQueue.EnqueueAsync(new ImportExportJobQueueItem { JobId = jobId, UserId = userId }, cancellationToken);

        return jobId;
    }
}
