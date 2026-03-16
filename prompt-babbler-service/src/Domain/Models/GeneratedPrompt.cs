using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record GeneratedPrompt
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("babbleId")]
    public required string BabbleId { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("templateId")]
    public required string TemplateId { get; init; }

    [JsonPropertyName("templateName")]
    public required string TemplateName { get; init; }

    [JsonPropertyName("promptText")]
    public required string PromptText { get; init; }

    [JsonPropertyName("generatedAt")]
    public required DateTimeOffset GeneratedAt { get; init; }
}
