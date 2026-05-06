<!-- markdownlint-disable-file -->
# Implementation Details: MCP Server for Prompt Babbler

## Context Reference

Sources: .copilot-tracking/research/2026-05-05/mcp-server-research.md, AppHost.cs (lines 66-86), Directory.Packages.props (line 33 insert point), PromptBabbler.slnx (line 5 insert point), AppHost.csproj (line 15 insert point)

---

## Implementation Phase 1: Package and Solution Infrastructure

<!-- parallelizable: false -->

### Step 1.1: Add `ModelContextProtocol.AspNetCore` to `Directory.Packages.props`

Add the package version after the existing `Newtonsoft.Json` entry (approximately line 33) in the Azure/AI group. Version 1.2.0 is the confirmed stable release for .NET 10.

Files:
* prompt-babbler-service/Directory.Packages.props — Add PackageVersion entry

Insert after the `Newtonsoft.Json` line:
```xml
<PackageVersion Include="ModelContextProtocol.AspNetCore" Version="1.2.0" />
```

Also verify that `Microsoft.AspNetCore.Authentication.JwtBearer` and `Microsoft.Identity.Web` are already present. If not, add them.

Success criteria:
* `Directory.Packages.props` contains `ModelContextProtocol.AspNetCore` with version 1.2.0
* No duplicate entries

Dependencies:
* None — first step

---

### Step 1.2: Create the McpServer project directory and `.csproj` file

Create the directory `prompt-babbler-service/src/McpServer/` and the project file. The `Directory.Build.props` at the solution root automatically applies `net10.0`, `ImplicitUsings`, `Nullable`, `TreatWarningsAsErrors`, and `EnforceCodeStyleInBuild` — do NOT repeat these in the `.csproj`.

Files:
* prompt-babbler-service/src/McpServer/PromptBabbler.McpServer.csproj — New project file

Full content for `PromptBabbler.McpServer.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <RootNamespace>PromptBabbler.McpServer</RootNamespace>
    <AssemblyName>PromptBabbler.McpServer</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Microsoft.Identity.Web" />
    <PackageReference Include="ModelContextProtocol.AspNetCore" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Orchestration\ServiceDefaults\PromptBabbler.ServiceDefaults.csproj" />
  </ItemGroup>
</Project>
```

Success criteria:
* File exists at the correct path
* SDK is `Microsoft.NET.Sdk.Web`
* Three package references present (no versions — central package management)
* One project reference to ServiceDefaults
* No `TargetFramework`, `Nullable`, or `TreatWarningsAsErrors` (inherited from Directory.Build.props)

Dependencies:
* Step 1.1 (package version must be in Directory.Packages.props)

---

### Step 1.3: Add McpServer project to `PromptBabbler.slnx`

The solution file has a `/src/` folder entry (lines 2–8). Insert the new project after the existing `Infrastructure` project line (line 5) in alphabetical order (M comes between I and O).

Files:
* prompt-babbler-service/PromptBabbler.slnx — Add Project entry in /src/ folder

Insert after the Infrastructure project line:
```xml
    <Project Path="src/McpServer/PromptBabbler.McpServer.csproj" />
```

Success criteria:
* `PromptBabbler.slnx` contains the McpServer project path
* Entry is inside the `/src/` folder block
* Alphabetical order maintained

Dependencies:
* Step 1.2 (project file must exist)

---

## Implementation Phase 2: HTTP Client Layer

<!-- parallelizable: false -->

### Step 2.1: Create `Client/Models/` DTOs

Create lightweight `sealed record` types that mirror the API JSON contracts. These are one-way deserialization DTOs (no constructors beyond `init`). All properties use `[JsonPropertyName]` attributes matching the API's JSON output. These types are NOT the same as the domain models in `PromptBabbler.Domain` — they are lightweight HTTP response shapes.

