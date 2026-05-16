namespace PromptBabbler.Api.Models.Responses;

public sealed record ExportSelectionResponse
{
    public bool IncludeBabbles { get; init; }

    public bool IncludeGeneratedPrompts { get; init; }

    public bool IncludeUserTemplates { get; init; }

    public bool IncludeSemanticVectors { get; init; }
}
