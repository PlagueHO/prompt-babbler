# API Structure Research — Prompt Babbler

## Research Topics

1. API project structure and startup/DI configuration
2. Domain model definitions
3. Controller endpoint signatures with request/response types
4. Project structure patterns (solution, packages, references)
5. Orchestration project patterns

---

## 1. API Project Structure

### Project File: `prompt-babbler-service/src/Api/PromptBabbler.Api.csproj`

- SDK: `Microsoft.NET.Sdk.Web`
- RootNamespace: `PromptBabbler.Api`
- References Domain + Infrastructure + ServiceDefaults projects
- NuGet: `Aspire.Microsoft.Azure.Cosmos`, `Azure.AI.OpenAI`, `Microsoft.Identity.Web`

### Directory Layout

```text
src/Api/
├── Controllers/
│   ├── BabbleController.cs
│   ├── ConfigController.cs
│   ├── GeneratedPromptController.cs
│   ├── PromptTemplateController.cs
│   ├── TranscriptionWebSocketController.cs
│   └── UserController.cs
├── Extensions/
│   └── ClaimsPrincipalExtensions.cs
├── HealthChecks/
├── Middleware/
├── Models/
│   ├── Requests/
│   │   ├── CreateBabbleRequest.cs
│   │   ├── CreateGeneratedPromptRequest.cs
│   │   ├── CreatePromptTemplateRequest.cs
│   │   ├── GeneratePromptRequest.cs
│   │   ├── PinBabbleRequest.cs
│   │   ├── UpdateBabbleRequest.cs
│   │   ├── UpdatePromptTemplateRequest.cs
│   │   └── UpdateUserSettingsRequest.cs
│   └── Responses/
│       ├── BabbleResponse.cs
│       ├── BabbleSearchResponse.cs
│       ├── GeneratedPromptResponse.cs
│       ├── GeneratePromptResponse.cs
│       ├── PagedResponse.cs
│       ├── PromptTemplateResponse.cs
│       └── UserProfileResponse.cs
├── Program.cs
├── Properties/
└── appsettings.json / appsettings.Development.json
```

### Program.cs — Key Patterns

- Uses top-level statements (no `Startup.cs`)
- `builder.AddServiceDefaults()` — Aspire service defaults
- `builder.AddAzureCosmosClient("cosmos", ...)` — Aspire Cosmos integration
- `builder.Services.AddInfrastructure(...)` — single extension method on `IServiceCollection` in Infrastructure layer
- Controllers registered via `builder.Services.AddControllers().AddJsonOptions(camelCase)`
- Auth: `AddMicrosoftIdentityWebApi` when `AzureAd:ClientId` configured; falls back to anonymous mode
- CORS configured with `SetIsOriginAllowed` pattern
- Middleware pipeline: ExceptionHandler → CORS → AccessCodeMiddleware → WebSockets → Auth → Authorization → Controllers

### Auth Pattern

- All controllers annotated with `[Authorize]` + `[RequiredScope("access_as_user")]`
- `ConfigController` is the exception — no auth (public endpoint)
- User ID extracted via `User.GetUserIdOrAnonymous()` extension method
- In anonymous mode, synthetic ClaimsPrincipal injected with `_anonymous` object ID

---

## 2. Domain Models

### `Babble` (Domain/Models/Babble.cs)

```csharp
public sealed record Babble
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; init; }

    [JsonPropertyName("contentVector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? ContentVector { get; init; }
}
```

### `GeneratedPrompt` (Domain/Models/GeneratedPrompt.cs)

```csharp
public sealed record GeneratedPrompt
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("babbleId")]
    public required string BabbleId { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("templateId")]
    public required string TemplateId { get; init; }

    [JsonPropertyName("templateName")]
    public required string TemplateName { get; init; }

    [JsonPropertyName("promptText")]
    public required string PromptText { get; init; }

    [JsonPropertyName("generatedAt")]
    public required DateTimeOffset GeneratedAt { get; init; }
}
```

### `PromptTemplate` (Domain/Models/PromptTemplate.cs)

```csharp
public sealed record PromptTemplate
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("instructions")]
    public required string Instructions { get; init; }

    [JsonPropertyName("outputDescription")]
    public string? OutputDescription { get; init; }

    [JsonPropertyName("outputTemplate")]
    public string? OutputTemplate { get; init; }

    [JsonPropertyName("examples")]
    public IReadOnlyList<PromptExample>? Examples { get; init; }

    [JsonPropertyName("guardrails")]
    public IReadOnlyList<string>? Guardrails { get; init; }

    [JsonPropertyName("defaultOutputFormat")]
    public string? DefaultOutputFormat { get; init; }

    [JsonPropertyName("defaultAllowEmojis")]
    public bool? DefaultAllowEmojis { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    [JsonPropertyName("additionalProperties")]
    public IReadOnlyDictionary<string, JsonElement>? AdditionalProperties { get; init; }

    [JsonPropertyName("isBuiltIn")]
    public required bool IsBuiltIn { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
```

