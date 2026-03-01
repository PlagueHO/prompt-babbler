namespace PromptBabbler.Api.Models.Responses;

public sealed record GeneratePromptResponse
{
    public required string PromptText { get; init; }
}
