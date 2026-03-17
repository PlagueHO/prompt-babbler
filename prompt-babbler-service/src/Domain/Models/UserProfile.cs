using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record UserSettings
{
    [JsonPropertyName("theme")]
    public required string Theme { get; init; }

    [JsonPropertyName("speechLanguage")]
    public required string SpeechLanguage { get; init; }

    public static UserSettings Default => new()
    {
        Theme = "system",
        SpeechLanguage = "",
    };
}

public sealed record UserProfile
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("settings")]
    public required UserSettings Settings { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
