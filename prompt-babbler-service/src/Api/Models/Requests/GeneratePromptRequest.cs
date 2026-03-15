namespace PromptBabbler.Api.Models.Requests;

public sealed record GeneratePromptRequest
{
    public required string BabbleText { get; init; }
    public required string SystemPrompt { get; init; }
    public string? TemplateName { get; init; }
    public string PromptFormat { get; init; } = "text";
    public bool AllowEmojis { get; init; }
}
