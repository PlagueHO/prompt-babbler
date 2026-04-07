namespace PromptBabbler.Domain.Configuration;

public sealed record AccessControlOptions
{
    public const string SectionName = "AccessControl";

    public string? AccessCode { get; init; }
}
