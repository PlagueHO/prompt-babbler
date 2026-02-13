namespace PromptBabbler.Api.Models.Responses;

public sealed record TestConnectionResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public int? LatencyMs { get; init; }
}
