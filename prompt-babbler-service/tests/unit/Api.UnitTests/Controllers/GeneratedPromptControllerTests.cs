using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class GeneratedPromptControllerTests
{
    private const string TestUserId = "00000000-0000-0000-0000-000000000000";
    private const string TestBabbleId = "test-babble-id";

    private readonly IGeneratedPromptService _promptService = Substitute.For<IGeneratedPromptService>();
    private readonly ILogger<GeneratedPromptController> _logger = Substitute.For<ILogger<GeneratedPromptController>>();
    private readonly GeneratedPromptController _controller;

    public GeneratedPromptControllerTests()
    {
        _controller = new GeneratedPromptController(_promptService, _logger);

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

    private static GeneratedPrompt CreatePrompt(
        string id = "test-prompt-id",
        string babbleId = TestBabbleId) => new()
        {
            Id = id,
            BabbleId = babbleId,
            UserId = TestUserId,
            TemplateId = "template-1",
            TemplateName = "Test Template",
            PromptText = "Generated prompt text.",
            GeneratedAt = DateTimeOffset.UtcNow,
        };

    // ---- GET /api/babbles/{babbleId}/prompts ----

    [TestMethod]
    public async Task GetPrompts_ReturnsPagedResponse()
    {
        var prompts = new List<GeneratedPrompt> { CreatePrompt() };
        _promptService.GetByBabbleAsync(TestUserId, TestBabbleId, null, 20, Arg.Any<CancellationToken>())
            .Returns((prompts.AsReadOnly(), (string?)null));

        var result = await _controller.GetPrompts(TestBabbleId, cancellationToken: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<PagedResponse<GeneratedPromptResponse>>();
    }

    [TestMethod]
    public async Task GetPrompts_BabbleNotFound_Returns404()
    {
        _promptService.GetByBabbleAsync(TestUserId, "missing", null, 20, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Babble not found"));

        var result = await _controller.GetPrompts("missing", cancellationToken: CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ---- GET /api/babbles/{babbleId}/prompts/{id} ----

    [TestMethod]
    public async Task GetPrompt_ExistingId_ReturnsPrompt()
    {
        var prompt = CreatePrompt();
        _promptService.GetByIdAsync(TestUserId, TestBabbleId, "test-prompt-id", Arg.Any<CancellationToken>())
            .Returns(prompt);

        var result = await _controller.GetPrompt(TestBabbleId, "test-prompt-id", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [TestMethod]
    public async Task GetPrompt_NonExistentId_Returns404()
    {
        _promptService.GetByIdAsync(TestUserId, TestBabbleId, "missing", Arg.Any<CancellationToken>())
            .Returns((GeneratedPrompt?)null);

        var result = await _controller.GetPrompt(TestBabbleId, "missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetPrompt_BabbleNotOwned_Returns404()
    {
        _promptService.GetByIdAsync(TestUserId, "wrong-babble", "test-prompt-id", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Babble not found"));

        var result = await _controller.GetPrompt("wrong-babble", "test-prompt-id", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ---- POST /api/babbles/{babbleId}/prompts ----

    [TestMethod]
    public async Task CreatePrompt_ValidRequest_Returns201()
    {
        var request = new CreateGeneratedPromptRequest
        {
            TemplateId = "template-1",
            TemplateName = "Test Template",
            PromptText = "Generated prompt text.",
        };

        _promptService.CreateAsync(TestUserId, Arg.Any<GeneratedPrompt>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<GeneratedPrompt>());

        var result = await _controller.CreatePrompt(TestBabbleId, request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
    }

    [TestMethod]
    public async Task CreatePrompt_EmptyTemplateId_Returns400()
    {
        var request = new CreateGeneratedPromptRequest
        {
            TemplateId = "",
            TemplateName = "Test Template",
            PromptText = "Generated prompt text.",
        };

        var result = await _controller.CreatePrompt(TestBabbleId, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreatePrompt_EmptyPromptText_Returns400()
    {
        var request = new CreateGeneratedPromptRequest
        {
            TemplateId = "template-1",
            TemplateName = "Test Template",
            PromptText = "",
        };

        var result = await _controller.CreatePrompt(TestBabbleId, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreatePrompt_EmptyTemplateName_Returns400()
    {
        var request = new CreateGeneratedPromptRequest
        {
            TemplateId = "template-1",
            TemplateName = "",
            PromptText = "Generated prompt text.",
        };

        var result = await _controller.CreatePrompt(TestBabbleId, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreatePrompt_BabbleNotOwned_Returns404()
    {
        var request = new CreateGeneratedPromptRequest
        {
            TemplateId = "template-1",
            TemplateName = "Test Template",
            PromptText = "Generated prompt text.",
        };

        _promptService.CreateAsync(TestUserId, Arg.Any<GeneratedPrompt>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Babble not found"));

        var result = await _controller.CreatePrompt("wrong-babble", request, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ---- DELETE /api/babbles/{babbleId}/prompts/{id} ----

    [TestMethod]
    public async Task DeletePrompt_ValidRequest_Returns204()
    {
        var result = await _controller.DeletePrompt(TestBabbleId, "test-prompt-id", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await _promptService.Received(1).DeleteAsync(TestUserId, TestBabbleId, "test-prompt-id", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeletePrompt_BabbleNotOwned_Returns404()
    {
        _promptService.DeleteAsync(TestUserId, "wrong-babble", "test-prompt-id", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Babble not found"));

        var result = await _controller.DeletePrompt("wrong-babble", "test-prompt-id", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }
}
