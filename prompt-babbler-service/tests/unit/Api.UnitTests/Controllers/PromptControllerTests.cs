using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Domain.Interfaces;

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
}
