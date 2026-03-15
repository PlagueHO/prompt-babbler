using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record PromptTemplate
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("systemPrompt")]
    public required string SystemPrompt { get; init; }

    [JsonPropertyName("isBuiltIn")]
    public required bool IsBuiltIn { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
