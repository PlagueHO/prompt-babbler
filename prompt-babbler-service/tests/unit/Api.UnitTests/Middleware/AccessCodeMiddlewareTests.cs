using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PromptBabbler.Api.Middleware;
using PromptBabbler.Domain.Configuration;

namespace PromptBabbler.Api.UnitTests.Middleware;

[TestClass]
[TestCategory("Unit")]
public sealed class AccessCodeMiddlewareTests
{
    private readonly ILogger<AccessCodeMiddleware> _logger = Substitute.For<ILogger<AccessCodeMiddleware>>();
    private bool _nextCalled;

    private AccessCodeMiddleware CreateMiddleware()
    {
        return new AccessCodeMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        }, _logger);
    }

    private static IOptionsMonitor<AccessControlOptions> CreateOptions(string? accessCode)
    {
        var monitor = Substitute.For<IOptionsMonitor<AccessControlOptions>>();
        monitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = accessCode });
        return monitor;
    }

    private static DefaultHttpContext CreateHttpContext(string path, string? accessCodeHeader = null, string? accessCodeQuery = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (accessCodeHeader is not null)
        {
            context.Request.Headers["X-Access-Code"] = accessCodeHeader;
        }

        if (accessCodeQuery is not null)
        {
            context.Request.QueryString = new QueryString($"?access_code={accessCodeQuery}");
        }

        return context;
    }

    [TestMethod]
    public async Task InvokeAsync_WhenAccessCodeIsNull_ShouldPassThrough()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/babbles");

        await middleware.InvokeAsync(context, CreateOptions(null));

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenAccessCodeIsEmpty_ShouldPassThrough()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/babbles");

        await middleware.InvokeAsync(context, CreateOptions(string.Empty));

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenValidCodeProvided_ShouldPassThrough()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/babbles", "secret123");

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenHeaderMissing_ShouldReturn401()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/babbles");

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _nextCalled.Should().BeFalse();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenWrongCode_ShouldReturn401()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/babbles", "wrongcode");

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _nextCalled.Should().BeFalse();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenUnauthorized_ShouldReturnJsonErrorBody()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/babbles", "wrongcode");

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        var errorDoc = JsonDocument.Parse(body);
        errorDoc.RootElement.GetProperty("error").GetString().Should().Be("Access code required");
        context.Response.ContentType.Should().Be("application/json");
    }

    [TestMethod]
    [DataRow("/health")]
    [DataRow("/health/ready")]
    [DataRow("/alive")]
    [DataRow("/api/config/access-status")]
    [DataRow("/api/error")]
    [DataRow("/openapi")]
    [DataRow("/openapi/v1.json")]
    public async Task InvokeAsync_AllowlistedPaths_ShouldPassThrough(string path)
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(path);

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("/Health")]
    [DataRow("/ALIVE")]
    [DataRow("/API/CONFIG/ACCESS-STATUS")]
    public async Task InvokeAsync_AllowlistedPaths_ShouldBeCaseInsensitive(string path)
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(path);

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("/api/babbles")]
    [DataRow("/api/templates")]
    [DataRow("/api/user")]
    public async Task InvokeAsync_ProtectedPaths_ShouldBeBlocked(string path)
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(path);

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _nextCalled.Should().BeFalse();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenValidCodeInQueryString_ShouldPassThrough()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/transcribe/stream", accessCodeQuery: "secret123");

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenWrongCodeInQueryString_ShouldReturn401()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/transcribe/stream", accessCodeQuery: "wrongcode");

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _nextCalled.Should().BeFalse();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenHeaderPresent_ShouldPreferHeaderOverQueryString()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/babbles", accessCodeHeader: "secret123", accessCodeQuery: "wrongcode");

        await middleware.InvokeAsync(context, CreateOptions("secret123"));

        _nextCalled.Should().BeTrue();
    }
}
