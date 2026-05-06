# API Endpoints Research — Prompt Babbler Service

## Research Status: Complete

## Research Questions

1. What API controllers exist and what endpoints do they expose?
2. What are the domain models and their structure?
3. What are the service interfaces/contracts?
4. How does authentication work (anonymous, access code, Entra ID)?
5. What API configuration exists?
6. Is there an existing shared HTTP client library?

---

## 1. API Controllers & Endpoints

All controllers are under `prompt-babbler-service/src/Api/Controllers/`. All controllers (except `ConfigController`) use `[Authorize]` + `[RequiredScope("access_as_user")]`.

### BabbleController — `api/babbles`

| Method | Route | Description | Request Body | Response |
|--------|-------|-------------|-------------|----------|
| GET | `/api/babbles` | List babbles (paged) | — | `PagedResponse<BabbleResponse>` |
| GET | `/api/babbles/search` | Vector/semantic search | — | `BabbleSearchResponse` |
| GET | `/api/babbles/{id}` | Get babble by ID | — | `BabbleResponse` |
| POST | `/api/babbles` | Create babble | `CreateBabbleRequest` | `BabbleResponse` (201) |
| PUT | `/api/babbles/{id}` | Update babble | `UpdateBabbleRequest` | `BabbleResponse` |
| PATCH | `/api/babbles/{id}/pin` | Pin/unpin babble | `PinBabbleRequest` | `BabbleResponse` |
| DELETE | `/api/babbles/{id}` | Delete babble | — | 204 No Content |
| POST | `/api/babbles/{id}/generate` | Generate prompt (SSE stream) | `GeneratePromptRequest` | text/event-stream |
| POST | `/api/babbles/{id}/generate-title` | Auto-generate title | — | `BabbleResponse` |
| POST | `/api/babbles/upload` | Upload audio file → transcribe → create babble | multipart/form-data | `BabbleResponse` (201) |

**GET `/api/babbles` Query Parameters:**

- `continuationToken` (string?) — pagination token
- `pageSize` (int, default 20, max 100)
- `search` (string?, max 200 chars) — text search
- `sortBy` (string?) — `"createdAt"` or `"title"`
- `sortDirection` (string?) — `"asc"` or `"desc"`
- `isPinned` (bool?) — filter by pin status

**GET `/api/babbles/search` Query Parameters:**

- `query` (string, required, max 200 chars)
- `topK` (int, default 10, max 50)

**POST `/api/babbles/{id}/generate` SSE Response Format:**

```
data: {"text": "chunk of generated text"}
data: {"text": "more text..."}
data: {"promptId": "uuid-of-saved-prompt"}
data: [DONE]
```

**POST `/api/babbles/upload` Form Fields:**

- `file` (IFormFile, required) — audio file (MP3, WAV, WebM, OGG, M4A), max 500 MB
- `title` (string?, 1–200 chars)
- `language` (string?, BCP-47 tag like "en-US")

### GeneratedPromptController — `api/babbles/{babbleId}/prompts`

| Method | Route | Description | Request Body | Response |
|--------|-------|-------------|-------------|----------|
| GET | `/api/babbles/{babbleId}/prompts` | List prompts for babble | — | `PagedResponse<GeneratedPromptResponse>` |
| GET | `/api/babbles/{babbleId}/prompts/{id}` | Get prompt by ID | — | `GeneratedPromptResponse` |
| POST | `/api/babbles/{babbleId}/prompts` | Create generated prompt | `CreateGeneratedPromptRequest` | `GeneratedPromptResponse` (201) |
| DELETE | `/api/babbles/{babbleId}/prompts/{id}` | Delete prompt | — | 204 No Content |

**GET Query Parameters:**

- `continuationToken` (string?)
- `pageSize` (int, default 20, max 100)

### PromptTemplateController — `api/templates`

| Method | Route | Description | Request Body | Response |
|--------|-------|-------------|-------------|----------|
| GET | `/api/templates` | List all templates | — | `PromptTemplateResponse[]` |
| GET | `/api/templates/{id}` | Get template by ID | — | `PromptTemplateResponse` |
| POST | `/api/templates` | Create user template | `CreatePromptTemplateRequest` | `PromptTemplateResponse` (201) |
| PUT | `/api/templates/{id}` | Update user template | `UpdatePromptTemplateRequest` | `PromptTemplateResponse` |
| DELETE | `/api/templates/{id}` | Delete user template | — | 204 No Content |

