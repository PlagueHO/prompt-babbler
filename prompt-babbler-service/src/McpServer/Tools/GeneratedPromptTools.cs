using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PromptBabbler.ApiClient;

namespace PromptBabbler.McpServer.Tools;

[McpServerToolType]
public sealed class GeneratedPromptTools(IPromptBabblerApiClient apiClient)
{
    [McpServerTool(Name = "list_generated_prompts", ReadOnly = true)]
    [Description("List all generated prompts for a specific babble, with pagination support.")]
    public async Task<string> ListGeneratedPrompts(
        [Description("The ID of the babble to list generated prompts for")] string babbleId,
        [Description("Continuation token from a previous list call for pagination (optional)")] string? continuationToken = null,
        [Description("Number of generated prompts to return per page (1-100)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var results = await apiClient.ListGeneratedPromptsAsync(babbleId, continuationToken, pageSize, cancellationToken);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "get_generated_prompt", ReadOnly = true)]
    [Description("Get a single generated prompt by its ID. Returns null if not found.")]
    public async Task<string?> GetGeneratedPrompt(
        [Description("The ID of the babble the generated prompt belongs to")] string babbleId,
        [Description("The unique identifier of the generated prompt")] string id,
        CancellationToken cancellationToken = default)
    {
        var prompt = await apiClient.GetGeneratedPromptAsync(babbleId, id, cancellationToken);
        return prompt is null ? null : JsonSerializer.Serialize(prompt);
    }
}