Files:
* prompt-babbler-service/src/McpServer/Client/Models/BabbleDto.cs — Babble response types
* prompt-babbler-service/src/McpServer/Client/Models/PromptTemplateDto.cs — Template response + request types
* prompt-babbler-service/src/McpServer/Client/Models/GeneratedPromptDto.cs — Generated prompt response type
* prompt-babbler-service/src/McpServer/Client/Models/PagedResponseDto.cs — Generic paged response
* prompt-babbler-service/src/McpServer/Client/Models/BabbleSearchResponseDto.cs — Search response type

`BabbleDto.cs`:
```csharp
using System.Text.Json.Serialization;

namespace PromptBabbler.McpServer.Client.Models;

public sealed record BabbleDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("text")] public required string Text { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("createdAt")] public required string CreatedAt { get; init; }
    [JsonPropertyName("updatedAt")] public required string UpdatedAt { get; init; }
    [JsonPropertyName("isPinned")] public bool IsPinned { get; init; }
}
```

`BabbleSearchResponseDto.cs`:
```csharp
using System.Text.Json.Serialization;

namespace PromptBabbler.McpServer.Client.Models;

public sealed record BabbleSearchResponseDto
{
    [JsonPropertyName("results")] public required IReadOnlyList<BabbleSearchResultItemDto> Results { get; init; }
}

// NOTE: The API serialises BabbleSearchResultItem as a FLAT object (no nested Babble property).
// Verify against the actual API response at implementation time. If the API returns a nested
// { babble: {...}, snippet, score } shape instead, add a nested BabbleDto property and remove
// the flat babble fields (Id, Title, Text, Tags, CreatedAt, IsPinned).
public sealed record BabbleSearchResultItemDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("text")] public string? Text { get; init; }
    [JsonPropertyName("snippet")] public string? Snippet { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("createdAt")] public required string CreatedAt { get; init; }
    [JsonPropertyName("isPinned")] public bool IsPinned { get; init; }
    [JsonPropertyName("score")] public double Score { get; init; }
}
```

`PromptTemplateDto.cs`:
```csharp
using System.Text.Json.Serialization;

namespace PromptBabbler.McpServer.Client.Models;

public sealed record PromptTemplateDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("description")] public required string Description { get; init; }
    [JsonPropertyName("instructions")] public required string Instructions { get; init; }
    [JsonPropertyName("outputDescription")] public string? OutputDescription { get; init; }
    [JsonPropertyName("outputTemplate")] public string? OutputTemplate { get; init; }
    [JsonPropertyName("defaultOutputFormat")] public string? DefaultOutputFormat { get; init; }
    [JsonPropertyName("defaultAllowEmojis")] public bool? DefaultAllowEmojis { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("examples")] public IReadOnlyList<string>? Examples { get; init; }
    [JsonPropertyName("guardrails")] public string? Guardrails { get; init; }
    [JsonPropertyName("additionalProperties")] public IReadOnlyDictionary<string, string>? AdditionalProperties { get; init; }
    [JsonPropertyName("isBuiltIn")] public bool IsBuiltIn { get; init; }
    [JsonPropertyName("createdAt")] public required string CreatedAt { get; init; }
    [JsonPropertyName("updatedAt")] public required string UpdatedAt { get; init; }
}

public sealed record CreatePromptTemplateRequest
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("description")] public required string Description { get; init; }
    [JsonPropertyName("instructions")] public required string Instructions { get; init; }
    [JsonPropertyName("outputDescription")] public string? OutputDescription { get; init; }
    [JsonPropertyName("defaultOutputFormat")] public string? DefaultOutputFormat { get; init; }
    [JsonPropertyName("defaultAllowEmojis")] public bool? DefaultAllowEmojis { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
}

public sealed record UpdatePromptTemplateRequest
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("description")] public required string Description { get; init; }
    [JsonPropertyName("instructions")] public required string Instructions { get; init; }
    [JsonPropertyName("outputDescription")] public string? OutputDescription { get; init; }
    [JsonPropertyName("defaultOutputFormat")] public string? DefaultOutputFormat { get; init; }
    [JsonPropertyName("defaultAllowEmojis")] public bool? DefaultAllowEmojis { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
}
```

