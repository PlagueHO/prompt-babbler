using System.Text.Json;
using System.Text.Json.Serialization;

namespace PromptBabbler.McpServer.Client.Models;

public sealed record PromptExampleDto
{
    [JsonPropertyName("input")] public required string Input { get; init; }
    [JsonPropertyName("output")] public required string Output { get; init; }
}

public sealed record PromptTemplateDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("description")] public required string Description { get; init; }
    [JsonPropertyName("instructions")] public required string Instructions { get; init; }
    [JsonPropertyName("outputDescription")] public string? OutputDescription { get; init; }
    [JsonPropertyName("outputTemplate")] public string? OutputTemplate { get; init; }
    [JsonPropertyName("defaultOutputFormat")] public string? DefaultOutputFormat { get; init; }
    [JsonPropertyName("defaultAllowEmojis")] public bool? DefaultAllowEmojis { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("examples")] public IReadOnlyList<PromptExampleDto>? Examples { get; init; }
    [JsonPropertyName("guardrails")] public IReadOnlyList<string>? Guardrails { get; init; }
    [JsonPropertyName("additionalProperties")] public IReadOnlyDictionary<string, JsonElement>? AdditionalProperties { get; init; }
    [JsonPropertyName("isBuiltIn")] public bool IsBuiltIn { get; init; }
    [JsonPropertyName("createdAt")] public required string CreatedAt { get; init; }
    [JsonPropertyName("updatedAt")] public required string UpdatedAt { get; init; }
}

public sealed record CreatePromptTemplateRequest
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("description")] public required string Description { get; init; }
    [JsonPropertyName("instructions")] public required string Instructions { get; init; }
    [JsonPropertyName("outputDescription")] public string? OutputDescription { get; init; }
    [JsonPropertyName("defaultOutputFormat")] public string? DefaultOutputFormat { get; init; }
    [JsonPropertyName("defaultAllowEmojis")] public bool? DefaultAllowEmojis { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
}

public sealed record UpdatePromptTemplateRequest
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("description")] public required string Description { get; init; }
    [JsonPropertyName("instructions")] public required string Instructions { get; init; }
    [JsonPropertyName("outputDescription")] public string? OutputDescription { get; init; }
    [JsonPropertyName("defaultOutputFormat")] public string? DefaultOutputFormat { get; init; }
    [JsonPropertyName("defaultAllowEmojis")] public bool? DefaultAllowEmojis { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
}
