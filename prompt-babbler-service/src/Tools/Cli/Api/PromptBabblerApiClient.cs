using System.Net.Http.Json;
using System.Text.Json;
using PromptBabbler.Tools.Cli.Models;

namespace PromptBabbler.Tools.Cli.Api;

public sealed class PromptBabblerApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public PromptBabblerApiClient(string apiUrl, string? accessCode)
        : this(apiUrl, accessCode, null)
    {
    }

    public PromptBabblerApiClient(string apiUrl, string? accessCode, HttpMessageHandler? handler)
    {
        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
        _httpClient.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);

        if (!string.IsNullOrWhiteSpace(accessCode))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Access-Code", accessCode);
        }
    }

    public async Task<HttpResponseMessage> UpsertBabbleAsync(BabbleImportItem item, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("api/babbles", item, JsonOptions, cancellationToken);
        return response;
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

    public void Dispose()
    {
        _httpClient.Dispose();
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
