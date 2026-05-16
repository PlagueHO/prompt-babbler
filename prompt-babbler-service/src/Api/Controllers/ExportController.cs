using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using PromptBabbler.Api.Extensions;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Authorize]
[RequiredScope("access_as_user")]
[Route("api/exports")]
public sealed class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IImportExportJobRepository _jobRepository;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IExportService exportService,
        IImportExportJobRepository jobRepository,
        ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> StartExport([FromBody] StartExportRequest request, CancellationToken cancellationToken = default)
    {
        var selection = new ExportSelection
        {
            IncludeBabbles = request.IncludeBabbles,
            IncludeGeneratedPrompts = request.IncludeGeneratedPrompts,
            IncludeUserTemplates = request.IncludeUserTemplates,
            IncludeSemanticVectors = request.IncludeSemanticVectors,
        };

        if (!selection.IncludeBabbles && !selection.IncludeGeneratedPrompts && !selection.IncludeUserTemplates)
        {
            return BadRequest("At least one export data type must be selected.");
        }

        var userId = User.GetUserIdOrAnonymous();
        string jobId;
        try
        {
            jobId = await _exportService.StartExportAsync(userId, selection, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        _logger.LogInformation("Queued export job {JobId} for user {UserId}", jobId, userId);
        return Accepted(new { jobId });
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetExportJob(string jobId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();
        var job = await _jobRepository.GetByIdAsync(userId, jobId, cancellationToken);

        if (job is null || job.JobType != ImportExportJobType.Export)
        {
            return NotFound();
        }

        return Ok(ToResponse(job));
    }

    [HttpGet("{jobId}/download")]
    public async Task<IActionResult> DownloadExport(string jobId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();
        var job = await _jobRepository.GetByIdAsync(userId, jobId, cancellationToken);

        if (job is null || job.JobType != ImportExportJobType.Export)
        {
            return NotFound();
        }

        if (job.Status != JobStatus.Completed || string.IsNullOrWhiteSpace(job.ResultFilePath))
        {
            return Conflict("Export is not ready for download yet.");
        }

        if (!System.IO.File.Exists(job.ResultFilePath))
        {
            return NotFound("Export file was not found. Please run export again.");
        }

        var stream = new FileStream(job.ResultFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = $"prompt-babbler-export-{job.CreatedAt:yyyyMMdd-HHmmss}.zip";
        return File(stream, "application/zip", fileName);
    }

    private static ImportExportJobResponse ToResponse(ImportExportJob job)
    {
        return new ImportExportJobResponse
        {
            Id = job.Id,
            JobType = job.JobType.ToString(),
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt.ToString("o"),
            StartedAt = job.StartedAt?.ToString("o"),
            CompletedAt = job.CompletedAt?.ToString("o"),
            ProgressPercentage = job.ProgressPercentage,
            CurrentStage = job.CurrentStage,
            TotalItems = job.TotalItems,
            ProcessedItems = job.ProcessedItems,
            ErrorMessage = job.ErrorMessage,
            OverwriteExisting = job.OverwriteExisting,
            ExportSelection = job.ExportSelection is null
                ? null
                : new ExportSelectionResponse
                {
                    IncludeBabbles = job.ExportSelection.IncludeBabbles,
                    IncludeGeneratedPrompts = job.ExportSelection.IncludeGeneratedPrompts,
                    IncludeUserTemplates = job.ExportSelection.IncludeUserTemplates,
                    IncludeSemanticVectors = job.ExportSelection.IncludeSemanticVectors,
                },
            Counts = job.Counts is null
                ? null
                : new ImportExportJobCountsResponse
                {
                    BabblesImported = job.Counts.BabblesImported,
                    BabblesSkipped = job.Counts.BabblesSkipped,
                    GeneratedPromptsImported = job.Counts.GeneratedPromptsImported,
                    GeneratedPromptsSkipped = job.Counts.GeneratedPromptsSkipped,
                    TemplatesImported = job.Counts.TemplatesImported,
                    TemplatesSkipped = job.Counts.TemplatesSkipped,
                    Failed = job.Counts.Failed,
                },
        };
    }
}
