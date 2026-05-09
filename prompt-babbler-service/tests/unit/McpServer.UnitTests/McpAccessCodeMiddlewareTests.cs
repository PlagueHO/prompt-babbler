using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PromptBabbler.McpServer;

namespace PromptBabbler.McpServer.UnitTests;

[TestClass]
[TestCategory("Unit")]
public sealed class McpAccessCodeMiddlewareTests
{
    private bool _nextCalled;

    [TestMethod]
    public async Task InvokeAsync_WhenAccessCodeNotConfigured_ShouldPassThrough()
    {
        var middleware = CreateMiddleware(string.Empty);
        var context = CreateHttpContext("/mcp");

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("/health")]
    [DataRow("/health/ready")]
    [DataRow("/alive")]
    [DataRow("/Alive")]
    public async Task InvokeAsync_AllowlistedPaths_ShouldPassThrough(string path)
    {
        var middleware = CreateMiddleware("secret123");
        var context = CreateHttpContext(path);

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenProtectedPathAndHeaderMissing_ShouldReturn401()
    {
        var middleware = CreateMiddleware("secret123");
        var context = CreateHttpContext("/mcp");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _nextCalled.Should().BeFalse();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenProtectedPathAndHeaderWrong_ShouldReturn401()
    {
        var middleware = CreateMiddleware("secret123");
        var context = CreateHttpContext("/mcp", "Bearer wrong");

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _nextCalled.Should().BeFalse();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenProtectedPathAndHeaderMatches_ShouldPassThrough()
    {
        var middleware = CreateMiddleware("secret123");
        var context = CreateHttpContext("/mcp", "Bearer secret123");

        await middleware.InvokeAsync(context);

        _nextCalled.Should().BeTrue();
    }

    private McpAccessCodeMiddleware CreateMiddleware(string accessCode)
    {
        _nextCalled = false;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessControl:AccessCode"] = accessCode,
            })
            .Build();

        return new McpAccessCodeMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        }, configuration);
    }

    private static DefaultHttpContext CreateHttpContext(string path, string? authorizationHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (authorizationHeader is not null)
        {
            context.Request.Headers.Authorization = authorizationHeader;
        }

        return context;
    }
}