`GeneratedPromptDto.cs`:
```csharp
using System.Text.Json.Serialization;

namespace PromptBabbler.McpServer.Client.Models;

public sealed record GeneratedPromptDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("babbleId")] public required string BabbleId { get; init; }
    [JsonPropertyName("templateId")] public required string TemplateId { get; init; }
    [JsonPropertyName("templateName")] public required string TemplateName { get; init; }
    [JsonPropertyName("promptText")] public required string PromptText { get; init; }
    [JsonPropertyName("generatedAt")] public required string GeneratedAt { get; init; }
}
```

`PagedResponseDto.cs`:
```csharp
using System.Text.Json.Serialization;

namespace PromptBabbler.McpServer.Client.Models;

public sealed record PagedResponseDto<T>
{
    [JsonPropertyName("items")] public required IReadOnlyList<T> Items { get; init; }
    [JsonPropertyName("continuationToken")] public string? ContinuationToken { get; init; }
}
```

Success criteria:
* All five DTO files exist
* All types are `sealed record`
* All properties use `[JsonPropertyName]` with camelCase names matching the API
* No logic or methods — pure data shapes

Dependencies:
* None (pure data types, no external dependencies)

---

### Step 2.2: Create `Client/IPromptBabblerApiClient.cs`

Define the typed client interface. All methods are async and accept `CancellationToken`.

Files:
* prompt-babbler-service/src/McpServer/Client/IPromptBabblerApiClient.cs — Interface

```csharp
using PromptBabbler.McpServer.Client.Models;

namespace PromptBabbler.McpServer.Client;

public interface IPromptBabblerApiClient
{
    // Babbles
    Task<BabbleSearchResponseDto> SearchBabblesAsync(string query, int topK, CancellationToken cancellationToken);
    Task<PagedResponseDto<BabbleDto>> ListBabblesAsync(string? continuationToken, int pageSize, CancellationToken cancellationToken);
    Task<BabbleDto?> GetBabbleAsync(string id, CancellationToken cancellationToken);

    // Prompt Templates
    Task<IReadOnlyList<PromptTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken);
    Task<PromptTemplateDto?> GetTemplateAsync(string id, CancellationToken cancellationToken);
    Task<PromptTemplateDto> CreateTemplateAsync(CreatePromptTemplateRequest request, CancellationToken cancellationToken);
    Task<PromptTemplateDto> UpdateTemplateAsync(string id, UpdatePromptTemplateRequest request, CancellationToken cancellationToken);
    Task DeleteTemplateAsync(string id, CancellationToken cancellationToken);

    // Generated Prompts
    Task<PagedResponseDto<GeneratedPromptDto>> ListGeneratedPromptsAsync(string babbleId, string? continuationToken, int pageSize, CancellationToken cancellationToken);
    Task<GeneratedPromptDto?> GetGeneratedPromptAsync(string babbleId, string id, CancellationToken cancellationToken);

    // Generation
    Task<string> GeneratePromptAsync(string babbleId, string templateId, string? promptFormat, bool? allowEmojis, CancellationToken cancellationToken);
}
```

