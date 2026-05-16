using System.Text.Json.Serialization;

namespace PromptBabbler.Tools.Cli.Models;

public sealed record ImportExportJobResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("currentStage")]
    public string? CurrentStage { get; init; }

    [JsonPropertyName("progressPercentage")]
    public int ProgressPercentage { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}
