using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class BuiltInTemplateSeedingServiceTests
{
    private readonly IPromptTemplateRepository _repository = Substitute.For<IPromptTemplateRepository>();
    private readonly ILogger<BuiltInTemplateSeedingService> _logger = Substitute.For<ILogger<BuiltInTemplateSeedingService>>();
    private readonly BuiltInTemplateSeedingService _service;

    public BuiltInTemplateSeedingServiceTests()
    {
        _service = new BuiltInTemplateSeedingService(_repository, _logger);
    }

    [TestMethod]
    public async Task StartAsync_WhenCosmosAvailable_UpsertAllTemplates()
    {
        var templates = BuiltInTemplateSeedingService.GetBuiltInTemplates();
        _repository.UpsertAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _service.StartAsync(CancellationToken.None);

        await _repository.Received(templates.Count)
            .UpsertAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task StartAsync_WhenTransient503OnFirstAttempt_RetriesAndSucceeds()
    {
        var serviceUnavailable = new CosmosException(
            "pgcosmos extension is still starting; retry request shortly",
            HttpStatusCode.ServiceUnavailable, 0, string.Empty, 0);

        // First call to UpsertAsync throws 503; subsequent calls succeed.
        _repository.UpsertAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw serviceUnavailable, _ => Task.CompletedTask);

        var act = async () => await _service.StartAsync(CancellationToken.None);

        // Retries absorb the transient failure — should not throw.
        await act.Should().NotThrowAsync();

        // Total calls = one extra retry on template 1 + one call per remaining template.
        var templateCount = BuiltInTemplateSeedingService.GetBuiltInTemplates().Count;
        await _repository.Received(templateCount + 1)
            .UpsertAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task StartAsync_WhenCancelledDuringRetryDelay_ThrowsOperationCancelled()
    {
        // Always return 503 so the service tries to wait before the next retry.
        var serviceUnavailable = new CosmosException(
            "pgcosmos extension is still starting; retry request shortly",
            HttpStatusCode.ServiceUnavailable, 0, string.Empty, 0);
        _repository.UpsertAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(serviceUnavailable);

        // Pre-cancel the token so Task.Delay throws immediately when reached.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _service.StartAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task StartAsync_WhenNonTransientCosmosException_ThrowsImmediately()
    {
        var notFound = new CosmosException(
            "Container not found",
            HttpStatusCode.NotFound, 0, string.Empty, 0);

        _repository.UpsertAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(notFound);

        var act = async () => await _service.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<CosmosException>()
            .WithMessage("*Container not found*");

        // Must not retry for non-transient errors.
        await _repository.Received(1)
            .UpsertAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>());
    }
}
