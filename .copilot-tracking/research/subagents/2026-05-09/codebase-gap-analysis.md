# Codebase Gap Analysis: MAF Agentic MCP Tool

**Date:** 2026-05-09  
**Scope:** `prompt-babbler-service/` only

---

## Gap 1 — AppHost Wiring

**File:** `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`

The `mcpServer` resource block (starting around line 88) currently reads:

```csharp
var mcpServer = builder.AddProject<Projects.PromptBabbler_McpServer>("mcp-server")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("AzureAd__ClientId", mcpClientId)
    ...
```

**Missing:** `.WithReference(foundryProject)` and `.WaitFor(foundryProject)`.

**Required change — insert two chained calls after `.WithReference(apiService)`:**

```csharp
var mcpServer = builder.AddProject<Projects.PromptBabbler_McpServer>("mcp-server")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(foundryProject)   // ADD THIS
    .WaitFor(apiService)
    .WaitFor(foundryProject)         // ADD THIS
    .WithEnvironment("AzureAd__ClientId", mcpClientId)
    ...
```

**Environment variable Aspire injects:** When `.WithReference(foundryProject)` is added and the resource is named `"ai-foundry"`, Aspire injects:

```text
ConnectionStrings__ai-foundry=Endpoint=https://...;...
```

This maps to `IConfiguration.GetConnectionString("ai-foundry")` in the McpServer's `Program.cs` — the same key the Api project already reads at line 65 of `src/Api/Program.cs`.

---

## Gap 2 — MCP Server .csproj

**File:** `prompt-babbler-service/src/McpServer/PromptBabbler.McpServer.csproj`

**Current `<PackageReference>` entries:**

| Package | Status |
|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Present |
| `Microsoft.Identity.Web` | Present |
| `ModelContextProtocol.AspNetCore` | Present |

**Packages that need to be added:**

| Package | Action |
|---|---|
| `Microsoft.Agents.AI` | ADD `<PackageReference Include="Microsoft.Agents.AI" />` |
| `Microsoft.Agents.AI.Foundry` | ADD `<PackageReference Include="Microsoft.Agents.AI.Foundry" />` |
| `Azure.AI.Projects` | ADD `<PackageReference Include="Azure.AI.Projects" />` |
| `Azure.Identity` | ADD `<PackageReference Include="Azure.Identity" />` — version is already pinned in `Directory.Packages.props` at 1.21.0, so no version attribute needed |

**Resulting `<ItemGroup>` block:**

```xml
<ItemGroup>
  <PackageReference Include="Azure.AI.Projects" />
  <PackageReference Include="Azure.Identity" />
  <PackageReference Include="Microsoft.Agents.AI" />
  <PackageReference Include="Microsoft.Agents.AI.Foundry" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
  <PackageReference Include="Microsoft.Identity.Web" />
  <PackageReference Include="ModelContextProtocol.AspNetCore" />
</ItemGroup>
```

---

## Gap 3 — Directory.Packages.props

**File:** `prompt-babbler-service/Directory.Packages.props`

**Azure/AI packages already pinned:**

| Package | Pinned Version |
|---|---|
| `Azure.AI.OpenAI` | 2.1.0 |
| `Azure.AI.Speech.Transcription` | 1.0.0-beta.2 |
| `Azure.Identity` | 1.21.0 |
| `Microsoft.Azure.Cosmos` | 3.59.0 |
| `Microsoft.CognitiveServices.Speech` | 1.49.1 |
| `Microsoft.Extensions.AI.OpenAI` | 10.5.1 |
| `Aspire.Microsoft.Azure.Cosmos` | 13.3.0 |

**New `<PackageVersion>` entries required** (versions to be confirmed against NuGet at implementation time — MAF is in preview):

```xml
<!-- Microsoft Agent Framework -->
<PackageVersion Include="Azure.AI.Projects" Version="1.0.0-beta.9" />
<PackageVersion Include="Microsoft.Agents.AI" Version="0.3.0-beta" />
<PackageVersion Include="Microsoft.Agents.AI.Foundry" Version="0.3.0-beta" />
```

