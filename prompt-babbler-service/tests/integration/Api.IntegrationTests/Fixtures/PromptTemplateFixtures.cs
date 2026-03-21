using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.IntegrationTests.Fixtures;

public static class PromptTemplateFixtures
{
    public const string TestUserId = "00000000-0000-0000-0000-000000000000";

    public static PromptTemplate CreateUserTemplate(
        string id = "template-user-1",
        string userId = TestUserId,
        string name = "User Template") => new()
        {
            Id = id,
            UserId = userId,
            Name = name,
            Description = "A test user template.",
            Instructions = "You are a helpful assistant.",
            IsBuiltIn = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    public static PromptTemplate CreateBuiltInTemplate(
        string id = "template-builtin-1",
        string name = "Built-in Template") => new()
        {
            Id = id,
            UserId = "_builtin",
            Name = name,
            Description = "A built-in template.",
            Instructions = "You are a built-in assistant.",
            IsBuiltIn = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
}
