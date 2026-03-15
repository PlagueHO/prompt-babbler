using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
public sealed class PromptControllerTests
{
    private readonly IPromptGenerationService _promptService = Substitute.For<IPromptGenerationService>();
    private readonly ILogger<PromptController> _logger = Substitute.For<ILogger<PromptController>>();
    private readonly PromptController _controller;

    public PromptControllerTests()
    {
        _controller = new PromptController(_promptService, _logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GeneratePrompt_WithEmptyBabbleText_Returns400()
    {
        var request = new GeneratePromptRequest
        {
            BabbleText = "",
            SystemPrompt = "You are a helpful assistant.",
        };

        await _controller.GeneratePrompt(request, CancellationToken.None);

        _controller.HttpContext.Response.StatusCode.Should().Be(400);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GeneratePrompt_WithEmptySystemPrompt_Returns400()
    {
        var request = new GeneratePromptRequest
        {
            BabbleText = "Some babble text",
            SystemPrompt = "",
        };

        await _controller.GeneratePrompt(request, CancellationToken.None);

        _controller.HttpContext.Response.StatusCode.Should().Be(400);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GeneratePrompt_WithTemplateName_UsesStructuredPath()
    {
        var request = new GeneratePromptRequest
        {
            BabbleText = "I want to build a REST API",
            SystemPrompt = "You are a prompt engineer.",
            TemplateName = "GitHub Copilot Prompt",
        };

        _promptService.GenerateStructuredPromptAsync(
            request.BabbleText,
            request.SystemPrompt,
            request.TemplateName,
            request.PromptFormat,
            request.AllowEmojis,
            Arg.Any<CancellationToken>())
            .Returns(new StructuredPromptResult
            {
                Name = "REST API Design",
                Prompt = "Create a REST API with CRUD endpoints.",
            });

        await _controller.GeneratePrompt(request, CancellationToken.None);

        _controller.HttpContext.Response.ContentType.Should().Be("text/event-stream");

        _controller.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(_controller.HttpContext.Response.Body).ReadToEndAsync();
        body.Should().Contain("\"name\":\"REST API Design\"");
        body.Should().Contain("\"text\":\"Create a REST API with CRUD endpoints.\"");
        body.Should().Contain("data: [DONE]");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GeneratePrompt_WithoutTemplateName_UsesStreamingPath()
    {
        var request = new GeneratePromptRequest
        {
            BabbleText = "Some babble text",
            SystemPrompt = "You are a helpful assistant.",
        };

        _promptService.GeneratePromptStreamAsync(
            request.BabbleText,
            request.SystemPrompt,
            request.PromptFormat,
            request.AllowEmojis,
            Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable("Hello", " world"));

        await _controller.GeneratePrompt(request, CancellationToken.None);

        _controller.HttpContext.Response.ContentType.Should().Be("text/event-stream");

        _controller.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(_controller.HttpContext.Response.Body).ReadToEndAsync();
        body.Should().Contain("\"text\":\"Hello\"");
        body.Should().Contain("\"text\":\" world\"");
        body.Should().Contain("data: [DONE]");
    }

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(params string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
