using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record ImportExportCounts
{
    [JsonPropertyName("babblesImported")]
    public int BabblesImported { get; init; }

    [JsonPropertyName("babblesSkipped")]
    public int BabblesSkipped { get; init; }

    [JsonPropertyName("generatedPromptsImported")]
    public int GeneratedPromptsImported { get; init; }

    [JsonPropertyName("generatedPromptsSkipped")]
    public int GeneratedPromptsSkipped { get; init; }

    [JsonPropertyName("templatesImported")]
    public int TemplatesImported { get; init; }

    [JsonPropertyName("templatesSkipped")]
    public int TemplatesSkipped { get; init; }

    [JsonPropertyName("failed")]
    public int Failed { get; init; }
}
