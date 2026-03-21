using FluentAssertions;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class PromptBuilderTests
{
    private readonly PromptBuilder _builder = new();

    private static PromptTemplate CreateTemplate(
        string instructions = "You are a helpful assistant.",
        string? outputDescription = null,
        string? outputTemplate = null,
        IReadOnlyList<PromptExample>? examples = null,
        IReadOnlyList<string>? guardrails = null) => new()
        {
            Id = "test-id",
            UserId = "user-1",
            Name = "Test",
            Description = "Test template",
            Instructions = instructions,
            OutputDescription = outputDescription,
            OutputTemplate = outputTemplate,
            Examples = examples,
            Guardrails = guardrails,
            IsBuiltIn = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    [TestMethod]
    public void BuildSystemPrompt_InstructionsOnly_StartsWithInstructions()
    {
        var template = CreateTemplate();

        var result = _builder.BuildSystemPrompt(template, "text", false);

        result.Should().StartWith("You are a helpful assistant.");
    }

    [TestMethod]
    public void BuildSystemPrompt_PlainText_ContainsPlainTextInstruction()
    {
        var template = CreateTemplate();

        var result = _builder.BuildSystemPrompt(template, "text", false);

        result.Should().Contain("Respond in plain text without any Markdown formatting.");
    }

    [TestMethod]
    public void BuildSystemPrompt_Markdown_ContainsMarkdownInstruction()
    {
        var template = CreateTemplate();

        var result = _builder.BuildSystemPrompt(template, "markdown", false);

        result.Should().Contain("Respond using Markdown formatting.");
    }

    [TestMethod]
    public void BuildSystemPrompt_AllowEmojis_ContainsEmojiInstruction()
    {
        var template = CreateTemplate();

        var result = _builder.BuildSystemPrompt(template, "text", true);

        result.Should().Contain("You may use emojis where appropriate.");
    }

    [TestMethod]
    public void BuildSystemPrompt_NoEmojis_ContainsNoEmojiInstruction()
    {
        var template = CreateTemplate();

        var result = _builder.BuildSystemPrompt(template, "text", false);

        result.Should().Contain("Do not use any emojis.");
    }

    [TestMethod]
    public void BuildSystemPrompt_WithOutputDescription_IncludesExpectedOutputSection()
    {
        var template = CreateTemplate(outputDescription: "A concise summary.");

        var result = _builder.BuildSystemPrompt(template, "text", false);

        result.Should().Contain("## Expected Output");
        result.Should().Contain("A concise summary.");
    }

    [TestMethod]
    public void BuildSystemPrompt_WithOutputTemplate_IncludesTemplateSection()
    {
        var template = CreateTemplate(outputTemplate: "Title: {{title}}");

        var result = _builder.BuildSystemPrompt(template, "text", false);

        result.Should().Contain("## Output Template");
        result.Should().Contain("Title: {{title}}");
    }

    [TestMethod]
    public void BuildSystemPrompt_WithGuardrails_IncludesGuardrailsList()
    {
        var template = CreateTemplate(guardrails: ["Don't be rude", "Don't hallucinate"]);

        var result = _builder.BuildSystemPrompt(template, "text", false);

        result.Should().Contain("## Guardrails");
        result.Should().Contain("You must NOT:");
        result.Should().Contain("- Don't be rude");
        result.Should().Contain("- Don't hallucinate");
    }

    [TestMethod]
    public void BuildSystemPrompt_WithExamples_IncludesExamplesSection()
    {
        var examples = new List<PromptExample>
        {
            new() { Input = "raw input", Output = "refined output" },
        };
        var template = CreateTemplate(examples: examples);

        var result = _builder.BuildSystemPrompt(template, "text", false);

        result.Should().Contain("## Examples");
        result.Should().Contain("**Input:** raw input");
        result.Should().Contain("**Output:** refined output");
    }

    [TestMethod]
    public void BuildSystemPrompt_UnknownFormat_DefaultsToPlainText()
    {
        var template = CreateTemplate();

        var result = _builder.BuildSystemPrompt(template, "html", false);

        result.Should().Contain("Respond in plain text without any Markdown formatting.");
    }
}