> **Note:** MAF packages (`Microsoft.Agents.AI.*`) are in early preview. Verify the latest stable-or-prerelease versions on NuGet before committing. The `Azure.AI.Projects` package version must align with what `Microsoft.Agents.AI.Foundry` depends on to avoid transitive conflicts.

---

## Gap 4 — Program.cs Registration Point

**File:** `prompt-babbler-service/src/McpServer/Program.cs`

**`IConfiguration` availability:** Yes — `builder.Configuration` is available from line 1 (`WebApplication.CreateBuilder(args)`). The full `IConfiguration` is also registered in the DI container automatically by ASP.NET Core and can be constructor-injected.

**Insertion point for `AddSingleton<PromptBabblerAgentOrchestrator>()`:**

Insert after the if/else block that registers `ApiAuthOptions` (ends around line 68) and before the HTTP client registrations (line 70):

```csharp
// --- existing: end of if/else auth block ---
builder.Services.AddSingleton(new ApiAuthOptions(accessCode, string.Empty, string.Empty));
}

// ADD HERE — after ApiAuthOptions registration, before HTTP clients:
builder.Services.AddSingleton<PromptBabblerAgentOrchestrator>();

// --- existing: HTTP client registrations ---
builder.Services.AddTransient<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IPromptBabblerApiClient, PromptBabblerApiClient>(client =>
```

The orchestrator's constructor can accept `IConfiguration` (or a named options type) and `TokenCredential` — both are resolvable at this point.

---

## Gap 5 — Credential Strategy

**Api project pattern** (`src/Api/Program.cs` lines 70–76):

```csharp
TokenCredential runtimeTokenCredential = builder.Environment.IsDevelopment()
    ? (!string.IsNullOrEmpty(tenantId)
        ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = tenantId, ExcludeManagedIdentityCredential = true })
        : new DefaultAzureCredential())
    : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
```

**McpServer `Program.cs` today:** No `builder.Environment.IsDevelopment()` check exists anywhere. There is no `TokenCredential` registered.

**Required addition to McpServer `Program.cs`** (after `builder.AddServiceDefaults()`, before service registrations):

```csharp
var tenantId = builder.Configuration["Azure:TenantId"];
TokenCredential credential = builder.Environment.IsDevelopment()
    ? (!string.IsNullOrEmpty(tenantId)
        ? new DefaultAzureCredential(new DefaultAzureCredentialOptions
          {
              TenantId = tenantId,
              ExcludeManagedIdentityCredential = true,
          })
        : new DefaultAzureCredential())
    : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
builder.Services.AddSingleton<TokenCredential>(credential);
```

**Usings to add:**

```csharp
using Azure.Core;
using Azure.Identity;
```

**`Azure:TenantId` availability in McpServer:** This config key is NOT currently forwarded to the McpServer via `WithEnvironment` in AppHost.cs. Must add:

```csharp
.WithEnvironment("Azure__TenantId", tenantId)
```

to the `mcpServer` block in AppHost.cs (mirrors what is already done for `apiService`).

---

## Gap 6 — Test Patterns

**Existing test class structure** (from `McpAccessCodeMiddlewareTests.cs` and `BabbleServiceTests.cs`):

```csharp
[TestClass]
[TestCategory("Unit")]
public sealed class SomeTests
{
    // Private readonly fields — substitutes created inline
    private readonly IDependency _dependency = Substitute.For<IDependency>();
    private readonly SUT _sut;

    public SomeTests()
    {
        _sut = new SUT(_dependency);
    }

    [TestMethod]
    public async Task MethodName_Condition_ExpectedResult()
    {
        // Arrange
        _dependency.Method(Arg.Any<string>()).Returns(expected);

        // Act
        var result = await _sut.MethodAsync("input");

        // Assert
        result.Should().Be(expected);
        await _dependency.Received(1).Method(Arg.Any<string>());
    }
}
```

