using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PromptBabbler.McpServer.HealthChecks;

namespace PromptBabbler.McpServer.UnitTests.HealthChecks;

[TestClass]
[TestCategory("Unit")]
public sealed class PromptBabblerApiHealthCheckTests
{
    [TestMethod]
    public async Task CheckHealthAsync_WhenApiReturns200_ReturnsHealthy()
    {
        var client = CreateClient(HttpStatusCode.OK);
        var healthCheck = new PromptBabblerApiHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenApiReturns503_ReturnsUnhealthy()
    {
        var client = CreateClient(HttpStatusCode.ServiceUnavailable);
        var healthCheck = new PromptBabblerApiHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("503");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenApiReturns404_ReturnsUnhealthy()
    {
        var client = CreateClient(HttpStatusCode.NotFound);
        var healthCheck = new PromptBabblerApiHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("404");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenApiThrowsHttpRequestException_ReturnsUnhealthy()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://api") };
        var healthCheck = new PromptBabblerApiHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("unreachable");
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenApiThrowsTaskCanceledException_ReturnsUnhealthy()
    {
        var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("Request timed out"));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://api") };
        var healthCheck = new PromptBabblerApiHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("unreachable");
        result.Exception.Should().BeOfType<TaskCanceledException>();
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler(statusCode);
        return new HttpClient(handler) { BaseAddress = new Uri("http://api") };
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }
}
