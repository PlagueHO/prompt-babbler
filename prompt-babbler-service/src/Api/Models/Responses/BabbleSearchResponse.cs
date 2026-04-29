namespace PromptBabbler.Api.Models.Responses;

public sealed record BabbleSearchResultItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Snippet { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public bool IsPinned { get; init; }
    public required double Score { get; init; }
}

public sealed record BabbleSearchResponse
{
    public required IReadOnlyList<BabbleSearchResultItem> Results { get; init; }
}
