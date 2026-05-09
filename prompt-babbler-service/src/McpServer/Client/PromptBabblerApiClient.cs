using System.Net;
using System.Text;
using System.Text.Json;
using PromptBabbler.McpServer.Client.Models;

namespace PromptBabbler.McpServer.Client;

public sealed class PromptBabblerApiClient : IPromptBabblerApiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PromptBabblerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BabbleSearchResponseDto> SearchBabblesAsync(string query, int topK, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"api/babbles/search?query={Uri.EscapeDataString(query)}&topK={topK}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BabbleSearchResponseDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<PagedResponseDto<BabbleDto>> ListBabblesAsync(string? continuationToken, int pageSize, CancellationToken cancellationToken)
    {
        var url = $"api/babbles?pageSize={pageSize}";
        if (!string.IsNullOrEmpty(continuationToken))
        {
            url += $"&continuationToken={Uri.EscapeDataString(continuationToken)}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResponseDto<BabbleDto>>(JsonOptions, cancellationToken))!;
    }

    public async Task<BabbleDto?> GetBabbleAsync(string id, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"api/babbles/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BabbleDto>(JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<PromptTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        var templates = new List<PromptTemplateDto>();
        string? continuationToken = null;

        do
        {
            var url = "api/templates?pageSize=100";
            if (!string.IsNullOrEmpty(continuationToken))
            {
                url += $"&continuationToken={Uri.EscapeDataString(continuationToken)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<PromptTemplateDto>>(JsonOptions, cancellationToken);
            if (page is null)
            {
                throw new InvalidOperationException("Template list response body was empty.");
            }

            templates.AddRange(page.Items);
            continuationToken = page.ContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        return templates;
    }

    public async Task<PromptTemplateDto?> GetTemplateAsync(string id, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"api/templates/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PromptTemplateDto>(JsonOptions, cancellationToken);
    }

    public async Task<PromptTemplateDto> CreateTemplateAsync(CreatePromptTemplateRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("api/templates", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PromptTemplateDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<PromptTemplateDto> UpdateTemplateAsync(string id, UpdatePromptTemplateRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/templates/{Uri.EscapeDataString(id)}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PromptTemplateDto>(JsonOptions, cancellationToken))!;
    }

    public async Task DeleteTemplateAsync(string id, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"api/templates/{Uri.EscapeDataString(id)}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PagedResponseDto<GeneratedPromptDto>> ListGeneratedPromptsAsync(
        string babbleId, string? continuationToken, int pageSize, CancellationToken cancellationToken)
    {
        var url = $"api/babbles/{Uri.EscapeDataString(babbleId)}/prompts?pageSize={pageSize}";
        if (!string.IsNullOrEmpty(continuationToken))
        {
            url += $"&continuationToken={Uri.EscapeDataString(continuationToken)}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResponseDto<GeneratedPromptDto>>(JsonOptions, cancellationToken))!;
    }

    public async Task<GeneratedPromptDto?> GetGeneratedPromptAsync(string babbleId, string id, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"api/babbles/{Uri.EscapeDataString(babbleId)}/prompts/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GeneratedPromptDto>(JsonOptions, cancellationToken);
    }

    public async Task<string> GeneratePromptAsync(
        string babbleId, string templateId, string? promptFormat, bool? allowEmojis, CancellationToken cancellationToken)
    {
        var url = $"api/babbles/{Uri.EscapeDataString(babbleId)}/generate";
        var body = new { templateId, promptFormat, allowEmojis };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = new StringBuilder();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                result.Append(line[5..].TrimStart());
            }
        }

        return result.ToString();
    }
}