### `PromptExample` (Domain/Models/PromptExample.cs)

```csharp
public sealed record PromptExample
{
    [JsonPropertyName("input")]
    public required string Input { get; init; }

    [JsonPropertyName("output")]
    public required string Output { get; init; }
}
```

### `UserProfile` + `UserSettings` (Domain/Models/UserProfile.cs)

```csharp
public sealed record UserSettings
{
    [JsonPropertyName("theme")]
    public required string Theme { get; init; }

    [JsonPropertyName("speechLanguage")]
    public required string SpeechLanguage { get; init; }

    public static UserSettings Default => new() { Theme = "system", SpeechLanguage = "" };
}

public sealed record UserProfile
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("settings")]
    public required UserSettings Settings { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
```

### `BabbleSearchResult` (Domain/Models/BabbleSearchResult.cs)

```csharp
public sealed record BabbleSearchResult(Babble Babble, double SimilarityScore);
```

### `TemplateValidationResult` (Domain/Models/TemplateValidationResult.cs)

```csharp
public sealed record TemplateValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> Errors { get; init; }

    public static TemplateValidationResult Success() => new() { IsValid = true, Errors = [] };
    public static TemplateValidationResult Failure(IReadOnlyList<string> errors) => new() { IsValid = false, Errors = errors };
}
```

### `AccessControlStatusResponse` (Domain/Models/AccessControlStatusResponse.cs)

```csharp
public sealed record AccessControlStatusResponse
{
    [JsonPropertyName("accessCodeRequired")]
    public required bool AccessCodeRequired { get; init; }
}
```

---

## 3. Controller Endpoints

### BabbleController — `[Route("api/babbles")]`

| Method | Route | Request Body | Query Params | Response |
|--------|-------|--------------|--------------|----------|
| GET | `/api/babbles` | — | `continuationToken`, `pageSize` (1–100, default 20), `search`, `sortBy` (createdAt\|title), `sortDirection` (desc\|asc), `isPinned` | `PagedResponse<BabbleResponse>` |
| GET | `/api/babbles/search` | — | `query` (required, max 200), `topK` (1–50, default 10) | `BabbleSearchResponse` |
| GET | `/api/babbles/{id}` | — | — | `BabbleResponse` |
| POST | `/api/babbles` | `CreateBabbleRequest` | — | 201 → `BabbleResponse` |
| PUT | `/api/babbles/{id}` | `UpdateBabbleRequest` | — | `BabbleResponse` |
| PATCH | `/api/babbles/{id}/pin` | `PinBabbleRequest` | — | `BabbleResponse` |
| DELETE | `/api/babbles/{id}` | — | — | 204 |
| POST | `/api/babbles/{id}/generate` | `GeneratePromptRequest` | — | SSE stream (`text/event-stream`) |
| POST | `/api/babbles/{id}/generate-title` | — | — | `BabbleResponse` |
| POST | `/api/babbles/upload` | multipart/form-data (`file`, `title?`, `language?`) | — | `BabbleResponse` (201) |

### GeneratedPromptController — `[Route("api/babbles/{babbleId}/prompts")]`

| Method | Route | Request Body | Query Params | Response |
|--------|-------|--------------|--------------|----------|
| GET | `/api/babbles/{babbleId}/prompts` | — | `continuationToken`, `pageSize` (1–100, default 20) | `PagedResponse<GeneratedPromptResponse>` |
| GET | `/api/babbles/{babbleId}/prompts/{id}` | — | — | `GeneratedPromptResponse` |
| POST | `/api/babbles/{babbleId}/prompts` | `CreateGeneratedPromptRequest` | — | 201 → `GeneratedPromptResponse` |
| DELETE | `/api/babbles/{babbleId}/prompts/{id}` | — | — | 204 |

### PromptTemplateController — `[Route("api/templates")]`

| Method | Route | Request Body | Query Params | Response |
|--------|-------|--------------|--------------|----------|
| GET | `/api/templates` | — | `forceRefresh` (bool, default false) | `PromptTemplateResponse[]` |
| GET | `/api/templates/{id}` | — | — | `PromptTemplateResponse` |
| POST | `/api/templates` | `CreatePromptTemplateRequest` | — | 201 → `PromptTemplateResponse` |
| PUT | `/api/templates/{id}` | `UpdatePromptTemplateRequest` | — | `PromptTemplateResponse` |
| DELETE | `/api/templates/{id}` | — | — | 204 |