**GET `/api/templates` Query Parameters:**

- `forceRefresh` (bool, default false) — bypass cache

**Restrictions:** Built-in templates cannot be modified or deleted (returns 403).

### UserController — `api/user`

| Method | Route | Description | Request Body | Response |
|--------|-------|-------------|-------------|----------|
| GET | `/api/user` | Get current user profile | — | `UserProfileResponse` |
| PUT | `/api/user/settings` | Update user settings | `UpdateUserSettingsRequest` | `UserProfileResponse` |

### ConfigController — `api/config` (NO AUTH REQUIRED)

| Method | Route | Description | Request Body | Response |
|--------|-------|-------------|-------------|----------|
| GET | `/api/config/access-status` | Check if access code required | — | `AccessControlStatusResponse` |

### TranscriptionWebSocketController — `api/transcribe`

| Method | Route | Description | Protocol |
|--------|-------|-------------|----------|
| GET | `/api/transcribe/stream` | Real-time audio transcription | WebSocket |

**Query Parameters:**

- `language` (string?) — BCP-47 language tag
- `access_token` (string?) — JWT for WebSocket auth

**WebSocket Protocol:**

- Client sends binary audio frames
- Server returns JSON messages: `{"text": "...", "isFinal": true/false}`

---

## 2. Domain Models

### Babble

```csharp
sealed record Babble {
    string Id
    string UserId
    string Title            // 1–200 chars
    string Text             // 1–50,000 chars
    DateTimeOffset CreatedAt
    IReadOnlyList<string>? Tags  // max 10, each max 30 chars
    DateTimeOffset UpdatedAt
    bool IsPinned
    float[]? ContentVector  // embedding vector (not exposed in API response)
}
```

### GeneratedPrompt

```csharp
sealed record GeneratedPrompt {
    string Id
    string BabbleId
    string UserId
    string TemplateId
    string TemplateName     // 1–100 chars
    string PromptText       // 1–50,000 chars
    DateTimeOffset GeneratedAt
}
```

### PromptTemplate

```csharp
sealed record PromptTemplate {
    string Id
    string UserId
    string Name             // 1–100 chars
    string Description      // 1–500 chars
    string Instructions     // 1–10,000 chars
    string? OutputDescription  // max 500 chars
    string? OutputTemplate    // max 5,000 chars
    IReadOnlyList<PromptExample>? Examples  // max 10
    IReadOnlyList<string>? Guardrails       // max 10, each max 200 chars
    string? DefaultOutputFormat  // "text" or "markdown"
    bool? DefaultAllowEmojis
    IReadOnlyList<string>? Tags  // max 10, each max 30 chars
    IReadOnlyDictionary<string, JsonElement>? AdditionalProperties
    bool IsBuiltIn
    DateTimeOffset CreatedAt
    DateTimeOffset UpdatedAt
}
```

### PromptExample

```csharp
sealed record PromptExample {
    string Input
    string Output
}
```

### UserProfile

```csharp
sealed record UserProfile {
    string Id
    string UserId
    string? DisplayName
    string? Email
    UserSettings Settings
    DateTimeOffset CreatedAt
    DateTimeOffset UpdatedAt
}
```

### UserSettings

```csharp
sealed record UserSettings {
    string Theme           // "light", "dark", "system"
    string SpeechLanguage  // BCP-47, max 10 chars
}
```

### BabbleSearchResult

```csharp
sealed record BabbleSearchResult(Babble Babble, double SimilarityScore);
```

### AccessControlStatusResponse

```csharp
sealed record AccessControlStatusResponse {
    bool AccessCodeRequired
}
```

### TemplateValidationResult

```csharp
sealed record TemplateValidationResult {
    bool IsValid
    IReadOnlyList<string> Errors
}
```

---

## 3. Domain Interfaces (Service Contracts)

### IBabbleService

