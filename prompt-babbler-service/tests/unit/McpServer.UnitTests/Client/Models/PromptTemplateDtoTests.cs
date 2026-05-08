using System.Text.Json;
using FluentAssertions;
using PromptBabbler.McpServer.Client.Models;

namespace PromptBabbler.McpServer.UnitTests.Client.Models;

[TestClass]
[TestCategory("Unit")]
public sealed class PromptTemplateDtoTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [TestMethod]
    public void Deserialize_WithGuardrailsArray_SucceedsAndMapsCorrectly()
    {
        const string json = """
            {
                "id": "builtin-test",
                "name": "Test Template",
                "description": "A test",
                "instructions": "Do stuff",
                "guardrails": ["Do not do X", "Do not do Y"],
                "isBuiltIn": true,
                "createdAt": "2026-01-01T00:00:00Z",
                "updatedAt": "2026-01-01T00:00:00Z"
            }
            """;

        var dto = JsonSerializer.Deserialize<PromptTemplateDto>(json, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Guardrails.Should().BeEquivalentTo(["Do not do X", "Do not do Y"]);
    }

    [TestMethod]
    public void Deserialize_WithExamplesArray_SucceedsAndMapsCorrectly()
    {
        const string json = """
            {
                "id": "builtin-test",
                "name": "Test Template",
                "description": "A test",
                "instructions": "Do stuff",
                "examples": [
                    { "input": "sample input", "output": "sample output" }
                ],
                "isBuiltIn": true,
                "createdAt": "2026-01-01T00:00:00Z",
                "updatedAt": "2026-01-01T00:00:00Z"
            }
            """;

        var dto = JsonSerializer.Deserialize<PromptTemplateDto>(json, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Examples.Should().HaveCount(1);
        dto.Examples![0].Input.Should().Be("sample input");
        dto.Examples[0].Output.Should().Be("sample output");
    }

    [TestMethod]
    public void Deserialize_WithAdditionalPropertiesObject_SucceedsAndMapsCorrectly()
    {
        const string json = """
            {
                "id": "builtin-test",
                "name": "Test Template",
                "description": "A test",
                "instructions": "Do stuff",
                "additionalProperties": { "foo": "bar", "count": 42 },
                "isBuiltIn": false,
                "createdAt": "2026-01-01T00:00:00Z",
                "updatedAt": "2026-01-01T00:00:00Z"
            }
            """;

        var dto = JsonSerializer.Deserialize<PromptTemplateDto>(json, JsonOptions);

        dto.Should().NotBeNull();
        dto!.AdditionalProperties.Should().ContainKey("foo");
        dto.AdditionalProperties!["foo"].GetString().Should().Be("bar");
        dto.AdditionalProperties["count"].GetInt32().Should().Be(42);
    }

    [TestMethod]
    public void Deserialize_WithNullOptionalFields_Succeeds()
    {
        const string json = """
            {
                "id": "builtin-test",
                "name": "Test Template",
                "description": "A test",
                "instructions": "Do stuff",
                "isBuiltIn": true,
                "createdAt": "2026-01-01T00:00:00Z",
                "updatedAt": "2026-01-01T00:00:00Z"
            }
            """;

        var dto = JsonSerializer.Deserialize<PromptTemplateDto>(json, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Guardrails.Should().BeNull();
        dto.Examples.Should().BeNull();
        dto.AdditionalProperties.Should().BeNull();
    }

    [TestMethod]
    public void Deserialize_List_WithBuiltinTemplateShape_SucceedsForAllItems()
    {
        const string json = """
            [
                {
                    "id": "builtin-github-issues",
                    "name": "GitHub Issues",
                    "description": "Creates GitHub issues",
                    "instructions": "Create issues",
                    "guardrails": ["Do not invent issues", "Do not add labels not mentioned"],
                    "examples": [
                        { "input": "I want a login feature", "output": "Feature: Login page" }
                    ],
                    "defaultOutputFormat": "markdown",
                    "defaultAllowEmojis": false,
                    "tags": ["github", "issues"],
                    "isBuiltIn": true,
                    "createdAt": "2026-01-01T00:00:00Z",
                    "updatedAt": "2026-01-01T00:00:00Z"
                }
            ]
            """;

        var dtos = JsonSerializer.Deserialize<IReadOnlyList<PromptTemplateDto>>(json, JsonOptions);

        dtos.Should().HaveCount(1);
        var dto = dtos![0];
        dto.Id.Should().Be("builtin-github-issues");
        dto.Guardrails.Should().HaveCount(2);
        dto.Examples.Should().HaveCount(1);
        dto.DefaultOutputFormat.Should().Be("markdown");
        dto.DefaultAllowEmojis.Should().BeFalse();
        dto.Tags.Should().BeEquivalentTo(["github", "issues"]);
    }
}
