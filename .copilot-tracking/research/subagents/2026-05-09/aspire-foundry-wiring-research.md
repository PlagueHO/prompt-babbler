# Research: Aspire Foundry Wiring for McpServer

**Status:** Complete  
**Date:** 2026-05-09

---

## Research Topics

1. What env var does Aspire inject when `.WithReference(foundryProject)` is called on a project?
1. Does `AIProjectClient` accept the Aspire connection string format directly, or does it need parsing?

---

## Question 1: Env Var Injected by `.WithReference(foundryProject)`

### Source

Aspire.Hosting.Foundry README (github.com/microsoft/aspire/tree/main/src/Aspire.Hosting.Foundry):

> **Microsoft Foundry project** connection properties:
>
> - `Uri` — The project endpoint URI, format `https://<account>.services.ai.azure.com/api/projects/<project>`
> - `ConnectionString` — The connection string, format `Endpoint=<uri>`
> - `ApplicationInsightsConnectionString` — The Application Insights connection string for telemetry
>
> "Aspire exposes each property as an environment variable named `[RESOURCE]_[PROPERTY]`."

### Finding

For `foundryProject = foundry.AddProject("ai-foundry")`, adding `.WithReference(foundryProject)` to mcpServer injects **three** env vars:

| Env var | Value format | Read via |
|---|---|---|
| `ConnectionStrings__ai-foundry` | `Endpoint=https://...` | `builder.Configuration.GetConnectionString("ai-foundry")` |
| `AI_FOUNDRY_URI` | `https://account.services.ai.azure.com/api/projects/name` (raw URI) | `Environment.GetEnvironmentVariable("AI_FOUNDRY_URI")` |
| `AI_FOUNDRY_CONNECTIONSTRING` | `Endpoint=https://...` | rarely used directly |
| `AI_FOUNDRY_APPLICATIONINSIGHTSCONNECTIONSTRING` | AppInsights connstr (if configured) | rarely used directly |

**Key fact:** The standard Aspire connection string injection always produces `ConnectionStrings__<resourceName>` (double underscore = IConfiguration section separator). This is identical to what `apiService` already reads via `GetConnectionString("ai-foundry")`.

The aspire.dev hosting docs confirm: "When you call `WithReference(project)`, Aspire injects the standard connection string and project-specific connection properties such as the project endpoint and Application Insights connection string into the consuming resource."

### Current state in prompt-babbler

The AppHost (`prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`) currently adds `.WithReference(foundryProject)` to `apiService` but **not** to `mcpServer`. Adding it to `mcpServer` injects the same `ConnectionStrings__ai-foundry` env var.

---

## Question 2: AIProjectClient — Raw URI or Connection String Parsing?

### Source

Azure.AI.Projects README (`github.com/Azure/azure-sdk-for-net/blob/main/sdk/ai/Azure.AI.Projects/README.md`):

```csharp
var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT");
AIProjectClient projectClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
```

> "Support for project connection string and hub-based projects has been **discontinued**. We recommend creating a new Azure AI Foundry resource utilizing project endpoint."

### Finding

`AIProjectClient` requires the **raw project URI** (`https://account.services.ai.azure.com/api/projects/name`), NOT the Aspire connection string format (`Endpoint=...`).

This means **parsing is required** when consuming the Aspire `ConnectionStrings__ai-foundry` value. Fortunately the Aspire connection string format is simple — just strip the `Endpoint=` key prefix.

There are two approaches:

**Option A — Parse the connection string (mirrors Api/Program.cs pattern):**

```csharp
var connStr = builder.Configuration.GetConnectionString("ai-foundry") ?? "";
var projectEndpoint = "";
foreach (var part in connStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
{
    var trimmed = part.Trim();
    if (trimmed.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
    {
        projectEndpoint = trimmed["Endpoint=".Length..].TrimEnd('/');
        break;
    }
}
if (!string.IsNullOrEmpty(projectEndpoint) && Uri.TryCreate(projectEndpoint, UriKind.Absolute, out var projectUri))
{
    var projectClient = new AIProjectClient(projectUri, credential);
    builder.Services.AddSingleton(projectClient);
}
```

**Option B — Use the `AI_FOUNDRY_URI` env var directly (no parsing):**

```csharp
var projectEndpoint = builder.Configuration["AI_FOUNDRY_URI"]
    ?? Environment.GetEnvironmentVariable("AI_FOUNDRY_URI")
    ?? "";
if (!string.IsNullOrEmpty(projectEndpoint) && Uri.TryCreate(projectEndpoint, UriKind.Absolute, out var projectUri))
{
    var projectClient = new AIProjectClient(projectUri, credential);
    builder.Services.AddSingleton(projectClient);
}
```

Option A is preferred for consistency with the existing Api/Program.cs pattern and IConfiguration-first access. Option B is simpler but relies on the Aspire-specific env var name `AI_FOUNDRY_URI`.

Note: unlike `AzureOpenAIClient` (which needs the **account-level** endpoint — stripping `/api/projects/name`), `AIProjectClient` uses the **project-level** endpoint **as-is**.

---

## Key Discoveries

1. **Exact env var name:** `ConnectionStrings__ai-foundry` (also `AI_FOUNDRY_URI` for raw URI).
1. **Connection string format:** `Endpoint=https://account.services.ai.azure.com/api/projects/project-name`.
1. **AIProjectClient:** Requires bare `Uri`, NOT the `Endpoint=...` string — parsing is required.
1. **No secondary parsing:** Unlike `AzureOpenAIClient` which strips `/api/projects/name`, `AIProjectClient` uses the project endpoint URI directly.
1. **AppHost wiring:** Adding `.WithReference(foundryProject)` to `mcpServer` is sufficient — no other changes needed in AppHost.cs.

---

## Recommended Config Reading Code (for McpServer/Program.cs)

```csharp
// Injected by Aspire .WithReference(foundryProject) as ConnectionStrings__ai-foundry
var aiFoundryConnStr = builder.Configuration.GetConnectionString("ai-foundry") ?? "";

if (!string.IsNullOrWhiteSpace(aiFoundryConnStr))
{
    // Parse "Endpoint=<uri>" format — AIProjectClient needs the raw URI
    var projectEndpoint = "";
    foreach (var part in aiFoundryConnStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
    {
        var trimmed = part.Trim();
        if (trimmed.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            projectEndpoint = trimmed["Endpoint=".Length..].TrimEnd('/');
            break;
        }
    }
    if (Uri.TryCreate(projectEndpoint, UriKind.Absolute, out var projectUri))
    {
        builder.Services.AddSingleton(new AIProjectClient(projectUri, credential));
    }
}
```

---

## References

- Aspire.Hosting.Foundry README: github.com/microsoft/aspire/tree/main/src/Aspire.Hosting.Foundry
- Aspire Hosting docs: aspire.dev/integrations/cloud/azure/azure-ai-foundry/azure-ai-foundry-host/
- Azure.AI.Projects README: github.com/Azure/azure-sdk-for-net/blob/main/sdk/ai/Azure.AI.Projects/README.md
- AppHost.cs: prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs (lines 62-100)
- Api/Program.cs: prompt-babbler-service/src/Api/Program.cs (lines 66-130)