- `GetByUserAsync(userId, continuationToken?, pageSize, search?, sortBy?, sortDirection?, isPinned?)` → `(IReadOnlyList<Babble>, string? token)`
- `GetByIdAsync(userId, babbleId)` → `Babble?`
- `CreateAsync(babble)` → `Babble`
- `UpdateAsync(userId, babble)` → `Babble`
- `DeleteAsync(userId, babbleId)` → void
- `SetPinAsync(userId, babbleId, isPinned)` → `Babble`
- `SearchAsync(userId, query, topN)` → `IReadOnlyList<BabbleSearchResult>`

### IGeneratedPromptService

- `GetByBabbleAsync(userId, babbleId, continuationToken?, pageSize)` → `(IReadOnlyList<GeneratedPrompt>, string? token)`
- `GetByIdAsync(userId, babbleId, promptId)` → `GeneratedPrompt?`
- `CreateAsync(userId, prompt)` → `GeneratedPrompt`
- `DeleteAsync(userId, babbleId, promptId)` → void

### IPromptTemplateService

- `GetTemplatesAsync(userId?, forceRefresh)` → `IReadOnlyList<PromptTemplate>`
- `GetByIdAsync(userId?, templateId)` → `PromptTemplate?`
- `CreateAsync(template)` → `PromptTemplate`
- `UpdateAsync(template)` → `PromptTemplate`
- `DeleteAsync(userId, templateId)` → void

### IPromptGenerationService

- `GeneratePromptStreamAsync(babbleText, template, promptFormat?, allowEmojis?)` → `IAsyncEnumerable<string>`
- `GenerateTitleAsync(babbleText)` → `string`

### IUserService

- `GetOrCreateAsync(userId, displayName?, email?)` → `UserProfile`
- `UpdateSettingsAsync(userId, settings)` → `UserProfile`
- `UpdateProfileAsync(userId, displayName?, email?)` → `UserProfile`

### IFileTranscriptionService

- `TranscribeAsync(stream, language?)` → `string`

### IRealtimeTranscriptionService

- `StartSessionAsync(language?)` → `TranscriptionSession`

### ITemplateValidationService

- `ValidateTemplateAsync(template)` → `TemplateValidationResult`

### IEmbeddingService

- (Used internally for vector search — generates embeddings for babble content)

### IPromptBuilder

- (Used internally — constructs the system prompt from template fields)

---

## 4. Authentication Architecture

The API supports **three modes** configured via `Program.cs`:

### Mode 1: Entra ID Authentication (Production)

**Trigger:** `AzureAd:ClientId` is configured (non-empty).

**Configuration (appsettings.json):**

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "Audience": "api://prompt-babbler-api",
    "Scopes": "access_as_user"
  }
}
```

**Behavior:**

- JWT Bearer authentication via `Microsoft.Identity.Web`
- Token validated against Azure AD
- User ID extracted from `objectidentifier` claim via `User.GetObjectId()`
- WebSocket auth: `?access_token={token}` query parameter (extracted in `OnMessageReceived` event)
- Required scope: `access_as_user`

### Mode 2: Anonymous Single-User Mode (Development)

**Trigger:** `AzureAd:ClientId` is empty/not configured.

**Behavior:**

- No real JWT validation
- Synthetic `ClaimsPrincipal` injected via middleware with:
  - `objectidentifier` = `"_anonymous"`
  - `scp` = `"access_as_user"`
- All `[Authorize]` and `[RequiredScope]` attributes pass
- All data scoped to `userId = "_anonymous"`
- All endpoints accessible without any token

### Mode 3: Access Code Protection (Optional Layer)

**Trigger:** `AccessControl:AccessCode` is non-empty (or `ACCESS_CODE` env var set).

**Behavior:**

- `AccessCodeMiddleware` runs BEFORE authentication
- Requires `X-Access-Code` header on every request (or `?access_code=` query param for WebSocket)
- Uses constant-time comparison (`CryptographicOperations.FixedTimeEquals`)
- Returns 401 with `{"error": "Access code required"}` on failure
- Allowlisted paths (bypass access code): `/health`, `/alive`, `/api/config/access-status`, `/api/error`, `/openapi`
- Can be combined WITH Entra ID auth (access code + JWT)

### Authentication Priority Order

1. Access Code middleware (if configured) — blocks non-allowlisted paths without valid code
2. Authentication middleware (Entra ID JWT or synthetic anonymous)
3. Authorization (`[Authorize]` + `[RequiredScope]`)

### ClaimsPrincipalExtensions

- `User.GetUserIdOrAnonymous()` → returns Entra ID object ID or `"_anonymous"`

---

## 5. API Configuration

### appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "",
    "ClientId": "",
    "Audience": "api://prompt-babbler-api",
    "Scopes": "access_as_user"
  },
  "PromptTemplates": {
    "CacheDurationMinutes": 5
  },
  "AccessControl": {
    "AccessCode": ""
  }
}
```

