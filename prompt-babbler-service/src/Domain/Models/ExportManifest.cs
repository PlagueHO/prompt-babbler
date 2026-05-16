using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record ExportManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("exportedAt")]
    public required DateTimeOffset ExportedAt { get; init; }

    [JsonPropertyName("sourceUserId")]
    public required string SourceUserId { get; init; }

    [JsonPropertyName("appVersion")]
    public string? AppVersion { get; init; }

    [JsonPropertyName("selection")]
    public required ExportSelection Selection { get; init; }

    [JsonPropertyName("counts")]
    public required ExportManifestCounts Counts { get; init; }
}
