namespace PromptBabbler.Domain.Interfaces;

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        string fileName,
        string? language = null,
        CancellationToken cancellationToken = default);
}

public sealed record TranscriptionResult
{
    public required string Text { get; init; }
    public string? Language { get; init; }
    public float? Duration { get; init; }
}
