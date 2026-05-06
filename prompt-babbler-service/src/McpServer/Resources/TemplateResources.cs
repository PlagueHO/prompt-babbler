using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PromptBabbler.McpServer.Client;

namespace PromptBabbler.McpServer.Resources;

[McpServerResourceType]
public sealed class TemplateResources(IPromptBabblerApiClient apiClient)
{
    [McpServerResource(
        UriTemplate = "babbler://templates",
        Name = "Prompt Templates",
        MimeType = "application/json")]
    [Description("All available prompt templates, including built-in and user-created templates.")]
    public async Task<string> GetTemplates(CancellationToken cancellationToken = default)
    {
        var templates = await apiClient.GetTemplatesAsync(cancellationToken);
        return JsonSerializer.Serialize(templates);
    }

    [McpServerResource(
        UriTemplate = "babbler://templates/{id}",
        Name = "Prompt Template",
        MimeType = "application/json")]
    [Description("A single prompt template by ID.")]
    public async Task<string?> GetTemplate(
        [Description("The unique identifier of the template")] string id,
        CancellationToken cancellationToken = default)
    {
        var template = await apiClient.GetTemplateAsync(id, cancellationToken);
        return template is null ? null : JsonSerializer.Serialize(template);
    }
}