### UserController — `[Route("api/user")]`

| Method | Route | Request Body | Query Params | Response |
|--------|-------|--------------|--------------|----------|
| GET | `/api/user` | — | — | `UserProfileResponse` |
| PUT | `/api/user/settings` | `UpdateUserSettingsRequest` | — | `UserProfileResponse` |

### ConfigController — `[Route("api/config")]` (NO AUTH)

| Method | Route | Request Body | Query Params | Response |
|--------|-------|--------------|--------------|----------|
| GET | `/api/config/access-status` | — | — | `AccessControlStatusResponse` |

---

## 4. Request/Response DTOs

### Requests

```csharp
// CreateBabbleRequest
public sealed record CreateBabbleRequest
{
    public required string Title { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("isPinned")]
    public bool? IsPinned { get; init; }
}

// UpdateBabbleRequest — identical shape to Create
public sealed record UpdateBabbleRequest
{
    public required string Title { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("isPinned")]
    public bool? IsPinned { get; init; }
}

// PinBabbleRequest
public sealed record PinBabbleRequest
{
    [JsonPropertyName("isPinned")]
    public required bool IsPinned { get; init; }
}

// GeneratePromptRequest
public sealed record GeneratePromptRequest
{
    public required string TemplateId { get; init; }
    public string? PromptFormat { get; init; }
    public bool? AllowEmojis { get; init; }
}

// CreateGeneratedPromptRequest
public sealed record CreateGeneratedPromptRequest
{
    public required string TemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required string PromptText { get; init; }
}

// CreatePromptTemplateRequest
public sealed record CreatePromptTemplateRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Instructions { get; init; }
    public string? OutputDescription { get; init; }
    public string? OutputTemplate { get; init; }
    public IReadOnlyList<ExampleRequest>? Examples { get; init; }
    public IReadOnlyList<string>? Guardrails { get; init; }
    public string? DefaultOutputFormat { get; init; }
    public bool? DefaultAllowEmojis { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

// ExampleRequest (shared by Create/Update template)
public sealed record ExampleRequest
{
    public required string Input { get; init; }
    public required string Output { get; init; }
}

// UpdatePromptTemplateRequest — same shape as Create
public sealed record UpdatePromptTemplateRequest { /* identical fields */ }

// UpdateUserSettingsRequest
public sealed record UpdateUserSettingsRequest
{
    public required string Theme { get; init; }
    public required string SpeechLanguage { get; init; }
}
```

### Responses

```csharp
// PagedResponse<T> — generic pagination wrapper
public sealed record PagedResponse<T>
{
    public required IEnumerable<T> Items { get; init; }
    public string? ContinuationToken { get; init; }
}

// BabbleResponse
public sealed record BabbleResponse
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public required string CreatedAt { get; init; }    // ISO 8601 "o" format
    public required string UpdatedAt { get; init; }    // ISO 8601 "o" format
    [JsonPropertyName("isPinned")]
    public required bool IsPinned { get; init; }
}

// BabbleSearchResponse + BabbleSearchResultItem
public sealed record BabbleSearchResultItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Snippet { get; init; }      // truncated to 200 chars
    public IReadOnlyList<string>? Tags { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public bool IsPinned { get; init; }
    public required double Score { get; init; }
}
public sealed record BabbleSearchResponse
{
    public required IReadOnlyList<BabbleSearchResultItem> Results { get; init; }
}

// GeneratedPromptResponse
public sealed record GeneratedPromptResponse
{
    public required string Id { get; init; }
    public required string BabbleId { get; init; }
    public required string TemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required string PromptText { get; init; }
    public required string GeneratedAt { get; init; }  // ISO 8601 "o" format
}

// PromptTemplateResponse
public sealed record PromptTemplateResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Instructions { get; init; }
    public string? OutputDescription { get; init; }
    public string? OutputTemplate { get; init; }
    public IReadOnlyList<PromptExample>? Examples { get; init; }
    public IReadOnlyList<string>? Guardrails { get; init; }
    public string? DefaultOutputFormat { get; init; }
    public bool? DefaultAllowEmojis { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? AdditionalProperties { get; init; }
    public required bool IsBuiltIn { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

// UserProfileResponse + UserSettingsResponse
public sealed record UserSettingsResponse
{
    public required string Theme { get; init; }
    public required string SpeechLanguage { get; init; }
}
public sealed record UserProfileResponse
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public required UserSettingsResponse Settings { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

// GeneratePromptResponse (not currently used — SSE streaming instead)
public sealed record GeneratePromptResponse
{
    public required string PromptText { get; init; }
}
```

---

## 5. Project Structure Patterns

### Solution Structure (`PromptBabbler.slnx`)

