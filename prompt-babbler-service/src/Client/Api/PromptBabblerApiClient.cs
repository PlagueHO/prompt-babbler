using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using PromptBabbler.ApiClient.Models;

namespace PromptBabbler.ApiClient;

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
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

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
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

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
        string babbleId,
        string? continuationToken,
        int pageSize,
        CancellationToken cancellationToken)
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
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GeneratedPromptDto>(JsonOptions, cancellationToken);
    }

    public async Task<string> GeneratePromptAsync(
        string babbleId,
        string templateId,
        string? promptFormat,
        bool? allowEmojis,
        CancellationToken cancellationToken)
    {
        var url = $"api/babbles/{Uri.EscapeDataString(babbleId)}/generate";
        var body = new { templateId, promptFormat, allowEmojis };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
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

    public async Task<HttpResponseMessage> UpsertBabbleAsync(BabbleImportItem item, CancellationToken cancellationToken)
    {
        return await _httpClient.PostAsJsonAsync("api/babbles", item, JsonOptions, cancellationToken);
    }

    public async Task<string> StartImportAsync(string zipFilePath, bool overwrite, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(zipFilePath);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", Path.GetFileName(zipFilePath));

        var response = await _httpClient.PostAsync($"api/imports?overwrite={overwrite.ToString().ToLowerInvariant()}", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJobIdAsync(response, cancellationToken);
    }

    public async Task<string> StartExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("api/exports", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJobIdAsync(response, cancellationToken);
    }

    public async Task<ImportExportJobResponse> GetImportJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"api/imports/{jobId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<ImportExportJobResponse>(response, cancellationToken);
    }

    public async Task<ImportExportJobResponse> GetExportJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"api/exports/{jobId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<ImportExportJobResponse>(response, cancellationToken);
    }

    public async Task DownloadExportAsync(string jobId, string outputPath, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"api/exports/{jobId}/download", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        await using var output = File.Create(outputPath);
        await response.Content.CopyToAsync(output, cancellationToken);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var model = JsonSerializer.Deserialize<T>(payload, JsonOptions);
        if (model is null)
        {
            throw new InvalidOperationException("Response payload was empty or invalid JSON.");
        }

        return model;
    }

    private static async Task<string> ReadJobIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(payload);
        if (!json.RootElement.TryGetProperty("jobId", out var jobIdElement))
        {
            throw new InvalidOperationException("Response did not include jobId.");
        }

        var jobId = jobIdElement.GetString();
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new InvalidOperationException("Response jobId was empty.");
        }

        return jobId;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"API request failed with {(int)response.StatusCode} {response.StatusCode}. {payload}");
    }
}
