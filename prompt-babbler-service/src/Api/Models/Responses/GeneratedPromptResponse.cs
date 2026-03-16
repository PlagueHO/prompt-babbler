namespace PromptBabbler.Api.Models.Responses;

public sealed record GeneratedPromptResponse
{
    public required string Id { get; init; }
    public required string BabbleId { get; init; }
    public required string TemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required string PromptText { get; init; }
    public required string GeneratedAt { get; init; }
}