```xml
<Solution defaultStartup="src/Orchestration/AppHost/PromptBabbler.AppHost.csproj">
  <Folder Name="/src/">
    <Project Path="src/Api/PromptBabbler.Api.csproj" />
    <Project Path="src/Domain/PromptBabbler.Domain.csproj" />
    <Project Path="src/Infrastructure/PromptBabbler.Infrastructure.csproj" />
    <Project Path="src/Orchestration/AppHost/PromptBabbler.AppHost.csproj" />
    <Project Path="src/Orchestration/ServiceDefaults/PromptBabbler.ServiceDefaults.csproj" />
  </Folder>
  <Folder Name="/tests/unit/">
    <Project Path="tests/unit/Api.UnitTests/PromptBabbler.Api.UnitTests.csproj" />
    <Project Path="tests/unit/Domain.UnitTests/PromptBabbler.Domain.UnitTests.csproj" />
    <Project Path="tests/unit/Infrastructure.UnitTests/PromptBabbler.Infrastructure.UnitTests.csproj" />
  </Folder>
  <Folder Name="/tests/integration/">
    <Project Path="tests/integration/Api.IntegrationTests/PromptBabbler.Api.IntegrationTests.csproj" />
    <Project Path="tests/integration/Infrastructure.IntegrationTests/PromptBabbler.Infrastructure.IntegrationTests.csproj" />
    <Project Path="tests/integration/IntegrationTests.Shared/PromptBabbler.IntegrationTests.Shared.csproj" />
    <Project Path="tests/integration/Orchestration.IntegrationTests/PromptBabbler.Orchestration.IntegrationTests.csproj" />
  </Folder>
</Solution>
```

### Central Package Management (`Directory.Packages.props`)

- `ManagePackageVersionsCentrally` = true
- `CentralPackageTransitivePinningEnabled` = true
- All package versions centralized — project files use `<PackageReference Include="X" />` without version

### Shared Build Properties (`Directory.Build.props`)

```xml
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
```

### Project Reference Hierarchy

```text
AppHost → Api
Api → Domain, Infrastructure, ServiceDefaults
Infrastructure → Domain
```

### Naming Conventions

- Root namespace matches folder: `PromptBabbler.Api`, `PromptBabbler.Domain`, `PromptBabbler.Infrastructure`
- All classes/records are `sealed`
- Domain models: immutable `sealed record`, `required` + `init`, `[JsonPropertyName]`
- Request/Response DTOs: `sealed record` in `Api/Models/Requests/` and `Api/Models/Responses/`
- Controllers map domain → response via private static `ToResponse()` methods
- Dates serialized as ISO 8601 strings via `.ToString("o")`

---

## 6. Orchestration / AppHost

### `PromptBabbler.AppHost.csproj`

- SDK: `Aspire.AppHost.Sdk/13.2.4` (version in csproj, not Directory.Packages.props)
- References: `Aspire.Hosting.Foundry`, `Aspire.Hosting.Azure.CosmosDB`, `Aspire.Hosting.JavaScript`
- Project reference: `..\..\Api\PromptBabbler.Api.csproj`

### Infrastructure DI Extension Method

```csharp
// PromptBabbler.Infrastructure.DependencyInjection
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    string speechRegion,
    string aiServicesEndpoint)
```

Registers all repositories and services (singletons for Cosmos repos, transient for generation services).

---

## 7. Key Patterns for HTTP Client Library

### Pagination

- Uses Cosmos DB continuation tokens (opaque strings)
- Generic `PagedResponse<T>` with `Items` + `ContinuationToken`
- Client sends `continuationToken` query param for next page

### SSE Streaming

- `POST /api/babbles/{id}/generate` returns `text/event-stream`
- Events: `data: {"text":"chunk"}\n\n` repeated, then `data: {"promptId":"..."}\n\n`, then `data: [DONE]\n\n`

### Error Responses

- Standard `ProblemDetails` for errors (400, 404, 502)
- Validation: inline `BadRequest("message")` strings

### Date Handling

- Domain models use `DateTimeOffset`
- Response DTOs serialize as `string` via `.ToString("o")` (ISO 8601)

### Auth Headers

- Bearer token in `Authorization` header
- Scope: `access_as_user`
- Access code sent via `AccessCodeMiddleware` (not researched in detail)

---

## Clarifying Questions

None — all research questions answered from codebase.

## Follow-on Research (Not Completed)

- [ ] Frontend `api-client.ts` patterns — how the React app currently calls these endpoints
- [ ] `AccessCodeMiddleware` implementation details (header name, validation logic)
- [ ] Domain interface definitions (full method signatures for `IBabbleService`, etc.)
- [ ] Existing test patterns for API integration tests (for testing the HTTP client)
