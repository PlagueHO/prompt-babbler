using Microsoft.AspNetCore.Mvc;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class TranscriptionController(ITranscriptionService transcriptionService, ISettingsService settingsService) : ControllerBase
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    [HttpPost("transcribe")]
    public async Task<IActionResult> Transcribe(
        IFormFile file,
        [FromForm] string? language,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Status = 400,
                Detail = "An audio file is required.",
            });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "File Too Large",
                Status = 400,
                Detail = $"Audio file size exceeds the maximum limit of 25 MB. File size: {file.Length / (1024 * 1024):F1} MB.",
            });
        }

        var settings = await settingsService.GetSettingsAsync(cancellationToken);
        if (settings is null)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Type = "https://promptbabbler.dev/errors/settings-not-configured",
                Title = "LLM Settings Not Configured",
                Status = 422,
                Detail = "Azure OpenAI settings must be configured before transcribing audio. Please configure your endpoint, API key, and Whisper deployment name in Settings.",
            });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await transcriptionService.TranscribeAsync(
                stream, file.FileName, language, cancellationToken);

            return Ok(new TranscriptionResponse
            {
                Text = result.Text,
                Language = result.Language,
                Duration = result.Duration,
            });
        }
        catch (InvalidOperationException)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Type = "https://promptbabbler.dev/errors/settings-not-configured",
                Title = "LLM Settings Not Configured",
                Status = 422,
                Detail = "Azure OpenAI settings must be configured before transcribing audio.",
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(502, new ProblemDetails
            {
                Title = "Azure OpenAI Whisper Error",
                Status = 502,
                Detail = "An error occurred while communicating with Azure OpenAI Whisper. Please try again or check your settings.",
            });
        }
    }
}
