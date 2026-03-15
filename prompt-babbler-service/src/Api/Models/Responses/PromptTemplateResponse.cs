namespace PromptBabbler.Api.Models.Responses;

public sealed record PromptTemplateResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string SystemPrompt { get; init; }
    public required bool IsBuiltIn { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}
