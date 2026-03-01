namespace PromptBabbler.Api.Models.Requests;

public sealed record LlmSettingsSaveRequest
{
    public required string Endpoint { get; init; }
    public required string ApiKey { get; init; }
    public required string DeploymentName { get; init; }
    public required string WhisperDeploymentName { get; init; }
}
