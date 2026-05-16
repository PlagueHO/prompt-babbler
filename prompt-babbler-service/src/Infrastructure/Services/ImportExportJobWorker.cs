using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class ImportExportJobWorker : BackgroundService
{
    private readonly IImportExportJobQueue _jobQueue;
    private readonly IImportExportJobRepository _jobRepository;
    private readonly IExportJobProcessor _exportJobProcessor;
    private readonly IImportJobProcessor _importJobProcessor;
    private readonly ILogger<ImportExportJobWorker> _logger;

    public ImportExportJobWorker(
        IImportExportJobQueue jobQueue,
        IImportExportJobRepository jobRepository,
        IExportJobProcessor exportJobProcessor,
        IImportJobProcessor importJobProcessor,
        ILogger<ImportExportJobWorker> logger)
    {
        _jobQueue = jobQueue;
        _jobRepository = jobRepository;
        _exportJobProcessor = exportJobProcessor;
        _importJobProcessor = importJobProcessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queueItem = await _jobQueue.DequeueAsync(stoppingToken);
                var job = await _jobRepository.GetByIdAsync(queueItem.UserId, queueItem.JobId, stoppingToken);

                if (job is null)
                {
                    _logger.LogWarning("Queued job {JobId} for user {UserId} was not found.", queueItem.JobId, queueItem.UserId);
                    continue;
                }

                try
                {
                    if (job.JobType == ImportExportJobType.Export)
                    {
                        await _exportJobProcessor.ProcessAsync(job, stoppingToken);
                    }
                    else
                    {
                        await _importJobProcessor.ProcessAsync(job, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Import/export job {JobId} failed for user {UserId}", job.Id, job.UserId);
                    var failed = job with
                    {
                        Status = JobStatus.Failed,
                        CompletedAt = DateTimeOffset.UtcNow,
                        CurrentStage = "Failed",
                        ErrorMessage = ex.Message,
                    };
                    await _jobRepository.UpdateAsync(failed, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Host shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing import/export job.");
            }
        }
    }
}
