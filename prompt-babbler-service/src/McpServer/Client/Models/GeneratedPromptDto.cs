using System.Text.Json.Serialization;

namespace PromptBabbler.McpServer.Client.Models;

public sealed record GeneratedPromptDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("babbleId")] public required string BabbleId { get; init; }
    [JsonPropertyName("templateId")] public required string TemplateId { get; init; }
    [JsonPropertyName("templateName")] public required string TemplateName { get; init; }
    [JsonPropertyName("promptText")] public required string PromptText { get; init; }
    [JsonPropertyName("generatedAt")] public required string GeneratedAt { get; init; }
}
