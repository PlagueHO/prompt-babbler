namespace PromptBabbler.Api.Models.Requests;

public sealed record UpdateUserSettingsRequest
{
    public required string Theme { get; init; }
    public required string SpeechLanguage { get; init; }
}
