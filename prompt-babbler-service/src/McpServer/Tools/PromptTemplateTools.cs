using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PromptBabbler.ApiClient;
using PromptBabbler.ApiClient.Models;

namespace PromptBabbler.McpServer.Tools;

[McpServerToolType]
public sealed class PromptTemplateTools(IPromptBabblerApiClient apiClient)
{
    [McpServerTool(Name = "list_prompt_templates", ReadOnly = true)]
    [Description("List all available prompt templates, including built-in and user-created templates.")]
    public async Task<string> ListTemplates(CancellationToken cancellationToken = default)
    {
        var templates = await apiClient.GetTemplatesAsync(cancellationToken);
        return JsonSerializer.Serialize(templates);
    }

    [McpServerTool(Name = "get_prompt_template", ReadOnly = true)]
    [Description("Get a single prompt template by its ID. Returns null if not found.")]
    public async Task<string?> GetTemplate(
        [Description("The unique identifier of the template")] string id,
        CancellationToken cancellationToken = default)
    {
        var template = await apiClient.GetTemplateAsync(id, cancellationToken);
        return template is null ? null : JsonSerializer.Serialize(template);
    }

    [McpServerTool(Name = "create_prompt_template")]
    [Description("Create a new user-defined prompt template.")]
    public async Task<string> CreateTemplate(
        [Description("The display name of the template")] string name,
        [Description("A description of what this template does")] string description,
        [Description("The system instructions for the AI when using this template")] string instructions,
        [Description("Description of the expected output format (optional)")] string? outputDescription = null,
        [Description("Default output format: 'text' or 'markdown' (optional)")] string? defaultOutputFormat = null,
        [Description("Whether to allow emojis by default (optional)")] bool? defaultAllowEmojis = null,
        [Description("Tags for categorisation (optional)")] IReadOnlyList<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CreatePromptTemplateRequest
        {
            Name = name,
            Description = description,
            Instructions = instructions,
            OutputDescription = outputDescription,
            DefaultOutputFormat = defaultOutputFormat,
            DefaultAllowEmojis = defaultAllowEmojis,
            Tags = tags
        };
        var template = await apiClient.CreateTemplateAsync(request, cancellationToken);
        return JsonSerializer.Serialize(template);
    }

    [McpServerTool(Name = "update_prompt_template")]
    [Description("Update an existing user-defined prompt template. Built-in templates cannot be updated.")]
    public async Task<string> UpdateTemplate(
        [Description("The unique identifier of the template to update")] string id,
        [Description("The updated display name")] string name,
        [Description("The updated description")] string description,
        [Description("The updated system instructions")] string instructions,
        [Description("Updated description of the expected output format (optional)")] string? outputDescription = null,
        [Description("Updated default output format: 'text' or 'markdown' (optional)")] string? defaultOutputFormat = null,
        [Description("Updated default emoji setting (optional)")] bool? defaultAllowEmojis = null,
        [Description("Updated tags (optional)")] IReadOnlyList<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdatePromptTemplateRequest
        {
            Name = name,
            Description = description,
            Instructions = instructions,
            OutputDescription = outputDescription,
            DefaultOutputFormat = defaultOutputFormat,
            DefaultAllowEmojis = defaultAllowEmojis,
            Tags = tags
        };
        var template = await apiClient.UpdateTemplateAsync(id, request, cancellationToken);
        return JsonSerializer.Serialize(template);
    }

    [McpServerTool(Name = "delete_prompt_template", Destructive = true)]
    [Description("Delete a user-defined prompt template. Built-in templates cannot be deleted. This action is irreversible.")]
    public async Task DeleteTemplate(
        [Description("The unique identifier of the template to delete")] string id,
        CancellationToken cancellationToken = default)
    {
        await apiClient.DeleteTemplateAsync(id, cancellationToken);
    }
}
