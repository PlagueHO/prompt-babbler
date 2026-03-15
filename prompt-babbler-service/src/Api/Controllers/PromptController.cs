using Microsoft.AspNetCore.Mvc;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Route("api/prompts")]
public sealed class PromptController(
    IPromptGenerationService promptService,
    ILogger<PromptController> logger) : ControllerBase
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

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            if (!string.IsNullOrWhiteSpace(request.TemplateName))
            {
                var result = await promptService.GenerateStructuredPromptAsync(
                    request.BabbleText, request.SystemPrompt, request.TemplateName,
                    request.PromptFormat, request.AllowEmojis, cancellationToken);

                var nameData = System.Text.Json.JsonSerializer.Serialize(new { name = result.Name });
                await Response.WriteAsync($"data: {nameData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                var textData = System.Text.Json.JsonSerializer.Serialize(new { text = result.Prompt });
                await Response.WriteAsync($"data: {textData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            else
            {
                await foreach (var chunk in promptService.GeneratePromptStreamAsync(
                    request.BabbleText, request.SystemPrompt,
                    request.PromptFormat, request.AllowEmojis, cancellationToken))
                {
                    var data = System.Text.Json.JsonSerializer.Serialize(new { text = chunk });
                    await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Prompt generation failed");
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
}
