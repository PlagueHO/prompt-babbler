using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class StatusControllerTests
{
    private readonly IDependencyChecker _dependencyChecker = Substitute.For<IDependencyChecker>();
    private readonly IHostEnvironment _environment = Substitute.For<IHostEnvironment>();
    private readonly ILogger<StatusController> _logger = Substitute.For<ILogger<StatusController>>();
    private readonly StatusController _controller;

    public StatusControllerTests()
    {
        _environment.EnvironmentName.Returns("Production");
        _controller = new StatusController(_dependencyChecker, _environment, _logger);
    }

    private static DependencyStatus HealthyStatus(string message = "OK") => new()
    {
        Status = DependencyHealth.Healthy,
        Message = message,
        DurationMs = 10,
    };

    private static DependencyStatus UnhealthyStatus(string message = "Failed") => new()
    {
        Status = DependencyHealth.Unhealthy,
        Message = message,
        Error = "SomeException: something went wrong",
        DurationMs = 500,
    };

    private static DependencyStatus DegradedStatus(string message = "Degraded") => new()
    {
        Status = DependencyHealth.Degraded,
        Message = message,
        DurationMs = 0,
    };

    [TestMethod]
    public async Task GetStatusAsync_AllDependenciesHealthy_Returns200WithHealthyOverall()
    {
        _dependencyChecker.CheckManagedIdentityAsync(Arg.Any<CancellationToken>())
            .Returns(HealthyStatus("Managed identity token acquired"));
        _dependencyChecker.CheckCosmosDbAsync(Arg.Any<CancellationToken>())
            .Returns(HealthyStatus("Cosmos DB reachable"));
        _dependencyChecker.CheckAiFoundry()
            .Returns(HealthyStatus("IChatClient registered"));

        var result = await _controller.GetStatusAsync(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<StatusResponse>().Subject;
        response.Overall.Should().Be(DependencyHealth.Healthy);
        response.ManagedIdentity.Status.Should().Be(DependencyHealth.Healthy);
        response.CosmosDb.Status.Should().Be(DependencyHealth.Healthy);
        response.AiFoundry.Status.Should().Be(DependencyHealth.Healthy);
        response.Environment.Should().Be("Production");
    }

    [TestMethod]
    public async Task GetStatusAsync_CosmosDbUnhealthy_Returns503WithUnhealthyOverall()
    {
        _dependencyChecker.CheckManagedIdentityAsync(Arg.Any<CancellationToken>())
            .Returns(HealthyStatus());
        _dependencyChecker.CheckCosmosDbAsync(Arg.Any<CancellationToken>())
            .Returns(UnhealthyStatus("Cosmos DB is unreachable"));
        _dependencyChecker.CheckAiFoundry()
            .Returns(HealthyStatus());

        var result = await _controller.GetStatusAsync(CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
        var response = objectResult.Value.Should().BeOfType<StatusResponse>().Subject;
        response.Overall.Should().Be(DependencyHealth.Unhealthy);
        response.CosmosDb.Status.Should().Be(DependencyHealth.Unhealthy);
    }

    [TestMethod]
    public async Task GetStatusAsync_ManagedIdentityUnhealthy_Returns503WithUnhealthyOverall()
    {
        _dependencyChecker.CheckManagedIdentityAsync(Arg.Any<CancellationToken>())
            .Returns(UnhealthyStatus("Managed identity not available"));
        _dependencyChecker.CheckCosmosDbAsync(Arg.Any<CancellationToken>())
            .Returns(HealthyStatus());
        _dependencyChecker.CheckAiFoundry()
            .Returns(HealthyStatus());

        var result = await _controller.GetStatusAsync(CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
        var response = objectResult.Value.Should().BeOfType<StatusResponse>().Subject;
        response.Overall.Should().Be(DependencyHealth.Unhealthy);
        response.ManagedIdentity.Status.Should().Be(DependencyHealth.Unhealthy);
    }

    [TestMethod]
    public async Task GetStatusAsync_AiFoundryDegradedButOthersHealthy_Returns200WithHealthyOverall()
    {
        _dependencyChecker.CheckManagedIdentityAsync(Arg.Any<CancellationToken>())
            .Returns(HealthyStatus());
        _dependencyChecker.CheckCosmosDbAsync(Arg.Any<CancellationToken>())
            .Returns(HealthyStatus());
        _dependencyChecker.CheckAiFoundry()
            .Returns(DegradedStatus("IChatClient not registered"));

        var result = await _controller.GetStatusAsync(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<StatusResponse>().Subject;
        response.Overall.Should().Be(DependencyHealth.Healthy);
        response.AiFoundry.Status.Should().Be(DependencyHealth.Degraded);
    }

    [TestMethod]
    public async Task GetStatusAsync_MultipleDependenciesUnhealthy_Returns503()
    {
        _dependencyChecker.CheckManagedIdentityAsync(Arg.Any<CancellationToken>())
            .Returns(UnhealthyStatus("Managed identity failed"));
        _dependencyChecker.CheckCosmosDbAsync(Arg.Any<CancellationToken>())
            .Returns(UnhealthyStatus("Cosmos DB failed"));
        _dependencyChecker.CheckAiFoundry()
            .Returns(DegradedStatus());

        var result = await _controller.GetStatusAsync(CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
        var response = objectResult.Value.Should().BeOfType<StatusResponse>().Subject;
        response.Overall.Should().Be(DependencyHealth.Unhealthy);
        response.ManagedIdentity.Status.Should().Be(DependencyHealth.Unhealthy);
        response.CosmosDb.Status.Should().Be(DependencyHealth.Unhealthy);
    }

    [TestMethod]
    public async Task GetStatusAsync_TimestampIsApproximatelyNow()
    {
        _dependencyChecker.CheckManagedIdentityAsync(Arg.Any<CancellationToken>())
            .Returns(HealthyStatus());
        _dependencyChecker.CheckCosmosDbAsync(Arg.Any<CancellationToken>())
            .Returns(HealthyStatus());
        _dependencyChecker.CheckAiFoundry()
            .Returns(HealthyStatus());

        var before = DateTimeOffset.UtcNow;
        var result = await _controller.GetStatusAsync(CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<StatusResponse>().Subject;
        response.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
