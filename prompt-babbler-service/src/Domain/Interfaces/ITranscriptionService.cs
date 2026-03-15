using System.Threading.Channels;

namespace PromptBabbler.Domain.Interfaces;

/// <summary>
/// Provides real-time speech-to-text transcription over a streaming audio session.
/// </summary>
public interface IRealtimeTranscriptionService
{
    /// <summary>
    /// Starts a new transcription session.
    /// </summary>
    Task<TranscriptionSession> StartSessionAsync(
        string? language = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an active real-time transcription session that accepts audio input
/// and produces transcription events.
/// </summary>
public sealed class TranscriptionSession : IAsyncDisposable
{
    private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> _writeAudio;
    private readonly Func<Task> _complete;
    private readonly Func<ValueTask> _dispose;

    public TranscriptionSession(
        ChannelReader<TranscriptionEvent> results,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> writeAudio,
        Func<Task> complete,
        Func<ValueTask> dispose)
    {
        Results = results;
        _writeAudio = writeAudio;
        _complete = complete;
        _dispose = dispose;
    }

    /// <summary>
    /// Channel of transcription events (partial and final results).
    /// </summary>
    public ChannelReader<TranscriptionEvent> Results { get; }

    /// <summary>
    /// Write raw PCM audio data (16 kHz, 16-bit, mono) to the recognizer.
    /// </summary>
    public Task WriteAudioAsync(ReadOnlyMemory<byte> pcmData, CancellationToken cancellationToken = default)
        => _writeAudio(pcmData, cancellationToken);

    /// <summary>
    /// Signal that no more audio will be sent. The session will finish processing
    /// remaining audio and close the results channel.
    /// </summary>
    public Task CompleteAsync() => _complete();

    public ValueTask DisposeAsync() => _dispose();
}

/// <summary>
/// A single transcription event emitted during real-time recognition.
/// </summary>
public sealed record TranscriptionEvent
{
    /// <summary>The recognized text.</summary>
    public required string Text { get; init; }

    /// <summary>True when this is a finalized recognition result; false for interim/partial results.</summary>
    public required bool IsFinal { get; init; }

    /// <summary>Offset from the start of the audio stream.</summary>
    public TimeSpan? Offset { get; init; }

    /// <summary>Duration of the recognized segment.</summary>
    public TimeSpan? Duration { get; init; }
}
