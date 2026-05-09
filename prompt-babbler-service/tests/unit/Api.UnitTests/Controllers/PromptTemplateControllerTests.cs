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
public sealed class PromptTemplateControllerTests
{
    private const string TestUserId = "00000000-0000-0000-0000-000000000000";

    private readonly IPromptTemplateService _templateService = Substitute.For<IPromptTemplateService>();
    private readonly ITemplateValidationService _validationService = Substitute.For<ITemplateValidationService>();
    private readonly ILogger<PromptTemplateController> _logger = Substitute.For<ILogger<PromptTemplateController>>();
    private readonly PromptTemplateController _controller;

    public PromptTemplateControllerTests()
    {
        _controller = new PromptTemplateController(_templateService, _validationService, _logger);

        // Default: validation always passes
        _validationService.ValidateTemplateAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .Returns(TemplateValidationResult.Success());

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

    private static PromptTemplate CreateTemplate(
        string id = "test-id",
        string userId = TestUserId,
        string name = "Test Template",
        bool isBuiltIn = false) => new()
        {
            Id = id,
            UserId = userId,
            Name = name,
            Description = "A test template description.",
            Instructions = "You are a test assistant.",
            IsBuiltIn = isBuiltIn,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ---- GET /api/templates ----

    [TestMethod]
    public async Task GetTemplates_ReturnsAllTemplates()
    {
        var templates = new List<PromptTemplate> { CreateTemplate(), CreateTemplate("id2", name: "Second") };
        _templateService.ListTemplatesAsync(
            TestUserId,
            null,
            20,
            null,
            null,
            null,
            null,
            Arg.Any<CancellationToken>())
            .Returns((templates, null));

        var result = await _controller.GetTemplates(cancellationToken: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PagedResponse<PromptTemplateResponse>>().Subject;
        response.Items.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetTemplates_WithForceRefresh_PassesFlagToService()
    {
        _templateService.GetTemplatesAsync(TestUserId, true, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PromptTemplate>());
        _templateService.ListTemplatesAsync(
            TestUserId,
            null,
            20,
            null,
            null,
            null,
            null,
            Arg.Any<CancellationToken>())
            .Returns((Array.Empty<PromptTemplate>(), null));

        await _controller.GetTemplates(forceRefresh: true, cancellationToken: CancellationToken.None);

        await _templateService.Received(1).GetTemplatesAsync(TestUserId, true, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetTemplates_EmptyResult_ReturnsEmptyArray()
    {
        _templateService.ListTemplatesAsync(
            TestUserId,
            null,
            20,
            null,
            null,
            null,
            null,
            Arg.Any<CancellationToken>())
            .Returns((Array.Empty<PromptTemplate>(), null));

        var result = await _controller.GetTemplates(cancellationToken: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PagedResponse<PromptTemplateResponse>>().Subject;
        response.Items.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetTemplates_WithFiltersAndSort_PassesParametersToService()
    {
        _templateService.ListTemplatesAsync(
            TestUserId,
            "ct",
            25,
            "writer",
            "creative",
            "name",
            "asc",
            Arg.Any<CancellationToken>())
            .Returns((Array.Empty<PromptTemplate>(), null));

        await _controller.GetTemplates(
            continuationToken: "ct",
            pageSize: 25,
            search: "writer",
            tag: "creative",
            sortBy: "name",
            sortDirection: "asc",
            cancellationToken: CancellationToken.None);

        await _templateService.Received(1).ListTemplatesAsync(
            TestUserId,
            "ct",
            25,
            "writer",
            "creative",
            "name",
            "asc",
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetTemplates_InvalidSortBy_ReturnsBadRequest()
    {
        var result = await _controller.GetTemplates(sortBy: "invalid", cancellationToken: CancellationToken.None);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ---- GET /api/templates/{id} ----

    [TestMethod]
    public async Task GetTemplate_ExistingId_ReturnsTemplate()
    {
        var template = CreateTemplate();
        _templateService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await _controller.GetTemplate("test-id", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [TestMethod]
    public async Task GetTemplate_NonExistentId_Returns404()
    {
        _templateService.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var result = await _controller.GetTemplate("missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ---- POST /api/templates ----

    [TestMethod]
    public async Task CreateTemplate_ValidRequest_Returns201WithTemplate()
    {
        var request = new CreatePromptTemplateRequest
        {
            Name = "New Template",
            Description = "A new template",
            Instructions = "You are helpful.",
        };

        _templateService.CreateAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<PromptTemplate>());

        var result = await _controller.CreateTemplate(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
    }

    [TestMethod]
    public async Task CreateTemplate_EmptyName_Returns400()
    {
        var request = new CreatePromptTemplateRequest
        {
            Name = "",
            Description = "A new template",
            Instructions = "You are helpful.",
        };

        var result = await _controller.CreateTemplate(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreateTemplate_EmptySystemPrompt_Returns400()
    {
        var request = new CreatePromptTemplateRequest
        {
            Name = "Valid Name",
            Description = "A new template",
            Instructions = "",
        };

        var result = await _controller.CreateTemplate(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreateTemplate_NameTooLong_Returns400()
    {
        var request = new CreatePromptTemplateRequest
        {
            Name = new string('x', 101),
            Description = "A new template",
            Instructions = "You are helpful.",
        };

        var result = await _controller.CreateTemplate(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreateTemplate_DescriptionTooLong_Returns400()
    {
        var request = new CreatePromptTemplateRequest
        {
            Name = "Valid Name",
            Description = new string('x', 501),
            Instructions = "You are helpful.",
        };

        var result = await _controller.CreateTemplate(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreateTemplate_SystemPromptTooLong_Returns400()
    {
        var request = new CreatePromptTemplateRequest
        {
            Name = "Valid Name",
            Description = "Valid description",
            Instructions = new string('x', 10001),
        };

        var result = await _controller.CreateTemplate(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ---- PUT /api/templates/{id} ----

    [TestMethod]
    public async Task UpdateTemplate_ExistingUserTemplate_Returns200()
    {
        var existing = CreateTemplate();
        _templateService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(existing);
        _templateService.UpdateAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<PromptTemplate>());

        var request = new UpdatePromptTemplateRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Instructions = "Updated prompt",
        };

        var result = await _controller.UpdateTemplate("test-id", request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [TestMethod]
    public async Task UpdateTemplate_BuiltInTemplate_Returns403()
    {
        var existing = CreateTemplate(isBuiltIn: true, userId: "_builtin");
        _templateService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new UpdatePromptTemplateRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Instructions = "Updated prompt",
        };

        var result = await _controller.UpdateTemplate("test-id", request, CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(403);
    }

    [TestMethod]
    public async Task UpdateTemplate_NonExistent_Returns404()
    {
        _templateService.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var request = new UpdatePromptTemplateRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Instructions = "Updated prompt",
        };

        var result = await _controller.UpdateTemplate("missing", request, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task UpdateTemplate_EmptyName_Returns400()
    {
        var request = new UpdatePromptTemplateRequest
        {
            Name = "",
            Description = "Valid description",
            Instructions = "Valid prompt",
        };

        var result = await _controller.UpdateTemplate("test-id", request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ---- DELETE /api/templates/{id} ----

    [TestMethod]
    public async Task DeleteTemplate_UserTemplate_Returns204()
    {
        var existing = CreateTemplate();
        _templateService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _controller.DeleteTemplate("test-id", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await _templateService.Received(1).DeleteAsync(existing.UserId, "test-id", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteTemplate_BuiltInTemplate_Returns403()
    {
        var existing = CreateTemplate(isBuiltIn: true, userId: "_builtin");
        _templateService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _controller.DeleteTemplate("test-id", CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(403);
    }

    [TestMethod]
    public async Task DeleteTemplate_NonExistent_Returns404()
    {
        _templateService.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var result = await _controller.DeleteTemplate("missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }
}
