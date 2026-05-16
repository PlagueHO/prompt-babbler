using System.Text.Json.Serialization;

namespace PromptBabbler.Tools.Cli.Models;

public sealed record ExportRequest
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
