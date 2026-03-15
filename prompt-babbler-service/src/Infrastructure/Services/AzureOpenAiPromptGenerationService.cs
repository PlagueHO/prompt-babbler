using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed partial class AzureOpenAiPromptGenerationService(IChatClient chatClient) : IPromptGenerationService
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

    public async Task<StructuredPromptResult> GenerateStructuredPromptAsync(
        string babbleText,
        string systemPrompt,
        string templateName,
        string promptFormat = "text",
        bool allowEmojis = false,
        CancellationToken cancellationToken = default)
    {
        var effectiveSystemPrompt = BuildSystemPrompt(systemPrompt, promptFormat, allowEmojis);

        var wrappedSystemPrompt = $"""
            {effectiveSystemPrompt}

            Additionally, you MUST respond with a JSON object containing exactly two fields:
            - "name": a short descriptive title (3-8 words) summarizing the babble content, suitable as a name for the babble recording (do NOT use the template name "{templateName}" as the title)
            - "prompt": the refined prompt text

            Respond ONLY with valid JSON. Do not wrap in markdown code fences.
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, wrappedSystemPrompt),
            new(ChatRole.User, babbleText),
        };

        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json,
        };

        var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);
        var responseText = response.Text ?? "";

        // Strip markdown code fences if present (e.g. ```json ... ```)
        responseText = CodeFenceRegex().Replace(responseText, "$1").Trim();

        var result = JsonSerializer.Deserialize<StructuredPromptResult>(responseText, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse structured prompt response from LLM.");

        return result;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [GeneratedRegex(@"^```(?:json)?\s*\n?(.*?)\n?\s*```$", RegexOptions.Singleline)]
    private static partial Regex CodeFenceRegex();
}
