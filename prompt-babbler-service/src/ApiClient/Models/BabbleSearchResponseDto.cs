using System.Text.Json.Serialization;

namespace PromptBabbler.ApiClient.Models;

public sealed record BabbleSearchResponseDto
{
    [JsonPropertyName("results")] public required IReadOnlyList<BabbleSearchResultItemDto> Results { get; init; }
}

public sealed record BabbleSearchResultItemDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("text")] public string? Text { get; init; }
    [JsonPropertyName("snippet")] public string? Snippet { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("createdAt")] public required string CreatedAt { get; init; }
    [JsonPropertyName("isPinned")] public bool IsPinned { get; init; }
    [JsonPropertyName("score")] public double Score { get; init; }
}
