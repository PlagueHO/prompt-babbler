namespace PromptBabbler.Api.Models.Responses;

public sealed record ImportExportJobCountsResponse
{
    public int BabblesImported { get; init; }

    public int BabblesSkipped { get; init; }

    public int GeneratedPromptsImported { get; init; }

    public int GeneratedPromptsSkipped { get; init; }

    public int TemplatesImported { get; init; }

    public int TemplatesSkipped { get; init; }

    public int Failed { get; init; }
}
