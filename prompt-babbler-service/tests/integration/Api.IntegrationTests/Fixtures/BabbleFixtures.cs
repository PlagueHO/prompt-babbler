using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.IntegrationTests.Fixtures;

public static class BabbleFixtures
{
    public const string TestUserId = "00000000-0000-0000-0000-000000000000";

    public static Babble CreateBabble(
        string id = "test-babble-id",
        string userId = TestUserId,
        string title = "Test Babble",
        string text = "This is a test babble transcription.") => new()
        {
            Id = id,
            UserId = userId,
            Title = title,
            Text = text,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
}
