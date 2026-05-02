namespace PromptBabbler.Domain.Interfaces;

/// <summary>
/// Transcribes audio files to text using batch processing.
/// </summary>
public interface IFileTranscriptionService
{
    /// <summary>
    /// Transcribes the audio content from the provided stream.
    /// </summary>
    /// <param name="audioStream">The audio data stream (MP3, WAV, WebM, OGG).</param>
    /// <param name="language">Optional BCP-47 locale (defaults to "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcribed text.</returns>
    Task<string> TranscribeAsync(
        Stream audioStream,
        string? language = null,
        CancellationToken cancellationToken = default);
}
