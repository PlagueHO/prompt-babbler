using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record ExportManifestCounts
{
    [JsonPropertyName("babbles")]
    public int Babbles { get; init; }

    [JsonPropertyName("generatedPrompts")]
    public int GeneratedPrompts { get; init; }

    [JsonPropertyName("templates")]
    public int Templates { get; init; }
}