Success criteria:
* Interface is NOT sealed (it's an interface)
* All operations covered with correct method signatures
* `CancellationToken` on every async method
* Nullable return types for single-item lookups (`BabbleDto?`, `PromptTemplateDto?`, `GeneratedPromptDto?`)

Dependencies:
* Step 2.1 (DTOs must exist)

---

### Step 2.3: Create `Client/ApiAuthDelegatingHandler.cs`

The delegating handler injects the correct auth header into outbound API calls. Behaviour depends on configured auth mode:
- Anonymous mode: no header added
- Access code mode: adds `X-Access-Code: {code}` header
- Entra ID mode: acquires OBO token via `ITokenAcquisition`, adds `Authorization: Bearer {token}`

The `ApiAuthOptions` record is a simple configuration bag registered as a singleton.

Files:
* prompt-babbler-service/src/McpServer/Client/ApiAuthOptions.cs — Options record
* prompt-babbler-service/src/McpServer/Client/ApiAuthDelegatingHandler.cs — DelegatingHandler

`ApiAuthOptions.cs`:
```csharp
namespace PromptBabbler.McpServer.Client;

// AzureAdClientId holds the MCP server's Entra ID client ID (empty string = anonymous/access-code mode)
public sealed record ApiAuthOptions(string AccessCode, string AzureAdClientId, string ApiScope);
```

`ApiAuthDelegatingHandler.cs`:
```csharp
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace PromptBabbler.McpServer.Client;

public sealed class ApiAuthDelegatingHandler : DelegatingHandler
{
    private readonly ApiAuthOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public ApiAuthDelegatingHandler(ApiAuthOptions options, IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_options.AzureAdClientId))
        {
            // Entra ID mode: acquire OBO token for the downstream API
            var tokenAcquisition = _serviceProvider.GetService<ITokenAcquisition>();
            if (tokenAcquisition is not null)
            {
                var token = await tokenAcquisition.GetAccessTokenForUserAsync(
                    [_options.ApiScope],
                    cancellationToken: cancellationToken);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        else if (!string.IsNullOrEmpty(_options.AccessCode))
        {
            request.Headers.Add("X-Access-Code", _options.AccessCode);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

Discrepancy references:
* DD-02 — Validation of incoming access code is handled by McpAccessCodeMiddleware (Phase 5), not here

Success criteria:
* `ApiAuthDelegatingHandler` is `sealed`
* Uses `IServiceProvider.GetService<ITokenAcquisition>()` rather than direct optional injection (avoids DI startup failures in non-Entra-ID modes)
* `AzureAdClientId` field name matches `ApiAuthOptions` record parameter name
* Anonymous mode: no headers added (falls through all conditions)

Dependencies:
* Step 2.2 (interface defined; not a hard dependency but logically grouped)

---

### Step 2.4: Create `Client/PromptBabblerApiClient.cs`

The typed HTTP client implementation. Uses `System.Text.Json` for deserialization. Handles SSE for the generate endpoint by reading lines until the stream ends and concatenating `data:` payloads. Handles 404 responses by returning null for single-item lookups.

Files:
* prompt-babbler-service/src/McpServer/Client/PromptBabblerApiClient.cs — Implementation

```csharp
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
        var response = await _httpClient.GetAsync("api/templates", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<PromptTemplateDto>>(JsonOptions, cancellationToken))!;
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
        var url = $"api/babbles/{Uri.EscapeDataString(babbleId)}/generate?templateId={Uri.EscapeDataString(templateId)}";
        if (promptFormat is not null) url += $"&promptFormat={Uri.EscapeDataString(promptFormat)}";
        if (allowEmojis.HasValue) url += $"&allowEmojis={allowEmojis.Value.ToString().ToLowerInvariant()}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = new StringBuilder();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                result.Append(line[5..].TrimStart());
            }
        }

        return result.ToString();
    }
}
```

Discrepancy references:
* DD-01 — GeneratePromptAsync collects SSE stream and returns text only; no separate save call

Success criteria:
* All `IPromptBabblerApiClient` methods implemented
* 404 responses return null for single-item lookups
* SSE stream collected and returned as single string for generate endpoint
* No hardcoded URLs beyond relative paths
* `sealed` class with `_httpClient` field using `_camelCase`

Dependencies:
* Step 2.1 (DTOs), Step 2.2 (interface), Step 2.3 (handler registered but not a compile dependency)

---

## Implementation Phase 3: MCP Tools

<!-- parallelizable: true -->

### Step 3.1: Create `Tools/BabbleTools.cs`

Four tools covering babble operations. `search_babbles`, `list_babbles`, and `get_babble` are read-only. `generate_prompt` calls the API generate endpoint and returns the full generated text.

Files:
* prompt-babbler-service/src/McpServer/Tools/BabbleTools.cs — Babble and generate tools

```csharp
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
```

Success criteria:
* Class is `sealed`, uses constructor injection (primary constructor syntax)
* All four tools have `[Description]` attributes on the method and all parameters
* `search_babbles`, `list_babbles`, `get_babble` have `ReadOnly = true`
* `generate_prompt` has no ReadOnly flag (it calls a POST endpoint)
* Returns JSON-serialized results for list/search; raw string for generate

Dependencies:
* Phase 2 complete (IPromptBabblerApiClient interface must exist)

---

### Step 3.2: Create `Tools/PromptTemplateTools.cs`

Five tools covering template CRUD. `list_templates` and `get_template` are read-only. `delete_template` is destructive.

Files:
* prompt-babbler-service/src/McpServer/Tools/PromptTemplateTools.cs — Template CRUD tools

```csharp
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PromptBabbler.McpServer.Client;
using PromptBabbler.McpServer.Client.Models;

