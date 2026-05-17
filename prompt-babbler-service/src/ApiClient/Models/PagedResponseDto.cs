using System.Text.Json.Serialization;

namespace PromptBabbler.ApiClient.Models;

public sealed record PagedResponseDto<T>
{
    [JsonPropertyName("items")] public required IReadOnlyList<T> Items { get; init; }
    [JsonPropertyName("continuationToken")] public string? ContinuationToken { get; init; }
}
