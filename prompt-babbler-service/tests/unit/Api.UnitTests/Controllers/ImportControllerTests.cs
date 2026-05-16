using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Constants;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class ImportControllerTests
{
    private const string TestUserId = "00000000-0000-0000-0000-000000000000";

    private readonly IImportService _importService = Substitute.For<IImportService>();
    private readonly IImportExportJobRepository _jobRepository = Substitute.For<IImportExportJobRepository>();
    private readonly ILogger<ImportController> _logger = Substitute.For<ILogger<ImportController>>();
    private readonly ImportController _controller;

    public ImportControllerTests()
    {
        _controller = new ImportController(_importService, _jobRepository, _logger);

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
    public async Task StartImport_NullFile_ReturnsBadRequest()
    {
        var result = await _controller.StartImport(null!, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task StartImport_EmptyFile_ReturnsBadRequest()
    {
        var file = CreateFormFile("empty.zip", []);

        var result = await _controller.StartImport(file, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task StartImport_NonZipExtension_ReturnsBadRequest()
    {
        var file = CreateFormFile("payload.txt", [1, 2, 3]);

        var result = await _controller.StartImport(file, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task StartImport_FileExceedsLimit_ReturnsBadRequest()
    {
        var file = Substitute.For<IFormFile>();
        file.Length.Returns(ExportLimits.MaxZipBytes + 1);
        file.FileName.Returns("large.zip");

        var result = await _controller.StartImport(file, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task StartImport_ValidZip_ReturnsAcceptedWithJobId()
    {
        var file = CreateFormFile("payload.zip", [1, 2, 3, 4]);
        string? capturedPath = null;

        _importService.StartImportAsync(TestUserId, Arg.Any<string>(), true, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedPath = call.ArgAt<string>(1);
                return "import-job-1";
            });

        try
        {
            var result = await _controller.StartImport(file, true, CancellationToken.None);

            var accepted = result.Should().BeOfType<AcceptedResult>().Subject;
            var jobId = accepted.Value?.GetType().GetProperty("jobId")?.GetValue(accepted.Value) as string;
            jobId.Should().Be("import-job-1");
            capturedPath.Should().NotBeNullOrWhiteSpace();
            File.Exists(capturedPath!).Should().BeTrue();
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(capturedPath) && File.Exists(capturedPath))
            {
                File.Delete(capturedPath);
            }
        }
    }

    [TestMethod]
    public async Task StartImport_ServiceValidationFailure_ReturnsBadRequest()
    {
        var file = CreateFormFile("payload.zip", [1]);
        string? capturedPath = null;

        _importService.StartImportAsync(
                TestUserId,
                Arg.Do<string>(path => capturedPath = path),
                false,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("invalid import"));

        try
        {
            var result = await _controller.StartImport(file, false, CancellationToken.None);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("invalid import");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(capturedPath) && File.Exists(capturedPath))
            {
                File.Delete(capturedPath);
            }
        }
    }

    [TestMethod]
    public async Task GetImportJob_NotFound_ReturnsNotFound()
    {
        _jobRepository.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((ImportExportJob?)null);

        var result = await _controller.GetImportJob("missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetImportJob_ExistingImportJob_ReturnsOk()
    {
        _jobRepository.GetByIdAsync(TestUserId, "job-1", Arg.Any<CancellationToken>())
            .Returns(CreateImportJob("job-1", JobStatus.Running));

        var result = await _controller.GetImportJob("job-1", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ImportExportJobResponse>();
    }

    private static ImportExportJob CreateImportJob(string id, JobStatus status)
    {
        return new ImportExportJob
        {
            Id = id,
            UserId = TestUserId,
            JobType = ImportExportJobType.Import,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            StartedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = status == JobStatus.Completed ? 100 : 50,
            CurrentStage = status == JobStatus.Completed ? "Completed" : "Running",
            TotalItems = 1,
            ProcessedItems = status == JobStatus.Completed ? 1 : 0,
            OverwriteExisting = true,
            Counts = new ImportExportCounts(),
        };
    }

    private static IFormFile CreateFormFile(string fileName, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.LongLength, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/zip",
        };
    }
}