namespace PromptBabbler.McpServer.Tools;

[McpServerToolType]
public sealed class PromptTemplateTools(IPromptBabblerApiClient apiClient)
{
    [McpServerTool(Name = "list_templates", ReadOnly = true)]
    [Description("List all available prompt templates, including built-in and user-created templates.")]
    public async Task<string> ListTemplates(CancellationToken cancellationToken = default)
    {
        var templates = await apiClient.GetTemplatesAsync(cancellationToken);
        return JsonSerializer.Serialize(templates);
    }

    [McpServerTool(Name = "get_template", ReadOnly = true)]
    [Description("Get a single prompt template by its ID. Returns null if not found.")]
    public async Task<string?> GetTemplate(
        [Description("The unique identifier of the template")] string id,
        CancellationToken cancellationToken = default)
    {
        var template = await apiClient.GetTemplateAsync(id, cancellationToken);
        return template is null ? null : JsonSerializer.Serialize(template);
    }

    [McpServerTool(Name = "create_template")]
    [Description("Create a new user-defined prompt template.")]
    public async Task<string> CreateTemplate(
        [Description("The display name of the template")] string name,
        [Description("A description of what this template does")] string description,
        [Description("The system instructions for the AI when using this template")] string instructions,
        [Description("Description of the expected output format (optional)")] string? outputDescription = null,
        [Description("Default output format: 'text' or 'markdown' (optional)")] string? defaultOutputFormat = null,
        [Description("Whether to allow emojis by default (optional)")] bool? defaultAllowEmojis = null,
        [Description("Comma-separated tags for categorisation (optional)")] IReadOnlyList<string>? tags = null,
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

    [McpServerTool(Name = "update_template")]
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

    [McpServerTool(Name = "delete_template", Destructive = true)]
    [Description("Delete a user-defined prompt template. Built-in templates cannot be deleted. This action is irreversible.")]
    public async Task DeleteTemplate(
        [Description("The unique identifier of the template to delete")] string id,
        CancellationToken cancellationToken = default)
    {
        await apiClient.DeleteTemplateAsync(id, cancellationToken);
    }
}
```

Success criteria:
* `delete_template` has `Destructive = true`
* `list_templates` and `get_template` have `ReadOnly = true`
* All parameters have `[Description]` attributes
* Class is `sealed`, uses primary constructor

Dependencies:
* Phase 2 complete (IPromptBabblerApiClient + DTOs)

---

### Step 3.3: Create `Tools/GeneratedPromptTools.cs`

Two read-only tools for listing and retrieving generated prompts.

Files:
* prompt-babbler-service/src/McpServer/Tools/GeneratedPromptTools.cs — Generated prompt tools

```csharp
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PromptBabbler.McpServer.Client;

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
```

Success criteria:
* Both tools have `ReadOnly = true`
* `babbleId` parameter present on both tools
* Class is `sealed`

Dependencies:
* Phase 2 complete

---

## Implementation Phase 4: MCP Resources and Prompts

<!-- parallelizable: true -->

### Step 4.1: Create `Resources/TemplateResources.cs`

Two MCP Resources: a list resource for all templates and a URI-template resource for individual templates. These allow MCP clients to browse the template catalog without invoking a tool.

Files:
* prompt-babbler-service/src/McpServer/Resources/TemplateResources.cs — Template resources

```csharp
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PromptBabbler.McpServer.Client;

namespace PromptBabbler.McpServer.Resources;

[McpServerResourceType]
public sealed class TemplateResources(IPromptBabblerApiClient apiClient)
{
    [McpServerResource(
        Uri = "babbler://templates",
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
```

Success criteria:
* Static URI resource uses `Uri` property; template URI resource uses `UriTemplate`
* `MimeType = "application/json"` on both resources
* Class is `sealed`

Dependencies:
* Phase 2 complete (IPromptBabblerApiClient)

---

### Step 4.2: Create `Prompts/TemplateReviewPrompt.cs`

A single user-triggered MCP Prompt (slash command). This is a static `ChatMessage` array returned to seed a conversation — it does NOT call the API. It is `static` because it has no dependencies.

Files:
* prompt-babbler-service/src/McpServer/Prompts/TemplateReviewPrompt.cs — User-triggered slash command

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace PromptBabbler.McpServer.Prompts;

[McpServerPromptType]
public static class TemplateReviewPrompt
{
    [McpServerPrompt(Name = "review_template")]
    [Description("Seed a conversation to review and improve a prompt template. Provides structured guidance for evaluating clarity, completeness, and effectiveness.")]
    public static IEnumerable<ChatMessage> ReviewTemplate(
        [Description("The template instructions text to review")] string instructions,
        [Description("The template description to provide context")] string description)
    {
        yield return new ChatMessage(ChatRole.User,
            $"""
            Please review this prompt template and suggest improvements.

            Description: {description}

            Instructions:
            {instructions}

            Evaluate the template for:
            1. Clarity — are the instructions unambiguous?
            2. Completeness — are there missing edge cases or scenarios?
            3. Effectiveness — will the instructions produce high-quality prompts?
            4. Conciseness — can any instructions be simplified without losing meaning?

            Provide specific, actionable suggestions for each area.
            """);
    }
}
```

Note: The class is `static` (an exception to the `sealed` rule since `static` classes cannot be `sealed` — the compiler enforces this). The attribute `[McpServerPromptType]` on a `static` class is valid with the SDK.

Success criteria:
* Class is `static` (not `sealed` — static classes are implicitly sealed; the two are mutually exclusive)
* Method returns `IEnumerable<ChatMessage>` (not async)
* No API calls; pure static content
* `ChatRole.User` used for the seed message
* Instructions use a raw string literal for readability

Dependencies:
* None for this step (no API client needed)
* `Microsoft.Extensions.AI` namespace for `ChatMessage` and `ChatRole` — comes from the MCP SDK transitively

---

## Implementation Phase 5: Application Bootstrap

<!-- parallelizable: false -->

### Step 5.1: Create `McpAccessCodeMiddleware.cs`

Validates the incoming `Authorization: Bearer {code}` header against the configured access code using `FixedTimeEquals`. Only active when `AccessControl:AccessCode` is configured. Returns 401 if the code is missing or incorrect.

Files:
* prompt-babbler-service/src/McpServer/McpAccessCodeMiddleware.cs — Access code validation middleware

```csharp
using System.Security.Cryptography;
using System.Text;

namespace PromptBabbler.McpServer;

public sealed class McpAccessCodeMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly string _accessCode = configuration["AccessControl:AccessCode"] ?? string.Empty;

    public async Task InvokeAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(_accessCode))
        {
            await next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var providedCode = authHeader["Bearer ".Length..].Trim();
        var validCode = Encoding.UTF8.GetBytes(_accessCode);
        var provided = Encoding.UTF8.GetBytes(providedCode);

        if (!CryptographicOperations.FixedTimeEquals(validCode, provided))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
```

Discrepancy references:
* DD-02 — Validation here; forwarding in ApiAuthDelegatingHandler

Success criteria:
* Uses `CryptographicOperations.FixedTimeEquals` (constant-time comparison)
* Middleware is only active when `AccessControl:AccessCode` is configured (short-circuit in `InvokeAsync`)
* Returns `401` for missing or incorrect code
* Does NOT log the access code value

Dependencies:
* None

---

### Step 5.2: Create `Program.cs`

The application entry point. Sets up the MCP server, conditional auth, HTTP client, and middleware pipeline.

Files:
* prompt-babbler-service/src/McpServer/Program.cs — Application entry point

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using ModelContextProtocol.AspNetCore.Authentication;
using PromptBabbler.McpServer;
using PromptBabbler.McpServer.Client;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var accessCode = builder.Configuration["AccessControl:AccessCode"] ?? string.Empty;
var azureAdClientId = builder.Configuration["AzureAd:ClientId"] ?? string.Empty;

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new() { Name = "PromptBabbler.McpServer", Version = "1.0.0" };
    options.ServerInstructions = "Prompt Babbler MCP server. Provides tools for managing babbles (voice transcriptions), prompt templates, and AI-generated prompts. Use search_babbles to find relevant voice notes, generate_prompt to create AI prompts from babbles, and the template tools to manage prompt templates.";
})
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly()
    .WithResourcesFromAssembly();

if (!string.IsNullOrEmpty(azureAdClientId))
{
    // Entra ID mode: JWT Bearer + MCP OAuth metadata + OBO
    var tenantId = builder.Configuration["AzureAd:TenantId"] ?? string.Empty;
    var apiScope = builder.Configuration["AzureAd:ApiScope"] ?? string.Empty;

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

    builder.Services.AddAuthentication()
        .AddMcp(options =>
        {
            options.ResourceMetadata = new()
            {
                AuthorizationServers = { $"https://login.microsoftonline.com/{tenantId}/v2.0" },
                ScopesSupported = ["mcp:tools"]
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddSingleton(new ApiAuthOptions(string.Empty, azureAdClientId, apiScope));
}
else
{
    // Anonymous mode or access code mode
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();
    });

    builder.Services.AddSingleton(new ApiAuthOptions(accessCode, string.Empty, string.Empty));
}

// NOTE: The double AddAuthentication() call pattern (AddMicrosoftIdentityWebApi + AddMcp) is from
// the MCP C# SDK documentation. If Step 7.1 build or runtime fails in Entra ID mode, investigate
// chaining .AddMcp() onto the existing IAuthenticationBuilder returned by AddMicrosoftIdentityWebApi
// rather than calling builder.Services.AddAuthentication() a second time.

builder.Services.AddTransient<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IPromptBabblerApiClient, PromptBabblerApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://api");
}).AddHttpMessageHandler<ApiAuthDelegatingHandler>();

var app = builder.Build();

if (!string.IsNullOrEmpty(accessCode) && string.IsNullOrEmpty(azureAdClientId))
{
    app.UseMiddleware<McpAccessCodeMiddleware>();
}

app.MapDefaultEndpoints();
app.MapMcp();

await app.RunAsync();
```

Success criteria:
* `builder.AddServiceDefaults()` called before any other service setup
* Conditional auth logic mirrors API's `AzureAd:ClientId` pattern
* `WithHttpTransport(options => options.Stateless = true)` present
* `MapDefaultEndpoints()` before `MapMcp()`
* No hardcoded secrets or connection strings
* Top-level statements (no `Program` class wrapper)

Dependencies:
* Steps 2.3, 5.1 complete (ApiAuthDelegatingHandler, McpAccessCodeMiddleware)

---

### Step 5.3: Create `appsettings.json`

Minimal configuration with placeholder values. No secrets.

Files:
* prompt-babbler-service/src/McpServer/appsettings.json — Default configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "",
    "TenantId": "",
    "ClientId": "",
    "ApiScope": ""
  },
  "AccessControl": {
    "AccessCode": ""
  }
}
```

Success criteria:
* No secrets present
* `AzureAd:ClientId` is empty string (enables anonymous mode by default for local dev)
* `AccessControl:AccessCode` is empty string

Dependencies:
* None

---

### Step 5.4: Create `Properties/launchSettings.json`

Enables running the MCP server locally via `dotnet run` or Aspire.

Files:
* prompt-babbler-service/src/McpServer/Properties/launchSettings.json — Launch configuration

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5242",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Success criteria:
* `launchBrowser: false` (no browser needed for MCP server)
* HTTP profile only (HTTPS not needed for local Aspire traffic)
* Port 5242 selected (not conflicting with API on 5241 or frontend)

Dependencies:
* None

---

## Implementation Phase 6: Aspire Integration

<!-- parallelizable: false -->

### Step 6.1: Add ProjectReference to `PromptBabbler.AppHost.csproj`

Add a reference to the McpServer project after the existing ProjectReference entries (after line 15, before the `</ItemGroup>` close tag on line 16).

Files:
* prompt-babbler-service/src/Orchestration/AppHost/PromptBabbler.AppHost.csproj — Add ProjectReference

Insert after the last existing `<ProjectReference>` line:
```xml
    <ProjectReference Include="..\..\McpServer\PromptBabbler.McpServer.csproj" />
```

Success criteria:
* Reference uses the correct relative path from AppHost to McpServer
* No version attributes (project references do not have versions)

Dependencies:
* Step 1.2 (McpServer.csproj must exist)

---

### Step 6.2: Update `AppHost.cs` to add the mcp-server resource

Insert the MCP server resource registration after the `apiService` block (after line 84, before the blank line and frontend registration on line 86).

Files:
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs — Add mcp-server resource

The new block to insert between the end of the apiService definition and the start of the frontendApp:
```csharp
var mcpClientId = builder.Configuration["EntraAuth:McpClientId"] ?? string.Empty;

var mcpServer = builder.AddProject<Projects.PromptBabbler_McpServer>("mcp-server")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("AzureAd__ClientId", mcpClientId)
    .WithEnvironment("AzureAd__TenantId", tenantId)
    .WithEnvironment("AzureAd__Instance", "https://login.microsoftonline.com/")
    .WithEnvironment("AccessControl__AccessCode", builder.Configuration["AccessControl:AccessCode"] ?? string.Empty);
```

Note: `tenantId` is already defined in AppHost.cs (check the existing variable name — match exactly). The `EntraAuth:McpClientId` config key follows the same pattern as the API's `EntraAuth:ClientId`.

Success criteria:
* `.WaitFor(apiService)` ensures MCP server starts after the API is ready
* `.WithReference(apiService)` injects service discovery for `https+http://api`
* `.WithExternalHttpEndpoints()` exposes the MCP endpoint to MCP clients
* Environment variable keys use double-underscore (`__`) for ASP.NET Core config hierarchy

Context references:
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs (Lines 66-86) — apiService definition and insertion point

Dependencies:
* Step 6.1 (AppHost.csproj must reference the McpServer project for the source generator to produce `Projects.PromptBabbler_McpServer`)

---

## Implementation Phase 7: Final Validation

<!-- parallelizable: false -->

### Step 7.1: Run full project validation

From `prompt-babbler-service/` directory:

```powershell
dotnet build PromptBabbler.slnx
dotnet format PromptBabbler.slnx --verify-no-changes --severity error
dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit --configuration Release --no-restore
```

### Step 7.2: Fix minor validation issues

Common issues to watch for:
* Missing `using` statements in `Program.cs` (add as needed based on build errors)
* `McpServerResource` attribute API differences between SDK versions (check `Uri` vs `ResourceUri` property name if build fails)
* `ChatRole` namespace — may be `Microsoft.Extensions.AI` or `ModelContextProtocol` depending on SDK version; check SDK docs
* `ITokenAcquisition` may require `Microsoft.Identity.Web.TokenAcquisition` namespace import

### Step 7.3: Report blocking issues

If validation failures require changes beyond minor namespace fixes or import corrections, document and report rather than attempting large-scale refactoring inline.

---

## Dependencies

* .NET 10 SDK
* `ModelContextProtocol.AspNetCore` v1.2.0
* `PromptBabbler.ServiceDefaults` project (already exists at correct path)

## Success Criteria

* `dotnet build PromptBabbler.slnx` exits with code 0
* `dotnet format --verify-no-changes` exits with code 0
* All 11 MCP Tools, 2 MCP Resources, and 1 MCP Prompt classes discoverable via assembly reflection
* HTTP client base address set to `https+http://api` for Aspire service discovery
* No project reference to `PromptBabbler.Domain`, `PromptBabbler.Api`, or `PromptBabbler.Infrastructure`