**`IPromptBabblerApiClient` mocking:** `Substitute.For<IPromptBabblerApiClient>()` — standard NSubstitute interface substitution. Return values set with `.Returns(...)`.

**Recommended test structure for `PromptBabblerAgentOrchestrator`:**

MAF's `AIAgent` is a concrete class with no interface, making direct substitution impossible. Two viable approaches:

1. **Wrap behind an interface** — Define `IAgentOrchestrator` with the single public method (e.g., `Task<string> RunAsync(string userMessage, CancellationToken ct)`). `PromptBabblerAgentOrchestrator` implements it; tests substitute the interface. Unit tests for the orchestrator itself then test against a fake `AIProjectClient` or use integration-style tests.

1. **Factory delegate injection** — Inject `Func<AIAgent>` so tests can provide a stub factory. Less idiomatic in this codebase.

**Recommended approach for this codebase:** option 1. Define `IAgentOrchestrator` in `Domain/Interfaces/`. Implement in `McpServer/Agents/`. Test the orchestrator with an integration test (category `Integration`) rather than a unit test, since `AIAgent` itself cannot be substituted.

Test file location: `tests/unit/McpServer.UnitTests/Agents/PromptBabblerAgentOrchestratorTests.cs`  
Test class: `PromptBabbler.McpServer.UnitTests.Agents`

---

## Gap 7 — Namespace Conventions

**Namespaces in `src/McpServer/` today:**

| File / Folder | Namespace |
|---|---|
| `Program.cs` (top-level statements) | `PromptBabbler.McpServer` (implicit) |
| `McpAccessCodeMiddleware.cs` | `PromptBabbler.McpServer` |
| `Tools/BabbleTools.cs` | `PromptBabbler.McpServer.Tools` |
| `Tools/GeneratedPromptTools.cs` | `PromptBabbler.McpServer.Tools` |
| `Client/IPromptBabblerApiClient.cs` | `PromptBabbler.McpServer.Client` |
| `HealthChecks/` | `PromptBabbler.McpServer.HealthChecks` (inferred) |

**Pattern:** `PromptBabbler.McpServer.<FolderName>` — folder name is PascalCase, used verbatim as the namespace segment.

**Exact namespaces for new files:**

| New File | Namespace |
|---|---|
| `src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs` | `PromptBabbler.McpServer.Agents` |
| `src/McpServer/Tools/AgenticTools.cs` | `PromptBabbler.McpServer.Tools` |

---

## Blocking Issues / Surprises

1. **`Azure:TenantId` not forwarded to McpServer** — AppHost passes `Azure__TenantId` to `apiService` but not to `mcpServer`. The credential construction in the MCP server will silently use `null` for `TenantId` in dev unless this env var is added to the `mcpServer` block.

1. **MAF package versions unknown** — `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Foundry` are in early preview. The exact NuGet versions must be confirmed at implementation time. There is a known transitive conflict risk with `Azure.AI.Projects` version alignment.

1. **`AIAgent` is not mockable** — No interface is exposed by MAF's `AIAgent`. The orchestrator must be tested at integration level or hidden behind a custom interface (see Gap 6). This affects the test plan significantly.

1. **No `Aspire.Hosting.Foundry` package in McpServer** — The McpServer project references `ServiceDefaults` but has no direct Aspire hosting dependency. The Foundry connection string will arrive purely via the `ConnectionStrings__ai-foundry` environment variable injected by Aspire. No extra NuGet package is needed in the McpServer `.csproj` for this.

1. **`WaitFor(foundryProject)` may cause local startup issues** — In dev, `foundryProject` points to the live Azure AI Foundry service (not a local emulator). `WaitFor` performs a health check. Verify the Foundry resource exposes a health endpoint Aspire can probe, or use `WithReference` only (no `WaitFor`) to avoid blocking startup.
