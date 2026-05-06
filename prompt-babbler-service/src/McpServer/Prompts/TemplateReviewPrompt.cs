using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace PromptBabbler.McpServer.Prompts;

[McpServerPromptType]
public static class TemplateReviewPrompt
{
    [McpServerPrompt(Name = "review_template")]
    [Description("Seed a conversation to review and improve a prompt template. Provides structured guidance for evaluating clarity, completeness, and effectiveness.")]
    public static IEnumerable<ChatMessage> ReviewTemplate(
        [Description("The template instructions text to review")] string instructions,
        [Description("The template description to provide context")] string description)
    {
        yield return new ChatMessage(ChatRole.User,
            $"""
            Please review this prompt template and suggest improvements.

            Description: {description}

            Instructions:
            {instructions}

            Evaluate the template for:
            1. Clarity — are the instructions unambiguous?
            2. Completeness — are there missing edge cases or scenarios?
            3. Effectiveness — will the instructions produce high-quality prompts?
            4. Conciseness — can any instructions be simplified without losing meaning?

            Provide specific, actionable suggestions for each area.
            """);
    }
}
