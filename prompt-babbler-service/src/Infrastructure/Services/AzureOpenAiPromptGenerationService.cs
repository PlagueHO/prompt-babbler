using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class AzureOpenAiPromptGenerationService(
    IChatClient chatClient,
    IPromptBuilder promptBuilder) : IPromptGenerationService
{
    public async IAsyncEnumerable<string> GeneratePromptStreamAsync(
        string babbleText,
        PromptTemplate template,
        string? promptFormat = null,
        bool? allowEmojis = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var effectiveFormat = promptFormat ?? template.DefaultOutputFormat ?? "text";
        var effectiveEmojis = allowEmojis ?? template.DefaultAllowEmojis ?? false;
        var effectiveSystemPrompt = promptBuilder.BuildSystemPrompt(template, effectiveFormat, effectiveEmojis);

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
