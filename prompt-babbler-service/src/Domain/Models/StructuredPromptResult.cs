namespace PromptBabbler.Domain.Models;

public sealed record StructuredPromptResult
{
    public required string Name { get; init; }
    public required string Prompt { get; init; }
}
