namespace PromptBabbler.Domain.Models;

public sealed record ImportExportJobQueueItem
{
    public required string JobId { get; init; }

    public required string UserId { get; init; }
}
