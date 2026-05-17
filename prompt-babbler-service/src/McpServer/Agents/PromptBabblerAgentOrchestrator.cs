using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using PromptBabbler.ApiClient;

namespace PromptBabbler.McpServer.Agents;

public sealed class PromptBabblerAgentOrchestrator(
    IPromptBabblerAgentRunner agentRunner,
    IPromptBabblerApiClient apiClient,
    IConfiguration configuration) : IPromptBabblerAgentOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string DefaultModelDeploymentName = "chat";

    private const string AgentInstructions =
        "You are Prompt Babbler's orchestration agent. " +
        "Prompt Babbler is a system that helps users generate prompts for AI systems based on their babbles (user-generated transcripts) and prompt templates. " +
        "Use tools to find babbles/templates and generate prompts when needed. " +
        "Prefer precise tool calls over assumptions, and return concise final answers.";

    public async Task<string> RunAsync(string request, CancellationToken cancellationToken)
    {
        var modelDeploymentName = configuration["Agentic:ModelDeploymentName"];
        var tools = BuildTools();
        var response = await agentRunner.RunAsync(
            request,
            string.IsNullOrWhiteSpace(modelDeploymentName) ? DefaultModelDeploymentName : modelDeploymentName,
            AgentInstructions,
            tools,
            cancellationToken);

        var trace = response.Contents
            .Select(ProjectTraceStep)
            .Where(step => step is not null)
            .Select(step => step!)
            .ToArray();

        var result = new AgentExecutionResult(
            Answer: response.Answer,
            Trace: trace);

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private IReadOnlyList<AITool> BuildTools()
    {
        return
        [
            AIFunctionFactory.Create(SearchBabblesAsync, name: "search_babbles_api"),
            AIFunctionFactory.Create(GetBabbleAsync, name: "get_babble_api"),
            AIFunctionFactory.Create(ListPromptTemplatesAsync, name: "list_prompt_templates_api"),
            AIFunctionFactory.Create(GetPromptTemplateAsync, name: "get_prompt_template_api"),
            AIFunctionFactory.Create(GeneratePromptAsync, name: "generate_prompt_api"),
            AIFunctionFactory.Create(ListGeneratedPromptsAsync, name: "list_generated_prompts_api")
        ];
    }

    private static AgentExecutionStep? ProjectTraceStep(AIContent content)
    {
        return content switch
        {
            FunctionCallContent call => new AgentExecutionStep(
                Kind: "act",
                Name: call.Name ?? string.Empty,
                Content: JsonSerializer.Serialize(call.Arguments, JsonOptions)),
            FunctionResultContent result => new AgentExecutionStep(
                Kind: "observe",
                Name: result.CallId ?? string.Empty,
                Content: result.Result?.ToString() ?? string.Empty),
            TextContent text when !string.IsNullOrWhiteSpace(text.Text) => new AgentExecutionStep(
                Kind: "reason",
                Name: "text",
                Content: text.Text),
            _ => null,
        };
    }

    [Description("Search babbles by semantic relevance.")]
    private async Task<string> SearchBabblesAsync(
        [Description("Natural-language search query")] string query,
        [Description("Maximum number of matches to return (1-50)")] int topK = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await apiClient.SearchBabblesAsync(query, topK, cancellationToken);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [Description("Get one babble by ID.")]
    private async Task<string?> GetBabbleAsync(
        [Description("Babble ID")] string id,
        CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetBabbleAsync(id, cancellationToken);
        return result is null ? null : JsonSerializer.Serialize(result, JsonOptions);
    }

    [Description("List all prompt templates.")]
    private async Task<string> ListPromptTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetTemplatesAsync(cancellationToken);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [Description("Get one prompt template by ID.")]
    private async Task<string?> GetPromptTemplateAsync(
        [Description("Template ID")] string id,
        CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetTemplateAsync(id, cancellationToken);
        return result is null ? null : JsonSerializer.Serialize(result, JsonOptions);
    }

    [Description("Generate a prompt from a babble and template.")]
    private Task<string> GeneratePromptAsync(
        [Description("Babble ID")] string babbleId,
        [Description("Template ID")] string templateId,
        [Description("Output format ('text' or 'markdown')")] string? promptFormat = null,
        [Description("Whether to allow emojis in the output")] bool? allowEmojis = null,
        CancellationToken cancellationToken = default)
    {
        return apiClient.GeneratePromptAsync(babbleId, templateId, promptFormat, allowEmojis, cancellationToken);
    }

    [Description("List generated prompts for a babble.")]
    private async Task<string> ListGeneratedPromptsAsync(
        [Description("Babble ID")] string babbleId,
        [Description("Continuation token for pagination")] string? continuationToken = null,
        [Description("Page size (1-100)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await apiClient.ListGeneratedPromptsAsync(babbleId, continuationToken, pageSize, cancellationToken);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private sealed record AgentExecutionResult(string Answer, IReadOnlyList<AgentExecutionStep> Trace);

    private sealed record AgentExecutionStep(string Kind, string Name, string Content);
}
