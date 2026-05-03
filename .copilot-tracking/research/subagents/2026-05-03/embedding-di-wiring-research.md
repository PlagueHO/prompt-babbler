# Embedding/AI Service DI Wiring Research

## Research Questions

1. How does the Aspire AppHost configure AI services and pass them to the API project?
1. How does `Program.cs` register `IChatClient` and related AI services?
1. What NuGet packages provide embedding capabilities?
1. Is there any existing embedding-related configuration in appsettings or environment variables?
1. How does `AzureOpenAiPromptGenerationService` get its chat client (pattern for AI injection)?
1. What's needed to register `IEmbeddingGenerator` and `IEmbeddingService`?

---

## Findings

### 1. Aspire AppHost AI Configuration

**File:** `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs` (lines 1–75)

The AppHost uses `Aspire.Hosting.Foundry` to configure Azure AI Foundry resources:

```csharp
var foundry = builder.AddFoundry("foundry");                          // Line 7
var foundryProject = foundry.AddProject("ai-foundry");                // Line 8
var chatDeployment = foundryProject.AddModelDeployment(                // Line 12
    "chat",
    builder.Configuration["MicrosoftFoundry:chatModelName"] ?? "gpt-5.3-chat",
    builder.Configuration["MicrosoftFoundry:chatModelVersion"] ?? "2026-03-03",
    "OpenAI")
    .WithProperties(deployment => {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });
```

These resources are passed to the API project via `.WithReference()`:

```csharp
var apiService = builder.AddProject<Projects.PromptBabbler_Api>("api")  // Line 49
    .WithReference(foundry)                                             // Line 50
    .WithReference(foundryProject)                                      // Line 51
    .WithReference(chatDeployment)                                      // Line 52
    .WithReference(cosmos)                                              // Line 53
    ...
    .WaitFor(chatDeployment)                                            // Line 58
```

**Key:** Only ONE model deployment is configured (`"chat"`) — **no embedding deployment exists yet**.

**Configuration source:** `MicrosoftFoundry:chatModelName` and `MicrosoftFoundry:chatModelVersion` are set via environment variables in `launchSettings.json` (lines 19–20, 38–39).

**AppHost csproj packages** (`prompt-babbler-service/src/Orchestration/AppHost/PromptBabbler.AppHost.csproj`):

- `Aspire.Hosting.Foundry` (version `13.2.4-preview.1.26224.4` from Directory.Packages.props)
- `Aspire.Hosting.Azure.CosmosDB`
- `Aspire.Hosting.JavaScript`

---

### 2. Program.cs AI Service Registration

**File:** `prompt-babbler-service/src/Api/Program.cs` (lines 65–115)

The chat client is registered manually (NOT via Aspire client integration):

```csharp
// Line 68-69: Parse ai-foundry connection string
var aiFoundryConnStr = builder.Configuration.GetConnectionString("ai-foundry") ?? "";
var isAiConfigured = !string.IsNullOrWhiteSpace(aiFoundryConnStr);

// Line 71-78: Create runtime TokenCredential
TokenCredential runtimeTokenCredential = builder.Environment.IsDevelopment()
    ? (... DefaultAzureCredential ...)
    : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);

// Lines 98-108: Register IChatClient
if (Uri.TryCreate(aiEndpoint, UriKind.Absolute, out var parsedEndpoint))
{
    var accountEndpoint = new UriBuilder(parsedEndpoint.Scheme, parsedEndpoint.Host, parsedEndpoint.Port).Uri;
    var openAiClient = new AzureOpenAIClient(accountEndpoint, runtimeTokenCredential);
    var chatClient = openAiClient.GetChatClient("chat").AsIChatClient();  // "chat" = deployment name
    builder.Services.AddSingleton<IChatClient>(chatClient);
    builder.Services.AddSingleton(openAiClient);
}
```

**Important observations:**

- Uses `Azure.AI.OpenAI.AzureOpenAIClient` directly (not Aspire client integration)
- `"chat"` is the deployment name passed to `GetChatClient()`
- `AsIChatClient()` is from `Microsoft.Extensions.AI.OpenAI` — converts OpenAI ChatClient to `IChatClient`
- The `AzureOpenAIClient` singleton is ALSO registered — reusable for embedding
- The `runtimeTokenCredential` is registered as `TokenCredential` singleton (line 121)

---

### 3. NuGet Packages for Embedding

**File:** `prompt-babbler-service/Directory.Packages.props`

| Package | Version | Location |
|---------|---------|----------|
| `Azure.AI.OpenAI` | 2.1.0 | Api.csproj (line 8) |
| `Microsoft.Extensions.AI.OpenAI` | 10.5.0 | Infrastructure.csproj (line 9) |

**`Microsoft.Extensions.AI.OpenAI` v10.5.0** provides:

- `AsIChatClient()` extension method
- `AsIEmbeddingGenerator()` extension method (for `EmbeddingClient`)
- Transitively brings in `Microsoft.Extensions.AI.Abstractions` which defines `IEmbeddingGenerator<TInput, TEmbedding>`

**`Azure.AI.OpenAI` v2.1.0** provides:

- `AzureOpenAIClient` — which has `GetEmbeddingClient(deploymentName)` method
- `EmbeddingClient` (from underlying `OpenAI` SDK)

