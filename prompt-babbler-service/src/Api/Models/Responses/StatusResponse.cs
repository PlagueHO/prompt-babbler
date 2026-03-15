namespace PromptBabbler.Api.Models.Responses;

public sealed record StatusResponse
{
    public required string Status { get; init; }
}
