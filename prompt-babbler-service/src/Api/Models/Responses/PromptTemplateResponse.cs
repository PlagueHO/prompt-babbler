using System.Text.Json;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.Models.Responses;

public sealed record PromptTemplateResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Instructions { get; init; }
    public string? OutputDescription { get; init; }
    public string? OutputTemplate { get; init; }
    public IReadOnlyList<PromptExample>? Examples { get; init; }
    public IReadOnlyList<string>? Guardrails { get; init; }
    public string? DefaultOutputFormat { get; init; }
    public bool? DefaultAllowEmojis { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? AdditionalProperties { get; init; }
    public required bool IsBuiltIn { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}
