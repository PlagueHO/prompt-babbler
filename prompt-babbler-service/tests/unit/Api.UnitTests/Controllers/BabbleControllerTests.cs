using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
    private readonly ILogger<BabbleController> _logger = Substitute.For<ILogger<BabbleController>>();
    private readonly BabbleController _controller;

    public BabbleControllerTests()
    {
        _controller = new BabbleController(_babbleService, _logger);

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
        _babbleService.GetByUserAsync(TestUserId, null, 20, Arg.Any<CancellationToken>())
            .Returns((babbles.AsReadOnly(), (string?)null));

        var result = await _controller.GetBabbles(cancellationToken: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<PagedResponse<BabbleResponse>>();
    }

    [TestMethod]
    public async Task GetBabbles_ClampsPageSizeToMax100()
    {
        _babbleService.GetByUserAsync(TestUserId, null, 100, Arg.Any<CancellationToken>())
            .Returns((new List<Babble>().AsReadOnly(), (string?)null));

        await _controller.GetBabbles(pageSize: 200, cancellationToken: CancellationToken.None);

        await _babbleService.Received(1).GetByUserAsync(TestUserId, null, 100, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetBabbles_ClampsPageSizeToMin1()
    {
        _babbleService.GetByUserAsync(TestUserId, null, 1, Arg.Any<CancellationToken>())
            .Returns((new List<Babble>().AsReadOnly(), (string?)null));

        await _controller.GetBabbles(pageSize: 0, cancellationToken: CancellationToken.None);

        await _babbleService.Received(1).GetByUserAsync(TestUserId, null, 1, Arg.Any<CancellationToken>());
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
}
