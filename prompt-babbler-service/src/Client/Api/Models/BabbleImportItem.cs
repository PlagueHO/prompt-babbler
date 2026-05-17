using System.Text.Json.Serialization;

namespace PromptBabbler.ApiClient.Models;

public sealed record BabbleImportItem
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    [JsonPropertyName("isPinned")]
    public bool? IsPinned { get; init; }
}
