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
public sealed class BabbleControllerTests
{
    private const string TestUserId = "00000000-0000-0000-0000-000000000000";

    private readonly IBabbleService _babbleService = Substitute.For<IBabbleService>();
    private readonly IPromptGenerationService _promptGenerationService = Substitute.For<IPromptGenerationService>();
    private readonly IPromptTemplateService _templateService = Substitute.For<IPromptTemplateService>();
    private readonly IGeneratedPromptService _generatedPromptService = Substitute.For<IGeneratedPromptService>();
    private readonly IFileTranscriptionService _fileTranscriptionService = Substitute.For<IFileTranscriptionService>();
    private readonly ILogger<BabbleController> _logger = Substitute.For<ILogger<BabbleController>>();
    private readonly BabbleController _controller;

    public BabbleControllerTests()
    {
        _controller = new BabbleController(
            _babbleService, _promptGenerationService, _templateService, _generatedPromptService, _fileTranscriptionService, _logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
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

    private static Babble CreateBabble(
        string id = "test-id",
        string userId = TestUserId) => new()
        {
            Id = id,
            UserId = userId,
            Title = "Test Babble",
            Text = "This is a test babble transcription.",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ---- GET /api/babbles ----

    [TestMethod]
    public async Task GetBabbles_ReturnsPagedResponse()
    {
        var babbles = new List<Babble> { CreateBabble() };
        _babbleService.GetByUserAsync(TestUserId, null, 20, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((babbles.AsReadOnly(), (string?)null));

        var result = await _controller.GetBabbles(cancellationToken: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<PagedResponse<BabbleResponse>>();
    }

    [TestMethod]
    public async Task GetBabbles_ClampsPageSizeToMax100()
    {
        _babbleService.GetByUserAsync(TestUserId, null, 100, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((new List<Babble>().AsReadOnly(), (string?)null));

        await _controller.GetBabbles(pageSize: 200, cancellationToken: CancellationToken.None);

        await _babbleService.Received(1).GetByUserAsync(TestUserId, null, 100, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetBabbles_ClampsPageSizeToMin1()
    {
        _babbleService.GetByUserAsync(TestUserId, null, 1, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns((new List<Babble>().AsReadOnly(), (string?)null));

        await _controller.GetBabbles(pageSize: 0, cancellationToken: CancellationToken.None);

        await _babbleService.Received(1).GetByUserAsync(TestUserId, null, 1, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>());
    }

    // ---- GET /api/babbles/{id} ----

    [TestMethod]
    public async Task GetBabble_ExistingId_ReturnsBabble()
    {
        var babble = CreateBabble();
        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(babble);

        var result = await _controller.GetBabble("test-id", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [TestMethod]
    public async Task GetBabble_NonExistentId_Returns404()
    {
        _babbleService.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var result = await _controller.GetBabble("missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ---- POST /api/babbles ----

    [TestMethod]
    public async Task CreateBabble_ValidRequest_Returns201()
    {
        var request = new CreateBabbleRequest
        {
            Title = "New Babble",
            Text = "This is a new babble.",
        };

        _babbleService.CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var result = await _controller.CreateBabble(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
    }

    [TestMethod]
    public async Task CreateBabble_EmptyTitle_Returns400()
    {
        var request = new CreateBabbleRequest
        {
            Title = "",
            Text = "Valid text.",
        };

        var result = await _controller.CreateBabble(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreateBabble_TitleTooLong_Returns400()
    {
        var request = new CreateBabbleRequest
        {
            Title = new string('x', 201),
            Text = "Valid text.",
        };

        var result = await _controller.CreateBabble(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreateBabble_EmptyText_Returns400()
    {
        var request = new CreateBabbleRequest
        {
            Title = "Valid Title",
            Text = "",
        };

        var result = await _controller.CreateBabble(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreateBabble_TextTooLong_Returns400()
    {
        var request = new CreateBabbleRequest
        {
            Title = "Valid Title",
            Text = new string('x', 50001),
        };

        var result = await _controller.CreateBabble(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ---- PUT /api/babbles/{id} ----

    [TestMethod]
    public async Task UpdateBabble_ExistingBabble_Returns200()
    {
        var existing = CreateBabble();
        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(existing);
        _babbleService.UpdateAsync(TestUserId, Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var request = new UpdateBabbleRequest
        {
            Title = "Updated Title",
            Text = "Updated text.",
        };

        var result = await _controller.UpdateBabble("test-id", request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [TestMethod]
    public async Task UpdateBabble_NonExistent_Returns404()
    {
        _babbleService.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var request = new UpdateBabbleRequest
        {
            Title = "Updated Title",
            Text = "Updated text.",
        };

        var result = await _controller.UpdateBabble("missing", request, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task UpdateBabble_EmptyTitle_Returns400()
    {
        var request = new UpdateBabbleRequest
        {
            Title = "",
            Text = "Valid text.",
        };

        var result = await _controller.UpdateBabble("test-id", request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ---- DELETE /api/babbles/{id} ----

    [TestMethod]
    public async Task DeleteBabble_ExistingBabble_Returns204()
    {
        var existing = CreateBabble();
        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _controller.DeleteBabble("test-id", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await _babbleService.Received(1).DeleteAsync(TestUserId, "test-id", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteBabble_NonExistent_Returns404()
    {
        _babbleService.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var result = await _controller.DeleteBabble("missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ---- POST /api/babbles/{id}/generate ----

    private static PromptTemplate CreateTemplate(string id = "template-1") => new()
    {
        Id = id,
        UserId = "_builtin",
        Name = "Test Template",
        Description = "A test template.",
        Instructions = "You are a prompt engineer.",
        IsBuiltIn = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(params string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }

    [TestMethod]
    public async Task GeneratePrompt_BabbleNotFound_Returns404()
    {
        _babbleService.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var request = new GeneratePromptRequest { TemplateId = "template-1" };

        await _controller.GeneratePrompt("missing", request, CancellationToken.None);

        _controller.HttpContext.Response.StatusCode.Should().Be(404);
    }

    [TestMethod]
    public async Task GeneratePrompt_EmptyTemplateId_Returns400()
    {
        var babble = CreateBabble();
        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(babble);

        var request = new GeneratePromptRequest { TemplateId = "" };

        await _controller.GeneratePrompt("test-id", request, CancellationToken.None);

        _controller.HttpContext.Response.StatusCode.Should().Be(400);
    }

    [TestMethod]
    public async Task GeneratePrompt_TemplateNotFound_Returns404()
    {
        var babble = CreateBabble();
        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(babble);
        _templateService.GetByIdAsync(null, "nonexistent", Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var request = new GeneratePromptRequest { TemplateId = "nonexistent" };

        await _controller.GeneratePrompt("test-id", request, CancellationToken.None);

        _controller.HttpContext.Response.StatusCode.Should().Be(404);
    }

    [TestMethod]
    public async Task GeneratePrompt_ValidRequest_StreamsSSEChunksAndPromptId()
    {
        var babble = CreateBabble();
        var template = CreateTemplate();

        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(babble);
        _templateService.GetByIdAsync(null, "template-1", Arg.Any<CancellationToken>())
            .Returns(template);
        _promptGenerationService.GeneratePromptStreamAsync(
            babble.Text, template, Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable("Hello ", "world"));
        _generatedPromptService.CreateAsync(TestUserId, Arg.Any<GeneratedPrompt>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var p = ci.Arg<GeneratedPrompt>();
                return p;
            });

        var request = new GeneratePromptRequest { TemplateId = "template-1" };
        await _controller.GeneratePrompt("test-id", request, CancellationToken.None);

        _controller.HttpContext.Response.ContentType.Should().Be("text/event-stream");

        _controller.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(_controller.HttpContext.Response.Body).ReadToEndAsync();
        body.Should().Contain("\"text\":\"Hello \"");
        body.Should().Contain("\"text\":\"world\"");
        body.Should().Contain("\"promptId\":");
        body.Should().Contain("data: [DONE]");
    }

    [TestMethod]
    public async Task GeneratePrompt_LlmFailure_Returns502()
    {
        var babble = CreateBabble();
        var template = CreateTemplate();

        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(babble);
        _templateService.GetByIdAsync(null, "template-1", Arg.Any<CancellationToken>())
            .Returns(template);
        _promptGenerationService.GeneratePromptStreamAsync(
            babble.Text, template, Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("LLM error"));

        var request = new GeneratePromptRequest { TemplateId = "template-1" };
        await _controller.GeneratePrompt("test-id", request, CancellationToken.None);

        _controller.HttpContext.Response.StatusCode.Should().Be(502);
    }

    // ---- POST /api/babbles/{id}/generate-title ----

    [TestMethod]
    public async Task GenerateTitle_BabbleNotFound_Returns404()
    {
        _babbleService.GetByIdAsync(TestUserId, "missing", Arg.Any<CancellationToken>())
            .Returns((Babble?)null);

        var result = await _controller.GenerateTitle("missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GenerateTitle_ValidRequest_UpdatesTitleAndReturnsResponse()
    {
        var babble = CreateBabble();
        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(babble);
        _promptGenerationService.GenerateTitleAsync(babble.Text, Arg.Any<CancellationToken>())
            .Returns("Sort Function Request");
        _babbleService.UpdateAsync(TestUserId, Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var result = await _controller.GenerateTitle("test-id", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BabbleResponse>().Subject;
        response.Title.Should().Be("Sort Function Request");
    }

    [TestMethod]
    public async Task GenerateTitle_LlmFailure_Returns502()
    {
        var babble = CreateBabble();
        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(babble);
        _promptGenerationService.GenerateTitleAsync(babble.Text, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("LLM error"));

        var result = await _controller.GenerateTitle("test-id", CancellationToken.None);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(502);
    }

    // ---- Tag validation ----

    [TestMethod]
    public async Task CreateBabble_TooManyTags_Returns400()
    {
        var tags = Enumerable.Range(0, 21).Select(i => $"tag{i}").ToList();
        var request = new CreateBabbleRequest
        {
            Title = "Valid Title",
            Text = "Valid text.",
            Tags = tags,
        };

        var result = await _controller.CreateBabble(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreateBabble_TagTooLong_Returns400()
    {
        var request = new CreateBabbleRequest
        {
            Title = "Valid Title",
            Text = "Valid text.",
            Tags = [new string('x', 51)],
        };

        var result = await _controller.CreateBabble(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task CreateBabble_ValidTags_Returns201()
    {
        _babbleService.CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var request = new CreateBabbleRequest
        {
            Title = "Valid Title",
            Text = "Valid text.",
            Tags = ["tag1", "tag2"],
        };

        var result = await _controller.CreateBabble(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = created.Value.Should().BeOfType<BabbleResponse>().Subject;
        response.Tags.Should().BeEquivalentTo(new[] { "tag1", "tag2" });
    }

    [TestMethod]
    public async Task UpdateBabble_TooManyTags_Returns400()
    {
        var tags = Enumerable.Range(0, 21).Select(i => $"tag{i}").ToList();
        var request = new UpdateBabbleRequest
        {
            Title = "Valid Title",
            Text = "Valid text.",
            Tags = tags,
        };

        var result = await _controller.UpdateBabble("test-id", request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UpdateBabble_ValidTags_Returns200()
    {
        var existing = CreateBabble();
        _babbleService.GetByIdAsync(TestUserId, "test-id", Arg.Any<CancellationToken>())
            .Returns(existing);
        _babbleService.UpdateAsync(TestUserId, Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var request = new UpdateBabbleRequest
        {
            Title = "Updated Title",
            Text = "Updated text.",
            Tags = ["alpha", "beta"],
        };

        var result = await _controller.UpdateBabble("test-id", request, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BabbleResponse>().Subject;
        response.Tags.Should().BeEquivalentTo(new[] { "alpha", "beta" });
    }

    // ---- PATCH /api/babbles/{id}/pin ----

    [TestMethod]
    public async Task PinBabble_WithValidRequest_ReturnsOkWithUpdatedBabble()
    {
        // Arrange
        var babble = CreateBabble() with { IsPinned = true };
        _babbleService.SetPinAsync(TestUserId, "test-id", true, Arg.Any<CancellationToken>())
            .Returns(babble);

        var request = new PinBabbleRequest { IsPinned = true };

        // Act
        var result = await _controller.PinBabble("test-id", request, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BabbleResponse>().Subject;
        response.IsPinned.Should().BeTrue();
        await _babbleService.Received(1).SetPinAsync(TestUserId, "test-id", true, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task PinBabble_WithNonExistentBabble_ReturnsNotFound()
    {
        // Arrange
        _babbleService.SetPinAsync(TestUserId, "missing", true, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Babble not found"));

        var request = new PinBabbleRequest { IsPinned = true };

        // Act
        var result = await _controller.PinBabble("missing", request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task PinBabble_UnpinBabble_ReturnsOkWithUnpinnedBabble()
    {
        // Arrange
        var babble = CreateBabble();
        _babbleService.SetPinAsync(TestUserId, "test-id", false, Arg.Any<CancellationToken>())
            .Returns(babble);

        var request = new PinBabbleRequest { IsPinned = false };

        // Act
        var result = await _controller.PinBabble("test-id", request, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BabbleResponse>().Subject;
        response.IsPinned.Should().BeFalse();
        await _babbleService.Received(1).SetPinAsync(TestUserId, "test-id", false, Arg.Any<CancellationToken>());
    }

    // ---- GET /api/babbles/search ----

    [TestMethod]
    public async Task SearchBabbles_ValidQuery_ReturnsOkWithResults()
    {
        var searchResults = new List<BabbleSearchResult>
        {
            new(CreateBabble(id: "result-1"), 0.95),
            new(CreateBabble(id: "result-2"), 0.80),
        };
        _babbleService.SearchAsync(TestUserId, "test query", 10, Arg.Any<CancellationToken>())
            .Returns(searchResults.AsReadOnly());

        var result = await _controller.SearchBabbles("test query", 10, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BabbleSearchResponse>().Subject;
        response.Results.Should().HaveCount(2);
        response.Results[0].Score.Should().Be(0.95);
    }

    [TestMethod]
    public async Task SearchBabbles_EmptyQuery_ReturnsBadRequest()
    {
        var result = await _controller.SearchBabbles("", 10, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task SearchBabbles_QueryTooLong_ReturnsBadRequest()
    {
        var result = await _controller.SearchBabbles(new string('x', 201), 10, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task SearchBabbles_TopKClamped_ClampsToMax50()
    {
        _babbleService.SearchAsync(TestUserId, "query", 50, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult>().AsReadOnly());

        await _controller.SearchBabbles("query", 100, CancellationToken.None);

        await _babbleService.Received(1).SearchAsync(TestUserId, "query", 50, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SearchBabbles_TopKClamped_ClampsToMin1()
    {
        _babbleService.SearchAsync(TestUserId, "query", 1, Arg.Any<CancellationToken>())
            .Returns(new List<BabbleSearchResult>().AsReadOnly());

        await _controller.SearchBabbles("query", 0, CancellationToken.None);

        await _babbleService.Received(1).SearchAsync(TestUserId, "query", 1, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SearchBabbles_ServiceThrows_Returns502()
    {
        _babbleService.SearchAsync(TestUserId, "test query", 10, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Embedding service unavailable"));

        var result = await _controller.SearchBabbles("test query", 10, CancellationToken.None);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(502);
    }
}
