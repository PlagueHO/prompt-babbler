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
[Route("api/templates")]
public sealed class PromptTemplateController : ControllerBase
{
    private readonly IPromptTemplateService _templateService;
    private readonly ILogger<PromptTemplateController> _logger;

    public PromptTemplateController(IPromptTemplateService templateService, ILogger<PromptTemplateController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        // Extract userId from the authenticated user's Entra ID object ID claim.
        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");
        var templates = await _templateService.GetTemplatesAsync(userId, forceRefresh, cancellationToken);
        return Ok(templates.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemplate(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");
        var template = await _templateService.GetByIdAsync(userId, id, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(template));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreatePromptTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateCreateRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        var now = DateTimeOffset.UtcNow;
        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");
        var template = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            SystemPrompt = request.SystemPrompt,
            IsBuiltIn = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _templateService.CreateAsync(template, cancellationToken);
        _logger.LogInformation("Created user template {TemplateId}: {TemplateName}", created.Id, created.Name);

        return CreatedAtAction(nameof(GetTemplate), new { id = created.Id }, ToResponse(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(
        string id,
        [FromBody] UpdatePromptTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateUpdateRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");
        var existing = await _templateService.GetByIdAsync(userId, id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.IsBuiltIn)
        {
            return Problem(
                title: "Forbidden",
                detail: "Built-in templates cannot be modified.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var updated = existing with
        {
            Name = request.Name,
            Description = request.Description,
            SystemPrompt = request.SystemPrompt,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var result = await _templateService.UpdateAsync(updated, cancellationToken);
        _logger.LogInformation("Updated user template {TemplateId}: {TemplateName}", result.Id, result.Name);

        return Ok(ToResponse(result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim is missing.");
        var existing = await _templateService.GetByIdAsync(userId, id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.IsBuiltIn)
        {
            return Problem(
                title: "Forbidden",
                detail: "Built-in templates cannot be deleted.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        await _templateService.DeleteAsync(existing.UserId, id, cancellationToken);
        _logger.LogInformation("Deleted user template {TemplateId}", id);

        return NoContent();
    }

    private static bool ValidateCreateRequest(CreatePromptTemplateRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
        {
            error = "Name is required and must be between 1 and 100 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 500)
        {
            error = "Description is required and must be between 1 and 500 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.SystemPrompt) || request.SystemPrompt.Length > 10000)
        {
            error = "SystemPrompt is required and must be between 1 and 10000 characters.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateUpdateRequest(UpdatePromptTemplateRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
        {
            error = "Name is required and must be between 1 and 100 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 500)
        {
            error = "Description is required and must be between 1 and 500 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.SystemPrompt) || request.SystemPrompt.Length > 10000)
        {
            error = "SystemPrompt is required and must be between 1 and 10000 characters.";
            return false;
        }

        error = null;
        return true;
    }

    private static PromptTemplateResponse ToResponse(PromptTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        Description = template.Description,
        SystemPrompt = template.SystemPrompt,
        IsBuiltIn = template.IsBuiltIn,
        CreatedAt = template.CreatedAt.ToString("o"),
        UpdatedAt = template.UpdatedAt.ToString("o"),
    };
}
