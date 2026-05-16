using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using PromptBabbler.Api.Extensions;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Constants;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Authorize]
[RequiredScope("access_as_user")]
[Route("api/imports")]
public sealed class ImportController : ControllerBase
{
    private readonly IImportService _importService;
    private readonly IImportExportJobRepository _jobRepository;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        IImportService importService,
        IImportExportJobRepository jobRepository,
        ILogger<ImportController> logger)
    {
        _importService = importService;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = ExportLimits.MaxZipBytes)]
    public async Task<IActionResult> StartImport(
        IFormFile file,
        [FromQuery] bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A ZIP file is required.");
        }

        if (!Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only .zip files are supported for import.");
        }

        if (file.Length > ExportLimits.MaxZipBytes)
        {
            return BadRequest($"ZIP file exceeds the maximum allowed size of {ExportLimits.MaxZipBytes} bytes.");
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"prompt-babbler-import-{Guid.NewGuid():N}.zip");

        await using (var stream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var userId = User.GetUserIdOrAnonymous();
        string jobId;
        try
        {
            jobId = await _importService.StartImportAsync(userId, tempFilePath, overwrite, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        _logger.LogInformation("Queued import job {JobId} for user {UserId}", jobId, userId);
        return Accepted(new { jobId });
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetImportJob(string jobId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();
        var job = await _jobRepository.GetByIdAsync(userId, jobId, cancellationToken);

        if (job is null || job.JobType != ImportExportJobType.Import)
        {
            return NotFound();
        }

        return Ok(ToResponse(job));
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
