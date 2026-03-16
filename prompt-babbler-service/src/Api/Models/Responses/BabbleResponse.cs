namespace PromptBabbler.Api.Models.Responses;

public sealed record BabbleResponse
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}
