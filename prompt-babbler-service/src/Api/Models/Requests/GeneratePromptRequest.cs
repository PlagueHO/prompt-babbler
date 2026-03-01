namespace PromptBabbler.Api.Models.Requests;

public sealed record GeneratePromptRequest
{
    public required string BabbleText { get; init; }
    public required string SystemPrompt { get; init; }
}
