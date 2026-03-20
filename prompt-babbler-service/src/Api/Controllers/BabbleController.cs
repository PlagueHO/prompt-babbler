using System.Text.Json;
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
[Route("api/babbles")]
public sealed class BabbleController : ControllerBase
{
    private readonly IBabbleService _babbleService;
    private readonly IPromptGenerationService _promptGenerationService;
    private readonly IPromptTemplateService _templateService;
    private readonly IGeneratedPromptService _generatedPromptService;
    private readonly ILogger<BabbleController> _logger;

    public BabbleController(
        IBabbleService babbleService,
        IPromptGenerationService promptGenerationService,
        IPromptTemplateService templateService,
        IGeneratedPromptService generatedPromptService,
        ILogger<BabbleController> logger)
    {
        _babbleService = babbleService;
        _promptGenerationService = promptGenerationService;
        _templateService = templateService;
        _generatedPromptService = generatedPromptService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBabbles(
        [FromQuery] string? continuationToken = null,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();

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
        var userId = User.GetUserIdOrAnonymous();
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
        var userId = User.GetUserIdOrAnonymous();
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

        var userId = User.GetUserIdOrAnonymous();
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
        var userId = User.GetUserIdOrAnonymous();
        var existing = await _babbleService.GetByIdAsync(userId, id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        await _babbleService.DeleteAsync(userId, id, cancellationToken);
        _logger.LogInformation("Deleted babble {BabbleId} for user {UserId}", id, userId);

        return NoContent();
    }

    [HttpPost("{id}/generate")]
    public async Task GeneratePrompt(
        string id,
        [FromBody] GeneratePromptRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();
        var babble = await _babbleService.GetByIdAsync(userId, id, cancellationToken);
        if (babble is null)
        {
            HttpContext.Response.StatusCode = 404;
            await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Babble Not Found",
                Status = 404,
                Detail = $"No babble found with ID '{id}'.",
            }, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Invalid Request",
                Status = 400,
                Detail = "TemplateId is required and cannot be empty.",
            }, cancellationToken);
            return;
        }

        var template = await _templateService.GetByIdAsync(null, request.TemplateId, cancellationToken);
        if (template is null)
        {
            HttpContext.Response.StatusCode = 404;
            await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Template Not Found",
                Status = 404,
                Detail = $"No template found with ID '{request.TemplateId}'.",
            }, cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            var fullText = new System.Text.StringBuilder();

            await foreach (var chunk in _promptGenerationService.GeneratePromptStreamAsync(
                babble.Text, template.SystemPrompt, request.PromptFormat, request.AllowEmojis, cancellationToken))
            {
                var textData = JsonSerializer.Serialize(new { text = chunk });
                await Response.WriteAsync($"data: {textData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                fullText.Append(chunk);
            }

            // Auto-persist the generated prompt
            var generatedPrompt = new GeneratedPrompt
            {
                Id = Guid.NewGuid().ToString(),
                BabbleId = babble.Id,
                UserId = userId,
                TemplateId = template.Id,
                TemplateName = template.Name,
                PromptText = fullText.ToString(),
                GeneratedAt = DateTimeOffset.UtcNow,
            };

            var created = await _generatedPromptService.CreateAsync(userId, generatedPrompt, cancellationToken);
            _logger.LogInformation("Auto-persisted generated prompt {PromptId} for babble {BabbleId}", created.Id, created.BabbleId);

            var promptIdData = JsonSerializer.Serialize(new { promptId = created.Id });
            await Response.WriteAsync($"data: {promptIdData}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Prompt generation failed for babble {BabbleId}", id);
            if (!Response.HasStarted)
            {
                HttpContext.Response.StatusCode = 502;
                await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "Azure OpenAI Error",
                    Status = 502,
                    Detail = "An error occurred while communicating with Azure OpenAI. Please try again.",
                }, cancellationToken);
            }
        }
    }

    [HttpPost("{id}/generate-title")]
    public async Task<IActionResult> GenerateTitle(
        string id,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();
        var babble = await _babbleService.GetByIdAsync(userId, id, cancellationToken);
        if (babble is null)
        {
            return NotFound();
        }

        try
        {
            var title = await _promptGenerationService.GenerateTitleAsync(babble.Text, cancellationToken);

            var updated = babble with
            {
                Title = title,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var result = await _babbleService.UpdateAsync(userId, updated, cancellationToken);
            _logger.LogInformation("Generated title for babble {BabbleId}: {Title}", result.Id, result.Title);

            return Ok(ToResponse(result));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Title generation failed for babble {BabbleId}", id);
            return StatusCode(502, new ProblemDetails
            {
                Title = "Azure OpenAI Error",
                Status = 502,
                Detail = "An error occurred while generating the title. Please try again.",
            });
        }
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
