using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record StatusResponse
{
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("environment")]
    public required string Environment { get; init; }

    [JsonPropertyName("overall")]
    public required DependencyHealth Overall { get; init; }

    [JsonPropertyName("managedIdentity")]
    public required DependencyStatus ManagedIdentity { get; init; }

    [JsonPropertyName("cosmosDb")]
    public required DependencyStatus CosmosDb { get; init; }

    [JsonPropertyName("aiFoundry")]
    public required DependencyStatus AiFoundry { get; init; }
}
