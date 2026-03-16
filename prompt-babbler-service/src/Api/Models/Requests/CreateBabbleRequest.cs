namespace PromptBabbler.Api.Models.Requests;

public sealed record CreateBabbleRequest
{
    public required string Title { get; init; }
    public required string Text { get; init; }
}
