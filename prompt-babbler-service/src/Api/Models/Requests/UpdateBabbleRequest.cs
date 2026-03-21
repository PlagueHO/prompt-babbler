namespace PromptBabbler.Api.Models.Requests;

public sealed record UpdateBabbleRequest
{
    public required string Title { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
}
