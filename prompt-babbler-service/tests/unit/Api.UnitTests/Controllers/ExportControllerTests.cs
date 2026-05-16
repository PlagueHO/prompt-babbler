using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class ExportControllerTests
{
    private const string TestUserId = "00000000-0000-0000-0000-000000000000";

    private readonly IExportService _exportService = Substitute.For<IExportService>();
    private readonly IImportExportJobRepository _jobRepository = Substitute.For<IImportExportJobRepository>();
    private readonly ILogger<ExportController> _logger = Substitute.For<ILogger<ExportController>>();
    private readonly ExportController _controller;

    public ExportControllerTests()
    {
        _controller = new ExportController(_exportService, _jobRepository, _logger);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", TestUserId),
            new Claim("preferred_username", "test@contoso.com"),
        ], "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };
    }

    [TestMethod]
    public async Task StartExport_NoDataTypesSelected_ReturnsBadRequest()
    {
        var request = new StartExportRequest();

        var result = await _controller.StartExport(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task StartExport_ServiceValidationFailure_ReturnsBadRequest()
    {
        var request = new StartExportRequest
        {
            IncludeBabbles = true,
        };

        _exportService.StartExportAsync(TestUserId, Arg.Any<ExportSelection>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("limit exceeded"));

        var result = await _controller.StartExport(request, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be("limit exceeded");
    }

    [TestMethod]
    public async Task StartExport_ValidRequest_ReturnsAcceptedWithJobId()
    {
        var request = new StartExportRequest
        {
            IncludeBabbles = true,
            IncludeSemanticVectors = false,
        };

        _exportService.StartExportAsync(TestUserId, Arg.Any<ExportSelection>(), Arg.Any<CancellationToken>())
            .Returns("job-123");

        var result = await _controller.StartExport(request, CancellationToken.None);

        var accepted = result.Should().BeOfType<AcceptedResult>().Subject;
        var jobId = accepted.Value?.GetType().GetProperty("jobId")?.GetValue(accepted.Value) as string;
        jobId.Should().Be("job-123");
    }

    [TestMethod]
    public async Task GetExportJob_NotFound_ReturnsNotFound()
    {
        _jobRepository.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((ImportExportJob?)null);

        var result = await _controller.GetExportJob("missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetExportJob_ExistingExportJob_ReturnsOk()
    {
        _jobRepository.GetByIdAsync(TestUserId, "job-1", Arg.Any<CancellationToken>())
            .Returns(CreateExportJob("job-1", JobStatus.Running));

        var result = await _controller.GetExportJob("job-1", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ImportExportJobResponse>();
    }

    [TestMethod]
    public async Task DownloadExport_NotCompleted_ReturnsConflict()
    {
        _jobRepository.GetByIdAsync(TestUserId, "job-1", Arg.Any<CancellationToken>())
            .Returns(CreateExportJob("job-1", JobStatus.Running));

        var result = await _controller.DownloadExport("job-1", CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [TestMethod]
    public async Task DownloadExport_CompletedButMissingFile_ReturnsNotFound()
    {
        _jobRepository.GetByIdAsync(TestUserId, "job-1", Arg.Any<CancellationToken>())
            .Returns(CreateExportJob("job-1", JobStatus.Completed, resultFilePath: Path.Combine(Path.GetTempPath(), "does-not-exist.zip")));

        var result = await _controller.DownloadExport("job-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [TestMethod]
    public async Task DownloadExport_CompletedWithFile_ReturnsFileResult()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid():N}.zip");
        await File.WriteAllBytesAsync(tempPath, [1, 2, 3], CancellationToken.None);

        try
        {
            _jobRepository.GetByIdAsync(TestUserId, "job-1", Arg.Any<CancellationToken>())
                .Returns(CreateExportJob("job-1", JobStatus.Completed, resultFilePath: tempPath));

            var result = await _controller.DownloadExport("job-1", CancellationToken.None);

            var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
            fileResult.FileStream.Dispose();
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static ImportExportJob CreateExportJob(string id, JobStatus status, string? resultFilePath = null)
    {
        return new ImportExportJob
        {
            Id = id,
            UserId = TestUserId,
            JobType = ImportExportJobType.Export,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            StartedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = status == JobStatus.Completed ? 100 : 50,
            CurrentStage = status == JobStatus.Completed ? "Completed" : "Running",
            TotalItems = 1,
            ProcessedItems = status == JobStatus.Completed ? 1 : 0,
            ResultFilePath = resultFilePath,
            OverwriteExisting = false,
        };
    }
}
