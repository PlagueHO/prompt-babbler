using System.Text.Json.Serialization;

namespace PromptBabbler.Api.Models.Requests;

public sealed record PinBabbleRequest
{
    [JsonPropertyName("isPinned")]
    public required bool IsPinned { get; init; }
}
