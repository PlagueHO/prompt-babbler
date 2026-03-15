namespace PromptBabbler.Api.Models.Requests;

public sealed record UpdatePromptTemplateRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string SystemPrompt { get; init; }
}
