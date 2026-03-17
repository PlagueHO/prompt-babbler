namespace PromptBabbler.Api.Models.Responses;

public sealed record UserSettingsResponse
{
    public required string Theme { get; init; }
    public required string SpeechLanguage { get; init; }
}

public sealed record UserProfileResponse
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public required UserSettingsResponse Settings { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}
