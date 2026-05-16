using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record ExportSelection
{
    [JsonPropertyName("includeBabbles")]
    public bool IncludeBabbles { get; init; }

    [JsonPropertyName("includeGeneratedPrompts")]
    public bool IncludeGeneratedPrompts { get; init; }

    [JsonPropertyName("includeUserTemplates")]
    public bool IncludeUserTemplates { get; init; }

    [JsonPropertyName("includeSemanticVectors")]
    public bool IncludeSemanticVectors { get; init; }
}
