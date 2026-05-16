namespace PromptBabbler.Api.Models.Requests;

public sealed record StartExportRequest
{
    public bool IncludeBabbles { get; init; }

    public bool IncludeGeneratedPrompts { get; init; }

    public bool IncludeUserTemplates { get; init; }

    public bool IncludeSemanticVectors { get; init; }
}
