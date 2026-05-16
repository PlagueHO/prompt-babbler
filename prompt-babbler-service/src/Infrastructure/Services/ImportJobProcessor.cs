using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class ImportJobProcessor : IImportJobProcessor
{
    private readonly IImportExportJobRepository _jobRepository;
    private readonly IBabbleRepository _babbleRepository;
    private readonly IGeneratedPromptRepository _generatedPromptRepository;
    private readonly IPromptTemplateRepository _promptTemplateRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<ImportJobProcessor> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public ImportJobProcessor(
        IImportExportJobRepository jobRepository,
        IBabbleRepository babbleRepository,
        IGeneratedPromptRepository generatedPromptRepository,
        IPromptTemplateRepository promptTemplateRepository,
        IEmbeddingService embeddingService,
        ILogger<ImportJobProcessor> logger)
    {
        _jobRepository = jobRepository;
        _babbleRepository = babbleRepository;
        _generatedPromptRepository = generatedPromptRepository;
        _promptTemplateRepository = promptTemplateRepository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task ProcessAsync(ImportExportJob job, CancellationToken cancellationToken = default)
    {
        var currentJob = job with
        {
            Status = JobStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            CurrentStage = "Preparing import",
            ProgressPercentage = 1,
            Counts = new ImportExportCounts(),
        };

        await _jobRepository.UpdateAsync(currentJob, cancellationToken);

        try
        {
            if (string.IsNullOrWhiteSpace(job.SourceFilePath) || !File.Exists(job.SourceFilePath))
            {
                throw new InvalidOperationException("Import source ZIP file was not found.");
            }

            var processedItems = 0;
            await using var fileStream = new FileStream(job.SourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);

            var manifest = await ReadManifestAsync(archive, cancellationToken);
            if (manifest.SchemaVersion != 1)
            {
                throw new InvalidOperationException($"Unsupported import schema version '{manifest.SchemaVersion}'.");
            }

            var entries = archive.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .Where(e => e.FullName.StartsWith("babbles/", StringComparison.OrdinalIgnoreCase)
                    || e.FullName.StartsWith("generated-prompts/", StringComparison.OrdinalIgnoreCase)
                    || e.FullName.StartsWith("templates/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            currentJob = currentJob with { TotalItems = entries.Count };
            await _jobRepository.UpdateAsync(currentJob, cancellationToken);

            async Task UpdateProgressAsync(string stage)
            {
                var progress = entries.Count == 0 ? 95 : Math.Min(95, (processedItems * 95) / entries.Count);
                currentJob = currentJob with
                {
                    CurrentStage = stage,
                    ProcessedItems = processedItems,
                    ProgressPercentage = progress,
                };

                await _jobRepository.UpdateAsync(currentJob, cancellationToken);
            }

            foreach (var entry in entries.Where(e => e.FullName.StartsWith("babbles/", StringComparison.OrdinalIgnoreCase)))
            {
                await UpdateProgressAsync("Importing babbles");
                await ImportBabbleAsync(entry, currentJob, cancellationToken);
                processedItems++;
            }

            foreach (var entry in entries.Where(e => e.FullName.StartsWith("generated-prompts/", StringComparison.OrdinalIgnoreCase)))
            {
                await UpdateProgressAsync("Importing generated prompts");
                await ImportGeneratedPromptAsync(entry, currentJob, cancellationToken);
                processedItems++;
            }

            foreach (var entry in entries.Where(e => e.FullName.StartsWith("templates/", StringComparison.OrdinalIgnoreCase)))
            {
                await UpdateProgressAsync("Importing templates");
                await ImportTemplateAsync(entry, currentJob, cancellationToken);
                processedItems++;
            }

            currentJob = currentJob with
            {
                Status = JobStatus.Completed,
                CompletedAt = DateTimeOffset.UtcNow,
                CurrentStage = "Completed",
                ProgressPercentage = 100,
                ProcessedItems = processedItems,
            };

            await _jobRepository.UpdateAsync(currentJob, cancellationToken);
            _logger.LogInformation("Completed import job {JobId} for user {UserId}", job.Id, job.UserId);
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
            _logger.LogError(ex, "Import job {JobId} failed for user {UserId}", job.Id, job.UserId);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(job.SourceFilePath) && File.Exists(job.SourceFilePath))
            {
                try
                {
                    File.Delete(job.SourceFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary import file {SourceFilePath}", job.SourceFilePath);
                }
            }
        }
    }

    private async Task ImportBabbleAsync(ZipArchiveEntry entry, ImportExportJob job, CancellationToken cancellationToken)
    {
        var importedBabble = await DeserializeEntryAsync<Babble>(entry, cancellationToken);
        if (importedBabble is null)
        {
            await IncrementFailedAsync(job, cancellationToken);
            return;
        }

        var existing = await _babbleRepository.GetByIdAsync(job.UserId, importedBabble.Id, cancellationToken);
        if (existing is not null && !job.OverwriteExisting)
        {
            await UpdateCountsAsync(job, counts => counts with { BabblesSkipped = counts.BabblesSkipped + 1 }, cancellationToken);
            return;
        }

        var vector = importedBabble.ContentVector;
        if (vector is null || vector.Length == 0)
        {
            vector = (await _embeddingService.GenerateEmbeddingAsync(importedBabble.Text, cancellationToken)).ToArray();
        }

        var babbleToSave = importedBabble with
        {
            UserId = job.UserId,
            ContentVector = vector,
        };

        if (existing is null)
        {
            await _babbleRepository.CreateAsync(babbleToSave, cancellationToken);
        }
        else
        {
            await _babbleRepository.UpdateAsync(babbleToSave, cancellationToken);
        }

        await UpdateCountsAsync(job, counts => counts with { BabblesImported = counts.BabblesImported + 1 }, cancellationToken);
    }

    private async Task ImportGeneratedPromptAsync(ZipArchiveEntry entry, ImportExportJob job, CancellationToken cancellationToken)
    {
        var importedPrompt = await DeserializeEntryAsync<GeneratedPrompt>(entry, cancellationToken);
        if (importedPrompt is null)
        {
            await IncrementFailedAsync(job, cancellationToken);
            return;
        }

        var babbleExists = await _babbleRepository.GetByIdAsync(job.UserId, importedPrompt.BabbleId, cancellationToken);
        if (babbleExists is null)
        {
            await IncrementFailedAsync(job, cancellationToken);
            return;
        }

        var existing = await _generatedPromptRepository.GetByIdAsync(importedPrompt.BabbleId, importedPrompt.Id, cancellationToken);
        if (existing is not null && !job.OverwriteExisting)
        {
            await UpdateCountsAsync(job, counts => counts with { GeneratedPromptsSkipped = counts.GeneratedPromptsSkipped + 1 }, cancellationToken);
            return;
        }

        if (existing is not null)
        {
            await _generatedPromptRepository.DeleteAsync(importedPrompt.BabbleId, importedPrompt.Id, cancellationToken);
        }

        var promptToSave = importedPrompt with { UserId = job.UserId };
        await _generatedPromptRepository.CreateAsync(promptToSave, cancellationToken);
        await UpdateCountsAsync(job, counts => counts with { GeneratedPromptsImported = counts.GeneratedPromptsImported + 1 }, cancellationToken);
    }

    private async Task ImportTemplateAsync(ZipArchiveEntry entry, ImportExportJob job, CancellationToken cancellationToken)
    {
        var importedTemplate = await DeserializeEntryAsync<PromptTemplate>(entry, cancellationToken);
        if (importedTemplate is null)
        {
            await IncrementFailedAsync(job, cancellationToken);
            return;
        }

        if (importedTemplate.IsBuiltIn)
        {
            await UpdateCountsAsync(job, counts => counts with { TemplatesSkipped = counts.TemplatesSkipped + 1 }, cancellationToken);
            return;
        }

        var existing = await _promptTemplateRepository.GetByIdAsync(job.UserId, importedTemplate.Id, cancellationToken);
        if (existing is not null && !job.OverwriteExisting)
        {
            await UpdateCountsAsync(job, counts => counts with { TemplatesSkipped = counts.TemplatesSkipped + 1 }, cancellationToken);
            return;
        }

        var templateToSave = importedTemplate with
        {
            UserId = job.UserId,
            IsBuiltIn = false,
        };

        if (existing is null)
        {
            await _promptTemplateRepository.CreateAsync(templateToSave, cancellationToken);
        }
        else
        {
            await _promptTemplateRepository.UpdateAsync(templateToSave, cancellationToken);
        }

        await UpdateCountsAsync(job, counts => counts with { TemplatesImported = counts.TemplatesImported + 1 }, cancellationToken);
    }

    private async Task<ExportManifest> ReadManifestAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        var manifestEntry = archive.GetEntry("manifest.json");
        if (manifestEntry is null)
        {
            throw new InvalidOperationException("Import ZIP is missing manifest.json.");
        }

        await using var manifestStream = manifestEntry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<ExportManifest>(manifestStream, _jsonSerializerOptions, cancellationToken);
        return manifest ?? throw new InvalidOperationException("Import ZIP contains an invalid manifest.json file.");
    }

    private async Task<T?> DeserializeEntryAsync<T>(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        await using var stream = entry.Open();
        return await JsonSerializer.DeserializeAsync<T>(stream, _jsonSerializerOptions, cancellationToken);
    }

    private async Task IncrementFailedAsync(ImportExportJob job, CancellationToken cancellationToken)
    {
        await UpdateCountsAsync(job, counts => counts with { Failed = counts.Failed + 1 }, cancellationToken);
    }

    private async Task UpdateCountsAsync(ImportExportJob job, Func<ImportExportCounts, ImportExportCounts> update, CancellationToken cancellationToken)
    {
        var latest = await _jobRepository.GetByIdAsync(job.UserId, job.Id, cancellationToken);
        if (latest is null)
        {
            return;
        }

        var updated = latest with { Counts = update(latest.Counts ?? new ImportExportCounts()) };
        await _jobRepository.UpdateAsync(updated, cancellationToken);
    }
}
