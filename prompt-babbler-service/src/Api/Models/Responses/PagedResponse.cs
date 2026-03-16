namespace PromptBabbler.Api.Models.Responses;

public sealed record PagedResponse<T>
{
    public required IEnumerable<T> Items { get; init; }
    public string? ContinuationToken { get; init; }
}
