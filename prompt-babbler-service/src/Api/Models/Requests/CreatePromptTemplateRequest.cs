namespace PromptBabbler.Api.Models.Requests;

public sealed record CreatePromptTemplateRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string SystemPrompt { get; init; }
}
