namespace PromptBabbler.Domain.Models;

public sealed record TemplateValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> Errors { get; init; }

    public static TemplateValidationResult Success() => new()
    {
        IsValid = true,
        Errors = [],
    };

    public static TemplateValidationResult Failure(IReadOnlyList<string> errors) => new()
    {
        IsValid = false,
        Errors = errors,
    };
}
