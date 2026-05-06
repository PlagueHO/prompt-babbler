# DR-05: ACCESS_CODE vs AccessControl__AccessCode Environment Variable Research

**Status:** Complete
**Date:** 2026-05-05
**Scope:** Resolve DD-05 discrepancy — should the MCP server's AppHost.cs use `ACCESS_CODE` or `AccessControl__AccessCode`?

---

## Questions Under Investigation

1. How exactly does `AppHost.cs` pass the access code to the API service?
1. How does `Program.cs` read and map the `ACCESS_CODE` env var?
1. Does `AccessCodeMiddleware` read from config or env var directly?
1. How does ASP.NET Core's double-underscore convention work?
1. Is `AddEnvironmentVariables()` active in the existing API?
1. Which env var key name should the MCP server use, and why?

---

## Evidence

### 1. AppHost.cs — Access Code Handling for the API

File: `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`

**Key finding: `ACCESS_CODE` is NOT set via `WithEnvironment()` for the API in AppHost.cs.**

The API service definition (lines 64–81) sets only these environment variables via Aspire:

```csharp
var apiService = builder.AddProject<Projects.PromptBabbler_Api>("api")
    // ... references omitted ...
    .WithEnvironment("Azure__TenantId", tenantId)
    .WithEnvironment("AZURE_TENANT_ID", tenantId)
    .WithEnvironment("Speech__Region", builder.Configuration["Azure:Location"] ?? "")
    .WithEnvironment("AzureAd__ClientId", apiClientId)
    .WithEnvironment("AzureAd__TenantId", tenantId)
    .WithEnvironment("AzureAd__Instance", "https://login.microsoftonline.com/");
```

There is **no** `.WithEnvironment("ACCESS_CODE", ...)` call for the API. `ACCESS_CODE` is set externally (e.g., OS env var, appsettings, user secrets) — not via Aspire.

**Critical observation:** The API uses the double-underscore convention for all other env vars it reads via Aspire (`Azure__TenantId`, `AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__Instance`). The single outlier is `ACCESS_CODE`, which uses a non-hierarchical flat name and is handled outside Aspire via explicit mapping code.

### 2. Program.cs — ACCESS_CODE Mapping (API)

File: `prompt-babbler-service/src/Api/Program.cs`

**Lines 14–18:** Explicit mapping code that reads `ACCESS_CODE` OS env var and injects it into the config hierarchy:

```csharp
// Map ACCESS_CODE environment variable to AccessControl:AccessCode configuration key.
var accessCodeEnvVar = Environment.GetEnvironmentVariable("ACCESS_CODE");
if (!string.IsNullOrEmpty(accessCodeEnvVar))
{
    builder.Configuration["AccessControl:AccessCode"] = accessCodeEnvVar;
}
```

This is **manual bridging** code. It exists because `ACCESS_CODE` does not conform to ASP.NET Core's `section__key` convention. Without this code, `ACCESS_CODE` would never reach `builder.Configuration["AccessControl:AccessCode"]`.

**Line 20:** The access control options are registered from the config section:

```csharp
builder.Services.Configure<AccessControlOptions>(builder.Configuration.GetSection(AccessControlOptions.SectionName));
```

Where `AccessControlOptions.SectionName = "AccessControl"` (verified in AccessControlOptions.cs).

### 3. AccessCodeMiddleware — Config vs Env Var

File: `prompt-babbler-service/src/Api/Middleware/AccessCodeMiddleware.cs`

The middleware reads **exclusively from the DI-injected `IOptionsMonitor<AccessControlOptions>`** (line 33):

```csharp
public async Task InvokeAsync(HttpContext context, IOptionsMonitor<AccessControlOptions> optionsMonitor)
{
    var options = optionsMonitor.CurrentValue;

    if (string.IsNullOrEmpty(options.AccessCode))
    {
        await _next(context);
        return;
    }
    // ...
    if (string.IsNullOrEmpty(providedCode) || !FixedTimeEquals(options.AccessCode, providedCode))
    // ...
}
```

`AccessControlOptions.AccessCode` is populated via the `Configure<AccessControlOptions>()` call in Program.cs, which reads from `builder.Configuration.GetSection("AccessControl")`. The middleware has no direct `Environment.GetEnvironmentVariable()` call. It is **entirely dependent on the config system** being correctly populated.

### 4. ASP.NET Core Double-Underscore Convention

`WebApplication.CreateBuilder(args)` (used in .NET 6+) calls `AddEnvironmentVariables()` by default as part of the default configuration providers pipeline. The `AddEnvironmentVariables()` provider applies this transformation:

- `__` (double underscore) in an env var name → `:` (colon) in the config key
- `AccessControl__AccessCode` → `AccessControl:AccessCode`

This is documented in [ASP.NET Core Configuration docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0#environment-variables).

No explicit `AddEnvironmentVariables()` call is required — it is included automatically by `WebApplication.CreateBuilder()`.

### 5. AddEnvironmentVariables() Is Active in the API

`Program.cs` uses `var builder = WebApplication.CreateBuilder(args);` (line 14 context). This activates the full default provider chain including `AddEnvironmentVariables()`. Therefore:

- Setting `ACCESS_CODE` as an OS env var does NOT automatically populate `AccessControl:AccessCode` — the explicit mapping code at lines 14–18 is required.
- Setting `AccessControl__AccessCode` as an OS env var WOULD automatically populate `AccessControl:AccessCode` via the built-in provider — **no mapping code needed**.

The existing API's `ACCESS_CODE` pattern is a **workaround** for an unconventional flat env var name, not a best practice.

---

## Key Finding: Which Approach Is Correct for the MCP Server?

### The MCP Server Program.cs Does NOT Include Mapping Code

The plan (mcp-server-log.md DD-05) confirms the MCP server's `Program.cs` does not include the `ACCESS_CODE → AccessControl:AccessCode` bridging code that exists in the API's `Program.cs`. This is critical.

### Two Options Compared

| Option | Env Var Key | Program.cs mapping needed | Works via Aspire | Works as bare OS env var |
|--------|-------------|--------------------------|-----------------|--------------------------|
| A — Mirror API | `ACCESS_CODE` | **Yes** (explicit `GetEnvironmentVariable` + config injection) | Only if mapping code added | Only if mapping code added |
| B — DD-05 Plan | `AccessControl__AccessCode` | No | Yes (built-in `__` convention) | Yes (built-in `__` convention) |

### Recommendation: Use `AccessControl__AccessCode` (DD-05 is Correct)

**Option B (`AccessControl__AccessCode`) is correct for the MCP server.** Rationale:

1. **No mapping code required.** The double-underscore convention is handled by ASP.NET Core's built-in `AddEnvironmentVariables()` provider, which is always active. This eliminates a bespoke workaround.

1. **Consistent with all other env vars in AppHost.cs.** Every other hierarchical config value passed via Aspire uses the double-underscore convention (`Azure__TenantId`, `AzureAd__ClientId`, etc.). `ACCESS_CODE` in the API is the odd one out.

1. **The API's `ACCESS_CODE` pattern is a workaround, not a template.** The API has the explicit bridging code because `ACCESS_CODE` is an unconventional flat name (possibly chosen for Docker/ACA compatibility or historical reasons). The MCP server should not replicate this workaround.

1. **Both work if mapping code is present, but Option A silently fails without it.** If a future developer edits the MCP server's `Program.cs` without knowing about the `ACCESS_CODE` convention, the access code feature will silently not activate (options will bind with null). Option B cannot silently fail in this way.

1. **Minor inconsistency with the API's external env var name.** Operators who set `ACCESS_CODE` as a bare OS env var for the API cannot reuse that exact var name for the MCP server. They must use `AccessControl__AccessCode` instead. This is a **documentation concern only** — it is not a failure mode, and it should be noted in the MCP server's README or configuration guide.

---

## Clarifying Questions (None Required)

All questions are answered through code inspection. No ambiguities remain.

---

## Summary

| Question | Answer |
|----------|--------|
| How does AppHost.cs pass `ACCESS_CODE` to the API? | It does **not**. `ACCESS_CODE` is not set via `.WithEnvironment()` in AppHost.cs at all. |
| How does Program.cs consume `ACCESS_CODE`? | Explicit bridging code (lines 14–18): `GetEnvironmentVariable("ACCESS_CODE")` → `builder.Configuration["AccessControl:AccessCode"]`. |
| Does AccessCodeMiddleware read env var or config? | Config only, via `IOptionsMonitor<AccessControlOptions>`. No direct env var access. |
| Is `AddEnvironmentVariables()` active in the API? | Yes — activated automatically by `WebApplication.CreateBuilder()`. |
| Does `AccessControl__AccessCode` work without mapping code? | Yes — `__` → `:` is handled by the built-in env var provider. |
| Which should the MCP server use? | **`AccessControl__AccessCode`** — DD-05 is correct. No mapping code needed. Consistent with all other Aspire env vars. |
| Should the research document be updated? | Yes — the original research document recommending `ACCESS_CODE` was based on mirroring the API pattern. The recommendation should be updated to `AccessControl__AccessCode` and note why the API's `ACCESS_CODE` pattern is a workaround, not a model to follow. |
