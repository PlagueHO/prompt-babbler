using System.ClientModel;
using Azure.AI.OpenAI;
using OpenAI.Audio;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class AzureOpenAiTranscriptionService(ISettingsService settingsService) : ITranscriptionService
{
    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        string fileName,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetSettingsAsync(cancellationToken)
            ?? throw new InvalidOperationException("LLM settings are not configured.");

        var client = new AzureOpenAIClient(
            new Uri(settings.Endpoint),
            new ApiKeyCredential(settings.ApiKey));

        var audioClient = client.GetAudioClient(settings.WhisperDeploymentName);

        var options = new AudioTranscriptionOptions();

        if (!string.IsNullOrEmpty(language))
        {
            options.Language = language;
        }

        var result = await audioClient.TranscribeAudioAsync(
            audioStream,
            fileName,
            options,
            cancellationToken);

        return new TranscriptionResult
        {
            Text = result.Value.Text,
            Language = result.Value.Language,
            Duration = (float?)result.Value.Duration?.TotalSeconds,
        };
    }
}
