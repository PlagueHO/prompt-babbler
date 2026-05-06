using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PromptBabbler.McpServer.Client;

namespace PromptBabbler.McpServer.Tools;

[McpServerToolType]
public sealed class BabbleTools(IPromptBabblerApiClient apiClient)
{
    [McpServerTool(Name = "search_babbles", ReadOnly = true)]
    [Description("Search babbles using semantic/vector search. Returns babbles ranked by relevance to the query.")]
    public async Task<string> SearchBabbles(
        [Description("The search query text")] string query,
        [Description("Maximum number of results to return (1-50)")] int topK = 10,
        CancellationToken cancellationToken = default)
    {
        var results = await apiClient.SearchBabblesAsync(query, topK, cancellationToken);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "list_babbles", ReadOnly = true)]
    [Description("List babbles with pagination support. Returns a page of babbles and an optional continuation token for the next page.")]
    public async Task<string> ListBabbles(
        [Description("Continuation token from a previous list call for pagination (optional)")] string? continuationToken = null,
        [Description("Number of babbles to return per page (1-100)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var results = await apiClient.ListBabblesAsync(continuationToken, pageSize, cancellationToken);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "get_babble", ReadOnly = true)]
    [Description("Get a single babble by its ID. Returns null if not found.")]
    public async Task<string?> GetBabble(
        [Description("The unique identifier of the babble")] string id,
        CancellationToken cancellationToken = default)
    {
        var babble = await apiClient.GetBabbleAsync(id, cancellationToken);
        return babble is null ? null : JsonSerializer.Serialize(babble);
    }

    [McpServerTool(Name = "generate_prompt")]
    [Description("Generate an AI prompt from a babble using a prompt template. Streams the result and returns the complete generated text.")]
    public async Task<string> GeneratePrompt(
        [Description("The ID of the babble to generate a prompt from")] string babbleId,
        [Description("The ID of the prompt template to use")] string templateId,
        [Description("Output format: 'text' or 'markdown' (optional)")] string? promptFormat = null,
        [Description("Whether to allow emojis in the output (optional)")] bool? allowEmojis = null,
        CancellationToken cancellationToken = default)
    {
        return await apiClient.GeneratePromptAsync(babbleId, templateId, promptFormat, allowEmojis, cancellationToken);
    }
}
