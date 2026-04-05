using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record DependencyStatus
{
    [JsonPropertyName("status")]
    public required DependencyHealth Status { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("durationMs")]
    public required long DurationMs { get; init; }
}
