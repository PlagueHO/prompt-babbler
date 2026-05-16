using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Constants;

namespace PromptBabbler.Infrastructure.Services;

public sealed class ExportJobProcessor : IExportJobProcessor
{
    private const int PageSize = 200;

    private readonly IImportExportJobRepository _jobRepository;
    private readonly IBabbleRepository _babbleRepository;
    private readonly IGeneratedPromptRepository _generatedPromptRepository;
    private readonly IPromptTemplateRepository _promptTemplateRepository;
    private readonly ILogger<ExportJobProcessor> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public ExportJobProcessor(
        IImportExportJobRepository jobRepository,
        IBabbleRepository babbleRepository,
        IGeneratedPromptRepository generatedPromptRepository,
        IPromptTemplateRepository promptTemplateRepository,
        ILogger<ExportJobProcessor> logger)
    {
        _jobRepository = jobRepository;
        _babbleRepository = babbleRepository;
        _generatedPromptRepository = generatedPromptRepository;
        _promptTemplateRepository = promptTemplateRepository;
        _logger = logger;
    }

    public async Task ProcessAsync(ImportExportJob job, CancellationToken cancellationToken = default)
    {
        var selection = job.ExportSelection ?? new ExportSelection
        {
            IncludeBabbles = true,
            IncludeGeneratedPrompts = true,
            IncludeUserTemplates = true,
            IncludeSemanticVectors = false,
        };

        var currentJob = job with
        {
            Status = JobStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            CurrentStage = "Preparing export",
            ProgressPercentage = 1,
        };

        await _jobRepository.UpdateAsync(currentJob, cancellationToken);

        try
        {
            var totalBabbles = selection.IncludeBabbles
                ? await _babbleRepository.CountByUserAsync(job.UserId, cancellationToken)
                : 0;
            var totalPrompts = selection.IncludeGeneratedPrompts
                ? await _generatedPromptRepository.CountByUserAsync(job.UserId, cancellationToken)
                : 0;
            var totalTemplates = selection.IncludeUserTemplates
                ? await _promptTemplateRepository.CountByUserAsync(job.UserId, cancellationToken)
                : 0;

            var totalItems = totalBabbles + totalPrompts + totalTemplates;
            var processedItems = 0;

            currentJob = currentJob with { TotalItems = totalItems };
            await _jobRepository.UpdateAsync(currentJob, cancellationToken);

            var tempZipPath = Path.Combine(Path.GetTempPath(), $"prompt-babbler-export-{job.UserId}-{job.Id}.zip");
            await using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: true);

            async Task UpdateProgressAsync(string stage)
            {
                var progress = totalItems == 0 ? 95 : Math.Min(95, (processedItems * 95) / totalItems);
                currentJob = currentJob with
                {
                    CurrentStage = stage,
                    ProcessedItems = processedItems,
                    ProgressPercentage = progress,
                };

                await _jobRepository.UpdateAsync(currentJob, cancellationToken);
            }

            if (selection.IncludeBabbles)
            {
                await UpdateProgressAsync("Exporting babbles");
                string? continuationToken = null;
                do
                {
                    var (items, nextToken) = await _babbleRepository.GetByUserAsync(
                        job.UserId,
                        continuationToken,
                        PageSize,
                        cancellationToken: cancellationToken);

                    foreach (var babble in items)
                    {
                        var exportBabble = selection.IncludeSemanticVectors
                            ? babble
                            : babble with { ContentVector = null };

                        await WriteJsonEntryAsync(archive, $"babbles/{exportBabble.Id}.json", exportBabble, cancellationToken);
                        processedItems++;
                    }

                    continuationToken = nextToken;
                    await EnsureZipSizeWithinLimitAsync(fileStream, cancellationToken);
                    await UpdateProgressAsync("Exporting babbles");
                }
                while (!string.IsNullOrEmpty(continuationToken));
            }

            if (selection.IncludeGeneratedPrompts)
            {
                await UpdateProgressAsync("Exporting generated prompts");
                string? continuationToken = null;
                do
                {
                    var (items, nextToken) = await _generatedPromptRepository.GetByUserAsync(
                        job.UserId,
                        continuationToken,
                        PageSize,
                        cancellationToken);

                    foreach (var generatedPrompt in items)
                    {
                        await WriteJsonEntryAsync(archive, $"generated-prompts/{generatedPrompt.Id}.json", generatedPrompt, cancellationToken);
                        processedItems++;
                    }

                    continuationToken = nextToken;
                    await EnsureZipSizeWithinLimitAsync(fileStream, cancellationToken);
                    await UpdateProgressAsync("Exporting generated prompts");
                }
                while (!string.IsNullOrEmpty(continuationToken));
            }

            if (selection.IncludeUserTemplates)
            {
                await UpdateProgressAsync("Exporting templates");
                var templates = await _promptTemplateRepository.GetUserTemplatesAsync(job.UserId, cancellationToken);
                foreach (var template in templates.Where(t => !t.IsBuiltIn))
                {
                    await WriteJsonEntryAsync(archive, $"templates/{template.Id}.json", template, cancellationToken);
                    processedItems++;
                }

                await EnsureZipSizeWithinLimitAsync(fileStream, cancellationToken);
                await UpdateProgressAsync("Exporting templates");
            }

            var manifest = new ExportManifest
            {
                ExportedAt = DateTimeOffset.UtcNow,
                SourceUserId = job.UserId,
                AppVersion = typeof(ExportJobProcessor).Assembly.GetName().Version?.ToString(),
                Selection = selection,
                Counts = new ExportManifestCounts
                {
                    Babbles = totalBabbles,
                    GeneratedPrompts = totalPrompts,
                    Templates = totalTemplates,
                },
            };

            await WriteJsonEntryAsync(archive, "manifest.json", manifest, cancellationToken);
            await EnsureZipSizeWithinLimitAsync(fileStream, cancellationToken);

            currentJob = currentJob with
            {
                Status = JobStatus.Completed,
                CompletedAt = DateTimeOffset.UtcNow,
                CurrentStage = "Completed",
                ProgressPercentage = 100,
                ProcessedItems = processedItems,
                ResultFilePath = tempZipPath,
            };

            await _jobRepository.UpdateAsync(currentJob, cancellationToken);
            _logger.LogInformation("Completed export job {JobId} for user {UserId}", job.Id, job.UserId);
        }
        catch (Exception ex)
        {
            currentJob = currentJob with
            {
                Status = JobStatus.Failed,
                CompletedAt = DateTimeOffset.UtcNow,
                CurrentStage = "Failed",
                ErrorMessage = ex.Message,
            };

            await _jobRepository.UpdateAsync(currentJob, cancellationToken);
            _logger.LogError(ex, "Export job {JobId} failed for user {UserId}", job.Id, job.UserId);
        }
    }

    private async Task WriteJsonEntryAsync<T>(ZipArchive archive, string path, T value, CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        await using var entryStream = entry.Open();
        await JsonSerializer.SerializeAsync(entryStream, value, _jsonSerializerOptions, cancellationToken);
    }

    private static async Task EnsureZipSizeWithinLimitAsync(FileStream fileStream, CancellationToken cancellationToken)
    {
        await fileStream.FlushAsync(cancellationToken);
        if (fileStream.Length > ExportLimits.MaxZipBytes)
        {
            throw new InvalidOperationException($"Export ZIP exceeded maximum size of {ExportLimits.MaxZipBytes} bytes.");
        }
    }
}
