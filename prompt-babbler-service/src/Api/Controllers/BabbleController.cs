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
    private readonly IFileTranscriptionService _fileTranscriptionService;
    private readonly ILogger<BabbleController> _logger;

    public BabbleController(
        IBabbleService babbleService,
        IPromptGenerationService promptGenerationService,
        IPromptTemplateService templateService,
        IGeneratedPromptService generatedPromptService,
        IFileTranscriptionService fileTranscriptionService,
        ILogger<BabbleController> logger)
    {
        _babbleService = babbleService;
        _promptGenerationService = promptGenerationService;
        _templateService = templateService;
        _generatedPromptService = generatedPromptService;
        _fileTranscriptionService = fileTranscriptionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBabbles(
        [FromQuery] string? continuationToken = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] bool? isPinned = null,
        CancellationToken cancellationToken = default)
    {
        if (search is not null && search.Length > 200)
        {
            return BadRequest("search must be at most 200 characters.");
        }

        if (sortBy is not null && sortBy != "createdAt" && sortBy != "title")
        {
            return BadRequest("sortBy must be 'createdAt' or 'title'.");
        }

        if (sortDirection is not null && sortDirection != "desc" && sortDirection != "asc")
        {
            return BadRequest("sortDirection must be 'desc' or 'asc'.");
        }

        var userId = User.GetUserIdOrAnonymous();

        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, nextToken) = await _babbleService.GetByUserAsync(
            userId, continuationToken, pageSize, search, sortBy, sortDirection, isPinned, cancellationToken);

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
            Tags = request.Tags,
            IsPinned = request.IsPinned ?? false,
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
            Tags = request.Tags,
            IsPinned = request.IsPinned ?? existing.IsPinned,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var result = await _babbleService.UpdateAsync(userId, updated, cancellationToken);
        _logger.LogInformation("Updated babble {BabbleId} for user {UserId}", result.Id, result.UserId);

        return Ok(ToResponse(result));
    }

    [HttpPatch("{id}/pin")]
    public async Task<IActionResult> PinBabble(
        string id,
        [FromBody] PinBabbleRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();

        try
        {
            var result = await _babbleService.SetPinAsync(userId, id, request.IsPinned, cancellationToken);
            _logger.LogInformation("Set pin {IsPinned} on babble {BabbleId} for user {UserId}", request.IsPinned, id, userId);

            return Ok(ToResponse(result));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
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
                babble.Text, template, request.PromptFormat, request.AllowEmojis, cancellationToken))
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

    [Consumes("multipart/form-data")]
    [HttpPost("upload")]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)] // 500 MB — overrides the 128 MB default
    public async Task<IActionResult> UploadAudio(
        IFormFile file,
        [FromForm] string? title,
        [FromForm] string? language,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No audio file provided.");
        }

        string[] allowedTypes = ["audio/mpeg", "audio/mp3", "audio/wav", "audio/webm", "audio/ogg", "audio/mp4", "audio/x-m4a"];
        if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported audio format. Supported: MP3, WAV, WebM, OGG, M4A.");
        }

        string[] allowedExtensions = [".mp3", ".wav", ".webm", ".ogg", ".m4a"];
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported file extension. Supported: .mp3, .wav, .webm, .ogg, .m4a.");
        }

        if (title is not null && (title.Trim().Length == 0 || title.Length > 200))
        {
            return BadRequest("Title must be between 1 and 200 characters.");
        }

        if (language is not null && (language.Length > 20 || !System.Text.RegularExpressions.Regex.IsMatch(language, @"^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{1,8})*$")))
        {
            return BadRequest("Invalid language code. Provide a valid BCP-47 language tag (e.g., 'en-US').");
        }

        var userId = User.GetUserIdOrAnonymous();
        await using var stream = file.OpenReadStream();

        string transcribedText;
        try
        {
            transcribedText = await _fileTranscriptionService.TranscribeAsync(stream, language, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "File transcription failed for uploaded audio");
            return StatusCode(502, new ProblemDetails
            {
                Title = "Transcription Service Error",
                Status = 502,
                Detail = "An error occurred during transcription. Please try again.",
            });
        }

        if (string.IsNullOrWhiteSpace(transcribedText))
        {
            return BadRequest("Could not transcribe audio. The file may be empty or contain no speech.");
        }

        var now = DateTimeOffset.UtcNow;
        var babble = new Babble
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? GenerateTitleFromText(transcribedText) : title.Trim(),
            Text = transcribedText,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _babbleService.CreateAsync(babble, cancellationToken);
        _logger.LogInformation("Created babble {BabbleId} from uploaded audio for user {UserId}", created.Id, created.UserId);

        return CreatedAtAction(nameof(GetBabble), new { id = created.Id }, ToResponse(created));
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

        if (!ValidateTags(request.Tags, out error))
        {
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

        if (!ValidateTags(request.Tags, out error))
        {
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateTags(IReadOnlyList<string>? tags, out string? error)
    {
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

    private static BabbleResponse ToResponse(Babble babble) => new()
    {
        Id = babble.Id,
        Title = babble.Title,
        Text = babble.Text,
        Tags = babble.Tags,
        CreatedAt = babble.CreatedAt.ToString("o"),
        UpdatedAt = babble.UpdatedAt.ToString("o"),
        IsPinned = babble.IsPinned,
    };

    private static string GenerateTitleFromText(string text)
    {
        const int maxLength = 50;
        var title = text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "...";
        return title.Replace('\n', ' ').Replace('\r', ' ');
    }
}
