using System.Text;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class PromptBuilder : IPromptBuilder
{
    private static readonly string[] AllowedFormats = ["text", "markdown"];

    public string BuildSystemPrompt(PromptTemplate template, string outputFormat, bool allowEmojis)
    {
        var sb = new StringBuilder();

        sb.Append(template.Instructions);

        if (!string.IsNullOrWhiteSpace(template.OutputDescription))
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("## Expected Output");
            sb.Append(template.OutputDescription);
        }

        if (!string.IsNullOrWhiteSpace(template.OutputTemplate))
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("## Output Template");
            sb.AppendLine("Follow this template format:");
            sb.Append(template.OutputTemplate);
        }

        if (template.Guardrails is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("## Guardrails");
            sb.AppendLine("You must NOT:");
            foreach (var guardrail in template.Guardrails)
            {
                sb.AppendLine($"- {guardrail}");
            }
        }

        if (template.Examples is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("## Examples");
            for (var i = 0; i < template.Examples.Count; i++)
            {
                var example = template.Examples[i];
                sb.AppendLine($"### Example {i + 1}");
                sb.AppendLine($"**Input:** {example.Input}");
                sb.AppendLine($"**Output:** {example.Output}");
            }
        }

        var format = AllowedFormats.Contains(outputFormat, StringComparer.OrdinalIgnoreCase)
            ? outputFormat
            : "text";

        var formatInstruction = string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase)
            ? "Respond using Markdown formatting."
            : "Respond in plain text without any Markdown formatting.";

        var emojiInstruction = allowEmojis
            ? "You may use emojis where appropriate."
            : "Do not use any emojis.";

        sb.AppendLine();
        sb.AppendLine();
        sb.Append(formatInstruction);
        sb.AppendLine();
        sb.Append(emojiInstruction);

        return sb.ToString();
    }
}
