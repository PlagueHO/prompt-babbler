using System.Text.Json.Serialization;

namespace PromptBabbler.McpServer.Client.Models;

public sealed record BabbleDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("text")] public required string Text { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("createdAt")] public required string CreatedAt { get; init; }
    [JsonPropertyName("updatedAt")] public required string UpdatedAt { get; init; }
    [JsonPropertyName("isPinned")] public bool IsPinned { get; init; }
}
