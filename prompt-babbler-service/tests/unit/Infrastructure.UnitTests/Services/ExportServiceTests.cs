using FluentAssertions;
using NSubstitute;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Constants;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class ExportServiceTests
{
    private readonly IImportExportJobRepository _jobRepository = Substitute.For<IImportExportJobRepository>();
    private readonly IImportExportJobQueue _jobQueue = Substitute.For<IImportExportJobQueue>();
    private readonly IBabbleRepository _babbleRepository = Substitute.For<IBabbleRepository>();
    private readonly IGeneratedPromptRepository _generatedPromptRepository = Substitute.For<IGeneratedPromptRepository>();
    private readonly IPromptTemplateRepository _promptTemplateRepository = Substitute.For<IPromptTemplateRepository>();

    private readonly ExportService _service;

    public ExportServiceTests()
    {
        _service = new ExportService(
            _jobRepository,
            _jobQueue,
            _babbleRepository,
            _generatedPromptRepository,
            _promptTemplateRepository);

        _jobRepository.CreateAsync(Arg.Any<ImportExportJob>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<ImportExportJob>());
    }

    [TestMethod]
    public async Task StartExportAsync_NoSelections_ThrowsInvalidOperationException()
    {
        var selection = new ExportSelection();

        var act = () => _service.StartExportAsync("user-1", selection, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*At least one export data type must be selected*");
    }

    [TestMethod]
    public async Task StartExportAsync_BabbleCountExceedsLimit_ThrowsInvalidOperationException()
    {
        _babbleRepository.CountByUserAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(ExportLimits.MaxBabbles + 1);

        var selection = new ExportSelection
        {
            IncludeBabbles = true,
        };

        var act = () => _service.StartExportAsync("user-1", selection, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Babbles export limit exceeded*");
    }

    [TestMethod]
    public async Task StartExportAsync_ValidSelection_CreatesQueuedJobAndEnqueuesWork()
    {
        _generatedPromptRepository.CountByUserAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(42);

        ImportExportJob? createdJob = null;
        _jobRepository.CreateAsync(Arg.Do<ImportExportJob>(job => createdJob = job), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<ImportExportJob>());

        var selection = new ExportSelection
        {
            IncludeGeneratedPrompts = true,
            IncludeSemanticVectors = false,
        };

        var jobId = await _service.StartExportAsync("user-1", selection, CancellationToken.None);

        jobId.Should().NotBeNullOrWhiteSpace();
        createdJob.Should().NotBeNull();
        createdJob!.Id.Should().Be(jobId);
        createdJob.UserId.Should().Be("user-1");
        createdJob.JobType.Should().Be(ImportExportJobType.Export);
        createdJob.Status.Should().Be(JobStatus.Queued);
        createdJob.ExportSelection.Should().NotBeNull();
        createdJob.ExportSelection!.IncludeGeneratedPrompts.Should().BeTrue();
        createdJob.ExportSelection.IncludeSemanticVectors.Should().BeFalse();

        await _jobQueue.Received(1).EnqueueAsync(
            Arg.Is<ImportExportJobQueueItem>(item => item.JobId == jobId && item.UserId == "user-1"),
            Arg.Any<CancellationToken>());
    }
}
