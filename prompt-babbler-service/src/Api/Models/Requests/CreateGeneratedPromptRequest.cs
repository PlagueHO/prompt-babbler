namespace PromptBabbler.Api.Models.Requests;

public sealed record CreateGeneratedPromptRequest
{
    public required string TemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required string PromptText { get; init; }
}
