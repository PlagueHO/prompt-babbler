using Microsoft.AspNetCore.Mvc;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Route("api/prompts")]
public sealed class PromptController(IPromptGenerationService promptService, ISettingsService settingsService) : ControllerBase
{
    [HttpPost("generate")]
    public async Task GeneratePrompt([FromBody] GeneratePromptRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BabbleText))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Invalid Request",
                Status = 400,
                Detail = "BabbleText is required and cannot be empty.",
            }, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Invalid Request",
                Status = 400,
                Detail = "SystemPrompt is required and cannot be empty.",
            }, cancellationToken);
            return;
        }

        var settings = await settingsService.GetSettingsAsync(cancellationToken);
        if (settings is null)
        {
            HttpContext.Response.StatusCode = 422;
            await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://promptbabbler.dev/errors/settings-not-configured",
                Title = "LLM Settings Not Configured",
                Status = 422,
                Detail = "Azure OpenAI settings must be configured before generating prompts. Please configure your endpoint, API key, and deployment name in Settings.",
            }, cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var chunk in promptService.GeneratePromptStreamAsync(
                request.BabbleText, request.SystemPrompt, cancellationToken))
            {
                var data = System.Text.Json.JsonSerializer.Serialize(new { text = chunk });
                await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            HttpContext.Response.StatusCode = 422;
            await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://promptbabbler.dev/errors/settings-not-configured",
                Title = "LLM Settings Not Configured",
                Status = 422,
                Detail = "Azure OpenAI settings must be configured before generating prompts.",
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (!Response.HasStarted)
            {
                HttpContext.Response.StatusCode = 502;
                await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "Azure OpenAI Error",
                    Status = 502,
                    Detail = $"An error occurred while communicating with Azure OpenAI: {ex.Message}",
                }, cancellationToken);
            }
        }
    }
}
