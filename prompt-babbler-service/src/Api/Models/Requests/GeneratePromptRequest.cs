namespace PromptBabbler.Api.Models.Requests;

public sealed record GeneratePromptRequest
{
    public required string BabbleText { get; init; }
    public required string TemplateId { get; init; }
    public string PromptFormat { get; init; } = "text";
    public bool AllowEmojis { get; init; }
}
