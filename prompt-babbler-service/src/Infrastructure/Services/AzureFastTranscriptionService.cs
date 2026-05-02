using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Infrastructure.Services;

/// <summary>
/// Transcribes audio files using Azure Fast Transcription API.
/// </summary>
public sealed class AzureFastTranscriptionService(
    ITranscriptionClientWrapper transcriptionClient,
    ILogger<AzureFastTranscriptionService> logger) : IFileTranscriptionService
{
    public async Task<string> TranscribeAsync(
        Stream audioStream,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var locale = language ?? "en-US";

        logger.LogInformation("Starting fast transcription for locale {Locale}", locale);

        var transcribedText = await transcriptionClient.TranscribeAsync(audioStream, locale, cancellationToken);

        logger.LogInformation("Fast transcription completed: {CharCount} characters", transcribedText.Length);

        return transcribedText;
    }
}
