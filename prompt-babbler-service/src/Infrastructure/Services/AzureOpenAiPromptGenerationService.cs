using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Infrastructure.Services;

public sealed class AzureOpenAiPromptGenerationService(IChatClient chatClient) : IPromptGenerationService
{
    private static readonly string[] AllowedFormats = ["text", "markdown"];

    private static string BuildSystemPrompt(string systemPrompt, string promptFormat, bool allowEmojis)
    {
        var format = AllowedFormats.Contains(promptFormat, StringComparer.OrdinalIgnoreCase)
            ? promptFormat
            : "text";

        var formatInstruction = string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase)
            ? "Respond using Markdown formatting."
            : "Respond in plain text without any Markdown formatting.";

        var emojiInstruction = allowEmojis
            ? "You may use emojis where appropriate."
            : "Do not use any emojis.";

        return $"{systemPrompt}\n\n{formatInstruction}\n{emojiInstruction}";
    }

    public async IAsyncEnumerable<string> GeneratePromptStreamAsync(
        string babbleText,
        string systemPrompt,
        string promptFormat = "text",
        bool allowEmojis = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var effectiveSystemPrompt = BuildSystemPrompt(systemPrompt, promptFormat, allowEmojis);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, effectiveSystemPrompt),
            new(ChatRole.User, babbleText),
        };

        await foreach (var update in chatClient.GetStreamingResponseAsync(
            messages, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }
    }

    public async Task<string> GenerateTitleAsync(
        string babbleText,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Generate a short descriptive title (3-8 words) summarizing the following text. Respond with ONLY the title, no quotes or extra text."),
            new(ChatRole.User, babbleText),
        };

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        return (response.Text ?? "").Trim();
    }
}
