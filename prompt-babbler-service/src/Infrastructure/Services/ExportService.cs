using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Constants;

namespace PromptBabbler.Infrastructure.Services;

public sealed class ExportService : IExportService
{
    private readonly IImportExportJobRepository _jobRepository;
    private readonly IImportExportJobQueue _jobQueue;
    private readonly IBabbleRepository _babbleRepository;
    private readonly IGeneratedPromptRepository _generatedPromptRepository;
    private readonly IPromptTemplateRepository _promptTemplateRepository;

    public ExportService(
        IImportExportJobRepository jobRepository,
        IImportExportJobQueue jobQueue,
        IBabbleRepository babbleRepository,
        IGeneratedPromptRepository generatedPromptRepository,
        IPromptTemplateRepository promptTemplateRepository)
    {
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
        _babbleRepository = babbleRepository;
        _generatedPromptRepository = generatedPromptRepository;
        _promptTemplateRepository = promptTemplateRepository;
    }

    public async Task<string> StartExportAsync(string userId, ExportSelection selection, CancellationToken cancellationToken = default)
    {
        if (!selection.IncludeBabbles && !selection.IncludeGeneratedPrompts && !selection.IncludeUserTemplates)
        {
            throw new InvalidOperationException("At least one export data type must be selected.");
        }

        if (selection.IncludeBabbles)
        {
            var babbleCount = await _babbleRepository.CountByUserAsync(userId, cancellationToken);
            if (babbleCount > ExportLimits.MaxBabbles)
            {
                throw new InvalidOperationException($"Babbles export limit exceeded. Maximum allowed is {ExportLimits.MaxBabbles}.");
            }
        }

        if (selection.IncludeGeneratedPrompts)
        {
            var generatedPromptCount = await _generatedPromptRepository.CountByUserAsync(userId, cancellationToken);
            if (generatedPromptCount > ExportLimits.MaxGeneratedPrompts)
            {
                throw new InvalidOperationException($"Generated prompts export limit exceeded. Maximum allowed is {ExportLimits.MaxGeneratedPrompts}.");
            }
        }

        if (selection.IncludeUserTemplates)
        {
            var templateCount = await _promptTemplateRepository.CountByUserAsync(userId, cancellationToken);
            if (templateCount > ExportLimits.MaxUserTemplates)
            {
                throw new InvalidOperationException($"User templates export limit exceeded. Maximum allowed is {ExportLimits.MaxUserTemplates}.");
            }
        }

        var jobId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        var job = new ImportExportJob
        {
            Id = jobId,
            UserId = userId,
            JobType = ImportExportJobType.Export,
            Status = JobStatus.Queued,
            CreatedAt = now,
            ProgressPercentage = 0,
            CurrentStage = "Queued",
            TotalItems = 0,
            ProcessedItems = 0,
            ExportSelection = selection,
            OverwriteExisting = false,
        };

        await _jobRepository.CreateAsync(job, cancellationToken);
        await _jobQueue.EnqueueAsync(new ImportExportJobQueueItem { JobId = jobId, UserId = userId }, cancellationToken);

        return jobId;
    }
}
