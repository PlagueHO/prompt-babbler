namespace PromptBabbler.Infrastructure.Services;

/// <summary>
/// Abstracts the Azure Fast Transcription client to enable unit testing.
/// </summary>
public interface ITranscriptionClientWrapper
{
    /// <summary>
    /// Transcribes audio content from the provided stream using the specified locale.
    /// </summary>
    /// <param name="audioStream">The audio data stream.</param>
    /// <param name="locale">BCP-47 locale (e.g. "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcribed text, or an empty string when no speech is detected.</returns>
    Task<string> TranscribeAsync(
        Stream audioStream,
        string locale,
        CancellationToken cancellationToken = default);
}
