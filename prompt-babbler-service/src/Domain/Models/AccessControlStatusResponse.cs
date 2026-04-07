using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record AccessControlStatusResponse
{
    [JsonPropertyName("accessCodeRequired")]
    public required bool AccessCodeRequired { get; init; }
}
