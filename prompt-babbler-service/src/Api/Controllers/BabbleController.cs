using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Authorize]
[RequiredScope("access_as_user")]
[Route("api/babbles")]
public sealed class BabbleController : ControllerBase
{
    private readonly IBabbleService _babbleService;
    private readonly ILogger<BabbleController> _logger;

    public BabbleController(IBabbleService babbleService, ILogger<BabbleController> logger)
    {
        _babbleService = babbleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBabbles(
        [FromQuery] string? continuationToken = null,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");

        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, nextToken) = await _babbleService.GetByUserAsync(userId, continuationToken, pageSize, cancellationToken);

        return Ok(new PagedResponse<BabbleResponse>
        {
            Items = items.Select(ToResponse),
            ContinuationToken = nextToken,
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBabble(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");
        var babble = await _babbleService.GetByIdAsync(userId, id, cancellationToken);
        if (babble is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(babble));
    }

    [HttpPost]
    public async Task<IActionResult> CreateBabble(
        [FromBody] CreateBabbleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateCreateRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        var now = DateTimeOffset.UtcNow;
        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");
        var babble = new Babble
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Title = request.Title,
            Text = request.Text,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _babbleService.CreateAsync(babble, cancellationToken);
        _logger.LogInformation("Created babble {BabbleId} for user {UserId}", created.Id, created.UserId);

        return CreatedAtAction(nameof(GetBabble), new { id = created.Id }, ToResponse(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBabble(
        string id,
        [FromBody] UpdateBabbleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateUpdateRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");
        var existing = await _babbleService.GetByIdAsync(userId, id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var updated = existing with
        {
            Title = request.Title,
            Text = request.Text,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var result = await _babbleService.UpdateAsync(userId, updated, cancellationToken);
        _logger.LogInformation("Updated babble {BabbleId} for user {UserId}", result.Id, result.UserId);

        return Ok(ToResponse(result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBabble(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");
        var existing = await _babbleService.GetByIdAsync(userId, id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        await _babbleService.DeleteAsync(userId, id, cancellationToken);
        _logger.LogInformation("Deleted babble {BabbleId} for user {UserId}", id, userId);

        return NoContent();
    }

    private static bool ValidateCreateRequest(CreateBabbleRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
        {
            error = "Title is required and must be between 1 and 200 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 50000)
        {
            error = "Text is required and must be between 1 and 50000 characters.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateUpdateRequest(UpdateBabbleRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
        {
            error = "Title is required and must be between 1 and 200 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 50000)
        {
            error = "Text is required and must be between 1 and 50000 characters.";
            return false;
        }

        error = null;
        return true;
    }

    private static BabbleResponse ToResponse(Babble babble) => new()
    {
        Id = babble.Id,
        Title = babble.Title,
        Text = babble.Text,
        CreatedAt = babble.CreatedAt.ToString("o"),
        UpdatedAt = babble.UpdatedAt.ToString("o"),
    };
}
