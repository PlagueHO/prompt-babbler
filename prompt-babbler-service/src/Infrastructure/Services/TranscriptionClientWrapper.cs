using Azure.AI.Speech.Transcription;

namespace PromptBabbler.Infrastructure.Services;

/// <summary>
/// Wraps <see cref="TranscriptionClient"/> to support unit testing via <see cref="ITranscriptionClientWrapper"/>.
/// </summary>
public sealed class TranscriptionClientWrapper(TranscriptionClient client) : ITranscriptionClientWrapper
{
    public async Task<string> TranscribeAsync(
        Stream audioStream,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var options = new TranscriptionOptions(audioStream);
        options.Locales.Add(locale);

        var response = await client.TranscribeAsync(options, cancellationToken);
        return response.Value.CombinedPhrases.FirstOrDefault()?.Text ?? string.Empty;
    }
}
