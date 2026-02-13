using System.ClientModel;
using System.Runtime.CompilerServices;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class AzureOpenAiPromptGenerationService(ISettingsService settingsService) : IPromptGenerationService
{
    public async IAsyncEnumerable<string> GeneratePromptStreamAsync(
        string babbleText,
        string systemPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetSettingsAsync(cancellationToken)
            ?? throw new InvalidOperationException("LLM settings are not configured.");

        var client = new AzureOpenAIClient(
            new Uri(settings.Endpoint),
            new ApiKeyCredential(settings.ApiKey));

        var chatClient = client.GetChatClient(settings.DeploymentName);

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(babbleText),
        };

        AsyncCollectionResult<StreamingChatCompletionUpdate> updates =
            chatClient.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken);

        await foreach (var update in updates.WithCancellation(cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return part.Text;
                }
            }
        }
    }
}
