using System.Text.Json.Serialization;

namespace PromptBabbler.McpServer.Client.Models;

public sealed record PagedResponseDto<T>
{
    [JsonPropertyName("items")] public required IReadOnlyList<T> Items { get; init; }
    [JsonPropertyName("continuationToken")] public string? ContinuationToken { get; init; }
}