**No additional packages are needed** — the existing packages already support embedding generation.

---

### 4. Existing Embedding Configuration

**No embedding-specific configuration exists** in:

- `prompt-babbler-service/src/Api/appsettings.json` — no embedding deployment name or model reference
- `prompt-babbler-service/src/Api/appsettings.Development.json` — no embedding references
- `prompt-babbler-service/src/Orchestration/AppHost/Properties/launchSettings.json` — only `chatModelName`/`chatModelVersion`
- `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs` — only one model deployment ("chat")

**Gap:** An embedding model deployment (e.g., `text-embedding-3-large`) needs to be:

1. Added to the AppHost as a second `AddModelDeployment()`
1. Passed to the API via `.WithReference()`
1. Consumed in `Program.cs` to register `IEmbeddingGenerator<string, Embedding<float>>`

---

### 5. AI Service Injection Pattern (ChatClient)

**File:** `prompt-babbler-service/src/Infrastructure/Services/AzureOpenAiPromptGenerationService.cs` (lines 1–30)

```csharp
public sealed class AzureOpenAiPromptGenerationService(
    IChatClient chatClient,                    // Constructor-injected from DI
    IPromptBuilder promptBuilder) : IPromptGenerationService
```

Pattern: `IChatClient` is registered as a singleton in `Program.cs` and injected via constructor.

**File:** `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs` (line 19)

```csharp
services.AddTransient<IPromptGenerationService, AzureOpenAiPromptGenerationService>();
```

The service is registered as Transient — it relies on `IChatClient` being available in the DI container (registered upstream in `Program.cs`).

---

### 6. Existing EmbeddingService (Already Implemented but NOT Registered)

**File:** `prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs` (lines 1–24)

```csharp
public sealed class EmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : IEmbeddingService
{
    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text, CancellationToken cancellationToken = default)
    {
        var embeddings = await embeddingGenerator.GenerateAsync(
            [text], cancellationToken: cancellationToken);
        if (embeddings.Count == 0)
            throw new InvalidOperationException("Embedding generator returned no results...");
        return embeddings[0].Vector;
    }
}
```

**File:** `prompt-babbler-service/src/Domain/Interfaces/IEmbeddingService.cs` (lines 1–8)

```csharp
public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text, CancellationToken cancellationToken = default);
}
```

**Critical finding:** `EmbeddingService` and `IEmbeddingService` are fully implemented and tested, but:

- `EmbeddingService` is **NOT registered** in `DependencyInjection.cs`
- `IEmbeddingGenerator<string, Embedding<float>>` is **NOT registered** in `Program.cs`
- No embedding model deployment exists in the AppHost

---

## What's Needed to Register IEmbeddingGenerator and IEmbeddingService

### Step 1: AppHost — Add embedding model deployment

In `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`, add after the chat deployment:

```csharp
var embeddingDeployment = foundryProject.AddModelDeployment(
    "embedding",
    builder.Configuration["MicrosoftFoundry:embeddingModelName"] ?? "text-embedding-3-large",
    builder.Configuration["MicrosoftFoundry:embeddingModelVersion"] ?? "1",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "Standard";
        deployment.SkuCapacity = 120;
    });
```

Pass it to the API:

```csharp
.WithReference(embeddingDeployment)
.WaitFor(embeddingDeployment)
```

### Step 2: Program.cs — Register IEmbeddingGenerator

In `prompt-babbler-service/src/Api/Program.cs`, after the `IChatClient` registration (around line 107):

```csharp
var embeddingClient = openAiClient.GetEmbeddingClient("embedding").AsIEmbeddingGenerator();
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(embeddingClient);
```

Note: `AsIEmbeddingGenerator()` is from `Microsoft.Extensions.AI.OpenAI` (already referenced).

### Step 3: DependencyInjection.cs — Register IEmbeddingService

In `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs`, add:

```csharp
services.AddSingleton<IEmbeddingService, EmbeddingService>();
```

### Step 4: launchSettings.json — Add embedding model config

Add environment variables:

```json
"MicrosoftFoundry__embeddingModelName": "text-embedding-3-large",
"MicrosoftFoundry__embeddingModelVersion": "1"
```

---

## References

- `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs` — Aspire AppHost configuration
- `prompt-babbler-service/src/Orchestration/AppHost/PromptBabbler.AppHost.csproj` — AppHost packages
- `prompt-babbler-service/src/Orchestration/AppHost/Properties/launchSettings.json` — Environment config
- `prompt-babbler-service/src/Api/Program.cs` — Service registration and AI client wiring
- `prompt-babbler-service/src/Api/PromptBabbler.Api.csproj` — API project packages
- `prompt-babbler-service/src/Infrastructure/PromptBabbler.Infrastructure.csproj` — Infrastructure packages
- `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs` — DI registration
- `prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs` — Existing embedding service impl
- `prompt-babbler-service/src/Infrastructure/Services/AzureOpenAiPromptGenerationService.cs` — AI injection pattern
- `prompt-babbler-service/src/Domain/Interfaces/IEmbeddingService.cs` — Domain interface
- `prompt-babbler-service/Directory.Packages.props` — Centralized package versions
