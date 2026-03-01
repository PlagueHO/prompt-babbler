using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(ISettingsService settingsService, IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetSettingsAsync(cancellationToken);

        if (settings is null)
        {
            return Ok(new LlmSettingsResponse
            {
                Endpoint = string.Empty,
                ApiKeyHint = string.Empty,
                DeploymentName = string.Empty,
                WhisperDeploymentName = string.Empty,
                IsConfigured = false,
            });
        }

        return Ok(new LlmSettingsResponse
        {
            Endpoint = settings.Endpoint,
            ApiKeyHint = MaskApiKey(settings.ApiKey),
            DeploymentName = settings.DeploymentName,
            WhisperDeploymentName = settings.WhisperDeploymentName,
            IsConfigured = true,
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] LlmSettingsSaveRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Settings",
                Status = 400,
                Detail = "Endpoint is required.",
            });
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Settings",
                Status = 400,
                Detail = "API key is required.",
            });
        }

        if (string.IsNullOrWhiteSpace(request.DeploymentName))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Settings",
                Status = 400,
                Detail = "Deployment name is required.",
            });
        }

        if (string.IsNullOrWhiteSpace(request.WhisperDeploymentName))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Settings",
                Status = 400,
                Detail = "Whisper deployment name is required.",
            });
        }

        var settings = new LlmSettings
        {
            Endpoint = request.Endpoint.Trim(),
            ApiKey = request.ApiKey.Trim(),
            DeploymentName = request.DeploymentName.Trim(),
            WhisperDeploymentName = request.WhisperDeploymentName.Trim(),
        };

        await settingsService.SaveSettingsAsync(settings, cancellationToken);

        return Ok(new LlmSettingsResponse
        {
            Endpoint = settings.Endpoint,
            ApiKeyHint = MaskApiKey(settings.ApiKey),
            DeploymentName = settings.DeploymentName,
            WhisperDeploymentName = settings.WhisperDeploymentName,
            IsConfigured = true,
        });
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetSettingsAsync(cancellationToken);
        if (settings is null)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Type = "https://promptbabbler.dev/errors/settings-not-configured",
                Title = "LLM Settings Not Configured",
                Status = 422,
                Detail = "Settings must be configured before testing the connection.",
            });
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("api-key", settings.ApiKey);
            var response = await httpClient.GetAsync(
                $"{settings.Endpoint.TrimEnd('/')}/openai/models?api-version=2024-06-01",
                cancellationToken);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return Ok(new TestConnectionResponse
                {
                    Success = true,
                    Message = "Successfully connected to Azure OpenAI endpoint.",
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                });
            }

            return Ok(new TestConnectionResponse
            {
                Success = false,
                Message = $"Connection failed with status {(int)response.StatusCode}: {response.ReasonPhrase}. Please verify your API key and endpoint.",
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Ok(new TestConnectionResponse
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}",
            });
        }
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 4)
        {
            return string.Empty;
        }

        return $"...{apiKey[^4..]}";
    }
}
