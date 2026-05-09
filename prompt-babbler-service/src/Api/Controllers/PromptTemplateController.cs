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
[Route("api/templates")]
public sealed class PromptTemplateController : ControllerBase
{
    private static readonly string[] AllowedOutputFormats = ["text", "markdown"];

    private readonly IPromptTemplateService _templateService;
    private readonly ITemplateValidationService _validationService;
    private readonly ILogger<PromptTemplateController> _logger;

    public PromptTemplateController(
        IPromptTemplateService templateService,
        ITemplateValidationService validationService,
        ILogger<PromptTemplateController> logger)
    {
        _templateService = templateService;
        _validationService = validationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] string? continuationToken = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? tag = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (search is not null && search.Length > 200)
        {
            return BadRequest("search must be at most 200 characters.");
        }

        if (tag is not null && (string.IsNullOrWhiteSpace(tag) || tag.Length > 50))
        {
            return BadRequest("tag must be between 1 and 50 characters.");
        }

        if (sortBy is not null && sortBy != "name" && sortBy != "updatedAt")
        {
            return BadRequest("sortBy must be 'name' or 'updatedAt'.");
        }

        if (sortDirection is not null && sortDirection != "desc" && sortDirection != "asc")
        {
            return BadRequest("sortDirection must be 'desc' or 'asc'.");
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = User.GetUserIdOrAnonymous();
        if (forceRefresh)
        {
            await _templateService.GetTemplatesAsync(userId, forceRefresh: true, cancellationToken);
        }

        var (items, nextToken) = await _templateService.ListTemplatesAsync(
            userId,
            continuationToken,
            pageSize,
            search,
            tag,
            sortBy,
            sortDirection,
            cancellationToken);

        return Ok(new PagedResponse<PromptTemplateResponse>
        {
            Items = items.Select(ToResponse),
            ContinuationToken = nextToken,
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemplate(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();
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
        if (!ValidateTemplateFields(request.Name, request.Description, request.Instructions,
            request.OutputDescription, request.OutputTemplate, request.Examples,
            request.Guardrails, request.DefaultOutputFormat, request.Tags, out var validationError))
        {
            return BadRequest(validationError);
        }

        var now = DateTimeOffset.UtcNow;
        var userId = User.GetUserIdOrAnonymous();
        var template = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Instructions = request.Instructions,
            OutputDescription = request.OutputDescription,
            OutputTemplate = request.OutputTemplate,
            Examples = request.Examples?.Select(e => new PromptExample { Input = e.Input, Output = e.Output }).ToList(),
            Guardrails = request.Guardrails,
            DefaultOutputFormat = request.DefaultOutputFormat,
            DefaultAllowEmojis = request.DefaultAllowEmojis,
            Tags = request.Tags,
            AdditionalProperties = request.AdditionalProperties,
            IsBuiltIn = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var validationResult = await _validationService.ValidateTemplateAsync(template, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors });
        }

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
        if (!ValidateTemplateFields(request.Name, request.Description, request.Instructions,
            request.OutputDescription, request.OutputTemplate, request.Examples,
            request.Guardrails, request.DefaultOutputFormat, request.Tags, out var validationError))
        {
            return BadRequest(validationError);
        }

        var userId = User.GetUserIdOrAnonymous();
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
            Instructions = request.Instructions,
            OutputDescription = request.OutputDescription,
            OutputTemplate = request.OutputTemplate,
            Examples = request.Examples?.Select(e => new PromptExample { Input = e.Input, Output = e.Output }).ToList(),
            Guardrails = request.Guardrails,
            DefaultOutputFormat = request.DefaultOutputFormat,
            DefaultAllowEmojis = request.DefaultAllowEmojis,
            Tags = request.Tags,
            AdditionalProperties = request.AdditionalProperties,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var validationResult = await _validationService.ValidateTemplateAsync(updated, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors });
        }

        var result = await _templateService.UpdateAsync(updated, cancellationToken);
        _logger.LogInformation("Updated user template {TemplateId}: {TemplateName}", result.Id, result.Name);

        return Ok(ToResponse(result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();
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

    private static bool ValidateTemplateFields(
        string name,
        string description,
        string instructions,
        string? outputDescription,
        string? outputTemplate,
        IReadOnlyList<ExampleRequest>? examples,
        IReadOnlyList<string>? guardrails,
        string? defaultOutputFormat,
        IReadOnlyList<string>? tags,
        out string? error)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            error = "Name is required and must be between 1 and 100 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(description) || description.Length > 500)
        {
            error = "Description is required and must be between 1 and 500 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(instructions) || instructions.Length > 10000)
        {
            error = "Instructions is required and must be between 1 and 10000 characters.";
            return false;
        }

        if (outputDescription is not null && outputDescription.Length > 2000)
        {
            error = "OutputDescription must be at most 2000 characters.";
            return false;
        }

        if (outputTemplate is not null && outputTemplate.Length > 10000)
        {
            error = "OutputTemplate must be at most 10000 characters.";
            return false;
        }

        if (examples is not null)
        {
            if (examples.Count > 10)
            {
                error = "At most 10 examples are allowed.";
                return false;
            }

            for (var i = 0; i < examples.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(examples[i].Input) || examples[i].Input.Length > 5000)
                {
                    error = $"Examples[{i}].Input is required and must be at most 5000 characters.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(examples[i].Output) || examples[i].Output.Length > 10000)
                {
                    error = $"Examples[{i}].Output is required and must be at most 10000 characters.";
                    return false;
                }
            }
        }

        if (guardrails is not null)
        {
            if (guardrails.Count > 20)
            {
                error = "At most 20 guardrails are allowed.";
                return false;
            }

            for (var i = 0; i < guardrails.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(guardrails[i]) || guardrails[i].Length > 500)
                {
                    error = $"Guardrails[{i}] is required and must be at most 500 characters.";
                    return false;
                }
            }
        }

        if (defaultOutputFormat is not null &&
            !AllowedOutputFormats.Contains(defaultOutputFormat, StringComparer.OrdinalIgnoreCase))
        {
            error = "DefaultOutputFormat must be 'text' or 'markdown'.";
            return false;
        }

        if (tags is not null)
        {
            if (tags.Count > 20)
            {
                error = "At most 20 tags are allowed.";
                return false;
            }

            for (var i = 0; i < tags.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(tags[i]) || tags[i].Length > 50)
                {
                    error = $"Tags[{i}] is required and must be at most 50 characters.";
                    return false;
                }
            }
        }

        error = null;
        return true;
    }

    private static PromptTemplateResponse ToResponse(PromptTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        Description = template.Description,
        Instructions = template.Instructions,
        OutputDescription = template.OutputDescription,
        OutputTemplate = template.OutputTemplate,
        Examples = template.Examples,
        Guardrails = template.Guardrails,
        DefaultOutputFormat = template.DefaultOutputFormat,
        DefaultAllowEmojis = template.DefaultAllowEmojis,
        Tags = template.Tags,
        AdditionalProperties = template.AdditionalProperties,
        IsBuiltIn = template.IsBuiltIn,
        CreatedAt = template.CreatedAt.ToString("o"),
        UpdatedAt = template.UpdatedAt.ToString("o"),
    };
}
