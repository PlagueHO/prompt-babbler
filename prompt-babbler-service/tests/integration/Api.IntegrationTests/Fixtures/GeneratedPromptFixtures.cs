using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.IntegrationTests.Fixtures;

public static class GeneratedPromptFixtures
{
    public const string TestUserId = "00000000-0000-0000-0000-000000000000";

    public static GeneratedPrompt CreateGeneratedPrompt(
        string id = "test-prompt-id",
        string babbleId = "test-babble-id",
        string userId = TestUserId,
        string templateId = "template-1",
        string templateName = "Test Template",
        string promptText = "Generated prompt text.") => new()
        {
            Id = id,
            BabbleId = babbleId,
            UserId = userId,
            TemplateId = templateId,
            TemplateName = templateName,
            PromptText = promptText,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
}
