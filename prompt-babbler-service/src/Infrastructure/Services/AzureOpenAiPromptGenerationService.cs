using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Infrastructure.Services;

public sealed class AzureOpenAiPromptGenerationService(IChatClient chatClient) : IPromptGenerationService
{
    public async IAsyncEnumerable<string> GeneratePromptStreamAsync(
        string babbleText,
        string systemPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
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
}
