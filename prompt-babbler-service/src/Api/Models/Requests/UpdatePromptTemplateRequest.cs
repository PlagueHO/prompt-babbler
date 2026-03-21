using System.Text.Json;

namespace PromptBabbler.Api.Models.Requests;

public sealed record UpdatePromptTemplateRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Instructions { get; init; }
    public string? OutputDescription { get; init; }
    public string? OutputTemplate { get; init; }
    public IReadOnlyList<ExampleRequest>? Examples { get; init; }
    public IReadOnlyList<string>? Guardrails { get; init; }
    public string? DefaultOutputFormat { get; init; }
    public bool? DefaultAllowEmojis { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