### Environment Variables

- `ACCESS_CODE` → Maps to `AccessControl:AccessCode`
- `AZURE_TENANT_ID` → Azure tenant ID
- Connection strings:
  - `cosmos` — Cosmos DB connection
  - `ai-foundry` — Azure AI Foundry endpoint (for OpenAI chat/embeddings)
  - `foundry` — Foundry account endpoint (for Speech STS)
- `Speech:Region` — Azure Speech service region
- `CORS:AllowedOrigins` — Comma-separated allowed origins

### CORS Configuration

- Development: allows `localhost`/`127.0.0.1` origins
- Production: configured origins + localhost

---

## 6. Existing HTTP Client Patterns

### Backend (.NET)

- **No shared HTTP client library exists.** There is no typed HTTP client for the API.
- The only `HttpClient` usage is in `AzureSpeechTranscriptionService` (static, for STS token exchange).
- Service defaults configure `HttpClientDefaults` with resilience handlers (from Aspire).

### Frontend (TypeScript)

- `prompt-babbler-app/src/services/api-client.ts` — TypeScript client wrapping `fetch()`
- Pattern: `fetchJson<T>(path, init?, accessToken?)` helper
- Handles: Bearer token injection, `X-Access-Code` header, error translation
- API base URL injected via Vite build-time variable `__API_BASE_URL__`
- All API calls go through this single module

---

## 7. API Response Models (Wire Format)

### PagedResponse\<T\>

```json
{
  "items": [...],
  "continuationToken": "string | null"
}
```

### BabbleResponse

```json
{
  "id": "string",
  "title": "string",
  "text": "string",
  "tags": ["string"] | null,
  "createdAt": "ISO 8601",
  "updatedAt": "ISO 8601",
  "isPinned": true
}
```

### BabbleSearchResponse

```json
{
  "results": [
    {
      "id": "string",
      "title": "string",
      "snippet": "string (max 200 chars + ...)",
      "tags": ["string"] | null,
      "createdAt": "ISO 8601",
      "isPinned": true,
      "score": 0.95
    }
  ]
}
```

### GeneratedPromptResponse

```json
{
  "id": "string",
  "babbleId": "string",
  "templateId": "string",
  "templateName": "string",
  "promptText": "string",
  "generatedAt": "ISO 8601"
}
```

### PromptTemplateResponse

```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "instructions": "string",
  "outputDescription": "string | null",
  "outputTemplate": "string | null",
  "examples": [{"input": "string", "output": "string"}] | null,
  "guardrails": ["string"] | null,
  "defaultOutputFormat": "text | markdown | null",
  "defaultAllowEmojis": true | null,
  "tags": ["string"] | null,
  "additionalProperties": {} | null,
  "isBuiltIn": true,
  "createdAt": "ISO 8601",
  "updatedAt": "ISO 8601"
}
```

### UserProfileResponse

```json
{
  "id": "string",
  "displayName": "string | null",
  "email": "string | null",
  "settings": {
    "theme": "light | dark | system",
    "speechLanguage": "en-US"
  },
  "createdAt": "ISO 8601",
  "updatedAt": "ISO 8601"
}
```

### AccessControlStatusResponse

```json
{
  "accessCodeRequired": true
}
```

---

## 8. Request Models (Wire Format)

### CreateBabbleRequest

