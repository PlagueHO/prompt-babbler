using System.Security.Claims;
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
    private readonly IPromptTemplateService _templateService = Substitute.For<IPromptTemplateService>();
    private readonly ILogger<PromptController> _logger = Substitute.For<ILogger<PromptController>>();
    private readonly PromptController _controller;

    public PromptControllerTests()
    {
        _controller = new PromptController(_promptService, _templateService, _logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "00000000-0000-0000-0000-000000000000"),
            new Claim("preferred_username", "test@contoso.com"),
        ], "TestAuth"));
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GeneratePrompt_WithEmptyBabbleText_Returns400()
    {
        var request = new GeneratePromptRequest
        {
            BabbleText = "",
            TemplateId = "template-1",
        };

        await _controller.GeneratePrompt(request, CancellationToken.None);

        _controller.HttpContext.Response.StatusCode.Should().Be(400);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GeneratePrompt_WithEmptyTemplateId_Returns400()
    {
        var request = new GeneratePromptRequest
        {
            BabbleText = "Some babble text",
            TemplateId = "",
        };

        await _controller.GeneratePrompt(request, CancellationToken.None);

        _controller.HttpContext.Response.StatusCode.Should().Be(400);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GeneratePrompt_WithNonExistentTemplate_Returns404()
    {
        var request = new GeneratePromptRequest
        {
            BabbleText = "Some babble text",
            TemplateId = "nonexistent-id",
        };

        _templateService.GetByIdAsync(null, "nonexistent-id", Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        await _controller.GeneratePrompt(request, CancellationToken.None);

        _controller.HttpContext.Response.StatusCode.Should().Be(404);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GeneratePrompt_WithValidTemplateId_UsesStructuredPath()
    {
        var request = new GeneratePromptRequest
        {
            BabbleText = "I want to build a REST API",
            TemplateId = "template-1",
        };

        var template = new PromptTemplate
        {
            Id = "template-1",
            UserId = "_builtin",
            Name = "GitHub Copilot Prompt",
            Description = "Prompt for GitHub Copilot",
            SystemPrompt = "You are a prompt engineer.",
            IsBuiltIn = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _templateService.GetByIdAsync(null, "template-1", Arg.Any<CancellationToken>())
            .Returns(template);

        _promptService.GenerateStructuredPromptAsync(
            request.BabbleText,
            template.SystemPrompt,
            template.Name,
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
}
