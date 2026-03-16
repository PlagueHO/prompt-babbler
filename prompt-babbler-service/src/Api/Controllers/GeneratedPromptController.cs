using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using PromptBabbler.Api.Extensions;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Authorize]
[RequiredScope("access_as_user")]
[Route("api/babbles/{babbleId}/prompts")]
public sealed class GeneratedPromptController : ControllerBase
{
    private readonly IGeneratedPromptService _promptService;
    private readonly ILogger<GeneratedPromptController> _logger;

    public GeneratedPromptController(IGeneratedPromptService promptService, ILogger<GeneratedPromptController> logger)
    {
        _promptService = promptService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPrompts(
        string babbleId,
        [FromQuery] string? continuationToken = null,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();

        pageSize = Math.Clamp(pageSize, 1, 100);

        try
        {
            var (items, nextToken) = await _promptService.GetByBabbleAsync(userId, babbleId, continuationToken, pageSize, cancellationToken);

            return Ok(new PagedResponse<GeneratedPromptResponse>
            {
                Items = items.Select(ToResponse),
                ContinuationToken = nextToken,
            });
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPrompt(
        string babbleId,
        string id,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();

        try
        {
            var prompt = await _promptService.GetByIdAsync(userId, babbleId, id, cancellationToken);
            if (prompt is null)
            {
                return NotFound();
            }

            return Ok(ToResponse(prompt));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePrompt(
        string babbleId,
        [FromBody] CreateGeneratedPromptRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateCreateRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        var userId = User.GetUserIdOrAnonymous();

        var prompt = new GeneratedPrompt
        {
            Id = Guid.NewGuid().ToString(),
            BabbleId = babbleId,
            UserId = userId,
            TemplateId = request.TemplateId,
            TemplateName = request.TemplateName,
            PromptText = request.PromptText,
            GeneratedAt = DateTimeOffset.UtcNow,
        };

        try
        {
            var created = await _promptService.CreateAsync(userId, prompt, cancellationToken);
            _logger.LogInformation("Created generated prompt {PromptId} for babble {BabbleId}", created.Id, created.BabbleId);

            return CreatedAtAction(nameof(GetPrompt), new { babbleId, id = created.Id }, ToResponse(created));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrompt(
        string babbleId,
        string id,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();

        try
        {
            await _promptService.DeleteAsync(userId, babbleId, id, cancellationToken);
            _logger.LogInformation("Deleted generated prompt {PromptId} for babble {BabbleId}", id, babbleId);

            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private static bool ValidateCreateRequest(CreateGeneratedPromptRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            error = "TemplateId is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.TemplateName) || request.TemplateName.Length > 100)
        {
            error = "TemplateName is required and must be between 1 and 100 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.PromptText) || request.PromptText.Length > 50000)
        {
            error = "PromptText is required and must be between 1 and 50000 characters.";
            return false;
        }

        error = null;
        return true;
    }

    private static GeneratedPromptResponse ToResponse(GeneratedPrompt prompt) => new()
    {
        Id = prompt.Id,
        BabbleId = prompt.BabbleId,
        TemplateId = prompt.TemplateId,
        TemplateName = prompt.TemplateName,
        PromptText = prompt.PromptText,
        GeneratedAt = prompt.GeneratedAt.ToString("o"),
    };
}
