namespace PromptBabbler.Api.Models.Responses;

public sealed record ImportExportJobResponse
{
    public required string Id { get; init; }

    public required string JobType { get; init; }

    public required string Status { get; init; }

    public required string CreatedAt { get; init; }

    public string? StartedAt { get; init; }

    public string? CompletedAt { get; init; }

    public int ProgressPercentage { get; init; }

    public string? CurrentStage { get; init; }

    public int TotalItems { get; init; }

    public int ProcessedItems { get; init; }

    public string? ErrorMessage { get; init; }

    public bool OverwriteExisting { get; init; }

    public ExportSelectionResponse? ExportSelection { get; init; }

    public ImportExportJobCountsResponse? Counts { get; init; }
}
