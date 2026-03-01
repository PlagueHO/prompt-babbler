namespace PromptBabbler.Api.Models.Responses;

public sealed record TranscriptionResponse
{
    public required string Text { get; init; }
    public string? Language { get; init; }
    public float? Duration { get; init; }
}
