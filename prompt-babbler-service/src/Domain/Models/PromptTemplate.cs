using System.Text.Json;
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

    [JsonPropertyName("instructions")]
    public required string Instructions { get; init; }

    [JsonPropertyName("outputDescription")]
    public string? OutputDescription { get; init; }

    [JsonPropertyName("outputTemplate")]
    public string? OutputTemplate { get; init; }

    [JsonPropertyName("examples")]
    public IReadOnlyList<PromptExample>? Examples { get; init; }

    [JsonPropertyName("guardrails")]
    public IReadOnlyList<string>? Guardrails { get; init; }

    [JsonPropertyName("defaultOutputFormat")]
    public string? DefaultOutputFormat { get; init; }

    [JsonPropertyName("defaultAllowEmojis")]
    public bool? DefaultAllowEmojis { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    [JsonPropertyName("additionalProperties")]
    public IReadOnlyDictionary<string, JsonElement>? AdditionalProperties { get; init; }

    [JsonPropertyName("isBuiltIn")]
    public required bool IsBuiltIn { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
