namespace PromptBabbler.Api.Models.Responses;

public sealed record LlmSettingsResponse
{
    public required string Endpoint { get; init; }
    public required string ApiKeyHint { get; init; }
    public required string DeploymentName { get; init; }
    public required string WhisperDeploymentName { get; init; }
    public required bool IsConfigured { get; init; }
}
