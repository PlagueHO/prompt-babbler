namespace PromptBabbler.Api.Models.Requests;

public sealed record GeneratePromptRequest
{
    public required string TemplateId { get; init; }
    public string? PromptFormat { get; init; }
    public bool? AllowEmojis { get; init; }
}