```json
{
  "title": "string (1–200)",
  "text": "string (1–50,000)",
  "tags": ["string"] | null,
  "isPinned": true | null
}
```

### UpdateBabbleRequest

```json
{
  "title": "string (1–200)",
  "text": "string (1–50,000)",
  "tags": ["string"] | null,
  "isPinned": true | null
}
```

### PinBabbleRequest

```json
{
  "isPinned": true
}
```

### GeneratePromptRequest

```json
{
  "templateId": "string (required)",
  "promptFormat": "text | markdown | null",
  "allowEmojis": true | null
}
```

### CreateGeneratedPromptRequest

```json
{
  "templateId": "string (required)",
  "templateName": "string (1–100, required)",
  "promptText": "string (1–50,000, required)"
}
```

### CreatePromptTemplateRequest

```json
{
  "name": "string (1–100)",
  "description": "string (1–500)",
  "instructions": "string (1–10,000)",
  "outputDescription": "string (max 500) | null",
  "outputTemplate": "string (max 5,000) | null",
  "examples": [{"input": "string", "output": "string"}] | null,
  "guardrails": ["string (max 200)"] | null,
  "defaultOutputFormat": "text | markdown | null",
  "defaultAllowEmojis": true | null,
  "tags": ["string (max 30)"] | null,
  "additionalProperties": {} | null
}
```

### UpdatePromptTemplateRequest

Same fields as `CreatePromptTemplateRequest`.

### UpdateUserSettingsRequest

```json
{
  "theme": "light | dark | system",
  "speechLanguage": "string (max 10)"
}
```

---

## 9. Key Architecture Notes for MCP Server

1. **User scoping:** All data is scoped by `userId`. The MCP server will need to either:
   - Authenticate as a specific user (Entra ID token with `access_as_user` scope)
   - Use anonymous mode (single-user, `userId = "_anonymous"`)
   - Provide an access code via `X-Access-Code` header

2. **Streaming:** The `/api/babbles/{id}/generate` endpoint uses SSE (Server-Sent Events), not a standard JSON response. The MCP server will need to handle SSE parsing.

3. **Pagination:** List endpoints use Cosmos DB continuation tokens, not offset-based pagination.

4. **No existing .NET HTTP client:** There is no shared typed HTTP client for the API. An MCP server would need to create its own client or call the domain services directly (if in-process).

5. **JSON naming:** All API responses use `camelCase` (configured via `JsonNamingPolicy.CamelCase`).

6. **Health endpoints:** `/health` and `/alive` are mapped via Aspire service defaults (`MapDefaultEndpoints()`).

---

## Follow-on Questions (Out of Scope)

- How will the MCP server authenticate? (As a user? Service principal? Anonymous mode?)
- Will the MCP server call the HTTP API or reference domain services directly?
- What MCP tools should map to which API endpoints?

---

## Source Files Referenced

- `prompt-babbler-service/src/Api/Controllers/BabbleController.cs`
- `prompt-babbler-service/src/Api/Controllers/GeneratedPromptController.cs`
- `prompt-babbler-service/src/Api/Controllers/PromptTemplateController.cs`
- `prompt-babbler-service/src/Api/Controllers/UserController.cs`
- `prompt-babbler-service/src/Api/Controllers/ConfigController.cs`
- `prompt-babbler-service/src/Api/Controllers/TranscriptionWebSocketController.cs`
- `prompt-babbler-service/src/Api/Program.cs`
- `prompt-babbler-service/src/Api/appsettings.json`
- `prompt-babbler-service/src/Api/Middleware/AccessCodeMiddleware.cs`
- `prompt-babbler-service/src/Api/Extensions/ClaimsPrincipalExtensions.cs`
- `prompt-babbler-service/src/Api/Models/Requests/*.cs`
- `prompt-babbler-service/src/Api/Models/Responses/*.cs`
- `prompt-babbler-service/src/Domain/Models/*.cs`
- `prompt-babbler-service/src/Domain/Interfaces/*.cs`
- `prompt-babbler-service/src/Domain/Configuration/AccessControlOptions.cs`
- `prompt-babbler-app/src/services/api-client.ts`
- `docs/API.md`
