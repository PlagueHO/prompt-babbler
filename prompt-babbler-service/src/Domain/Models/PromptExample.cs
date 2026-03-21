using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record PromptExample
{
    [JsonPropertyName("input")]
    public required string Input { get; init; }

    [JsonPropertyName("output")]
    public required string Output { get; init; }
}
