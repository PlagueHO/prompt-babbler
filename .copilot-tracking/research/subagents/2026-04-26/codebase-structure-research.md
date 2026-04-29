<!-- markdownlint-disable-file -->
# Subagent Research: Codebase Structure Analysis for Babble Semantic Search

## Research Topics and Questions

1. Domain model — Babble entity definition, properties, existing vector/embedding fields
2. Cosmos DB infrastructure — configuration, repository pattern, container naming, indexing policies, create/update pipeline
3. API layer — endpoints, controllers, routing conventions, search endpoints, auth patterns
4. Orchestration layer — operation coordination, AI service usage, service registration
5. Frontend app — component structure, API call patterns, styling framework, auth context, search functionality, layout placement
6. Aspire/infrastructure configuration — service definitions, Cosmos DB and Azure OpenAI deployment, Bicep templates, model deployments
7. Project conventions — coding conventions, package management, test structure, architectural decisions

## Research Executed

### 1. Domain Model

#### Babble Entity — `prompt-babbler-service/src/Domain/Models/Babble.cs` (lines 1–31)

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
}
```

**Key findings:**

- C# sealed record with `init` properties (immutable pattern).
- Partition key: `userId` (used in Cosmos DB).
- **No existing vector/embedding fields** — no `float[]`, `ReadOnlyMemory<float>`, or embedding property.
- Fields: `Id`, `UserId`, `Title`, `Text`, `CreatedAt`, `Tags`, `UpdatedAt`, `IsPinned`.
- Uses `System.Text.Json` with explicit `[JsonPropertyName]` attributes.

#### Other Domain Models

- **GeneratedPrompt** — `prompt-babbler-service/src/Domain/Models/GeneratedPrompt.cs`: Child of Babble, partitioned by `babbleId`. Fields: `Id`, `BabbleId`, `UserId`, `TemplateId`, `TemplateName`, `PromptText`, `GeneratedAt`.
- **PromptTemplate** — `prompt-babbler-service/src/Domain/Models/PromptTemplate.cs`: Partitioned by `userId`, includes `Instructions`, `OutputTemplate`, `Examples`, `Guardrails`, etc.
- **UserProfile** — `prompt-babbler-service/src/Domain/Models/UserProfile.cs`: Fields: `Id`, `UserId`, `DisplayName`, `Email`, `Settings`, `CreatedAt`, `UpdatedAt`.
- **UserSettings** — nested record: `Theme`, `SpeechLanguage`.

#### Domain Interfaces — `prompt-babbler-service/src/Domain/Interfaces/`

- `IBabbleRepository.cs` — CRUD + `GetByUserAsync` with search, sort, pagination, pin filtering. Existing `search` parameter does title-only `CONTAINS` match.
- `IBabbleService.cs` — mirrors repository with added `userId` validation on updates.
- `IPromptGenerationService.cs` — streaming prompt generation + title generation.
- `ITranscriptionService.cs` — real-time speech transcription.
- No embedding service interface exists.

### 2. Cosmos DB Infrastructure

#### Repository Implementation — `prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs` (lines 1–170)

- Database name: `"prompt-babbler"` (constant at line 13).
- Container name: `"babbles"` (constant at line 14).
- Partition key: `/userId`.
- Uses `Microsoft.Azure.Cosmos` SDK directly (`CosmosClient`, `Container`, `QueryDefinition`).
- **Existing search** (line 40): `CONTAINS(LOWER(c.title), @search)` — simple title substring match only.
- Parameterized queries with `@userId`, `@search`, `@isPinned`.
- Pagination via Cosmos DB continuation tokens.
- Sorting: `ORDER BY c.createdAt DESC` or `ORDER BY c.title ASC/DESC`.
- Point reads via `ReadItemAsync` with partition key.
- Creates via `CreateItemAsync`, updates via `ReplaceItemAsync`, deletes via `DeleteItemAsync`.
- All operations scoped to `PartitionKey(userId)`.

#### Service Layer — `prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs` (lines 1–60)

- Thin service wrapper over repository.
- Cascade delete: `DeleteAsync` deletes all generated prompts for the babble first (via `IGeneratedPromptRepository`).
- Validates babble ownership on update.

#### Dependency Injection — `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs` (lines 1–76)

- All repositories registered as `Singleton` (Cosmos containers are thread-safe).
- `AddInfrastructure()` takes `speechRegion` and `aiServicesEndpoint`.
- Prompt template repository uses `MemoryCache`.
- `BuiltInTemplateSeedingService` is a hosted service.
- No embedding service registration exists.

#### Cosmos DB Configuration in API — `prompt-babbler-service/src/Api/Program.cs` (lines 29–47)

- Uses `builder.AddAzureCosmosClient("cosmos", ...)` from Aspire integration.
- In development: `DefaultAzureCredential`. In production: `ManagedIdentityCredential(SystemAssigned)`.
- STJ serializer with web defaults: `options.UseSystemTextJsonSerializerWithOptions`.
- Connection string name: `"cosmos"`.

#### All Cosmos DB Containers (4 containers, 1 database)

| Container | Partition Key | Used For |
|-----------|---------------|----------|
| `babbles` | `/userId` | User speech transcriptions |
| `generated-prompts` | `/babbleId` | Generated prompts (child of babble) |
| `prompt-templates` | `/userId` | Prompt templates |
| `users` | `/userId` | User profiles/settings |

#### Bicep Cosmos DB Configuration — `infra/main.bicep` (lines 365–460)

- AVM module: `br/public:avm/res/document-db/database-account:0.19.0`.
- Serverless capability: `EnableServerless`.
- **No vector indexing policy defined** — uses default container definitions with partition key paths only.
- Private endpoint with DNS zone integration.
- RBAC: `Cosmos DB Built-in Data Contributor` for Container App managed identity and deployment principal.

### 3. API Layer

#### Controllers — `prompt-babbler-service/src/Api/Controllers/`

| Controller | Route | Description |
|-----------|-------|-------------|
| `BabbleController.cs` | `/api/babbles` | Full CRUD + generate prompt + generate title + pin |
| `GeneratedPromptController.cs` | `/api/babbles/{id}/prompts` | CRUD for generated prompts |
| `PromptTemplateController.cs` | `/api/templates` | CRUD for prompt templates |
| `TranscriptionWebSocketController.cs` | `/api/transcribe/stream` | WebSocket speech-to-text |
| `UserController.cs` | `/api/user` | User profile + settings |
| `ConfigController.cs` | `/api/config` | Access status + config |

#### BabbleController Details — `prompt-babbler-service/src/Api/Controllers/BabbleController.cs` (lines 1–200+)

- Class attributes: `[ApiController]`, `[Authorize]`, `[RequiredScope("access_as_user")]`, `[Route("api/babbles")]`.
- Endpoints:
  - `GET /api/babbles` — paginated list with `search`, `sortBy`, `sortDirection`, `isPinned`, `pageSize`, `continuationToken` query params.
  - `GET /api/babbles/{id}` — single babble by ID.
  - `POST /api/babbles` — create babble.
  - `PUT /api/babbles/{id}` — update babble.
  - `PATCH /api/babbles/{id}/pin` — toggle pin.
  - `DELETE /api/babbles/{id}` — delete babble (cascade deletes generated prompts).
  - `POST /api/babbles/{id}/generate` — SSE streaming prompt generation.
  - `POST /api/babbles/{id}/generate-title` — AI title generation.
- User ID extraction: `User.GetUserIdOrAnonymous()` from `ClaimsPrincipalExtensions.cs` (returns `"_anonymous"` when auth is disabled).
- Input validation: inline checks (search max 200 chars, sortBy enum, sortDirection enum).
- Response mapping: `ToResponse()` method maps `Babble` → `BabbleResponse`.
- **No search endpoint exists** — the `search` param on `GET /api/babbles` does title-only substring filtering.

#### Request/Response Models

- `CreateBabbleRequest.cs`: `Title`, `Text`, `Tags`, `IsPinned`.
- `UpdateBabbleRequest.cs`: `Title`, `Text`, `Tags`, `IsPinned`.
- `BabbleResponse.cs`: `Id`, `Title`, `Text`, `Tags`, `CreatedAt`, `UpdatedAt`, `IsPinned`.
- `PagedResponse<T>.cs`: `Items`, `ContinuationToken`.

#### Authentication Patterns — `prompt-babbler-service/src/Api/Program.cs`

- **Anonymous single-user mode** (no `AzureAd:ClientId`): Synthetic `_anonymous` ClaimsPrincipal injected via middleware. All `[Authorize]` attributes pass. User ID = `"_anonymous"`.
- **Entra ID multi-user mode** (`AzureAd:ClientId` set): JWT Bearer via `Microsoft.Identity.Web`. User ID from Entra object ID claim.
- Access code middleware: optional `X-Access-Code` header check for additional protection.
- CORS: localhost in dev, configured origins in production.
- Health checks: `/health` for Cosmos DB, AI Foundry, managed identity.

### 4. Orchestration Layer

#### Aspire AppHost — `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs` (lines 1–70)

- **Azure AI Foundry**: `builder.AddFoundry("foundry")` → `foundry.AddProject("ai-foundry")` → `AddModelDeployment("chat", ...)`.
- Chat model: `gpt-5.3-chat` version `2026-03-03`, SKU `GlobalStandard` capacity 50.
- **No embedding model deployment** — only chat model.
- **Cosmos DB**: `builder.AddAzureCosmosDB("cosmos")` → `RunAsPreviewEmulator()` with DataExplorer.
- Database: `prompt-babbler`, containers defined: `prompt-templates`, `babbles`, `generated-prompts`, `users`.
- API service gets references to: `foundry`, `foundryProject`, `chatDeployment`, `cosmos`, all containers.
- Frontend: Vite app with pnpm, gets API reference + environment variables for MSAL.
- Environment variables: `Azure__TenantId`, `AZURE_TENANT_ID`, `Speech__Region`, `AzureAd__*`.

#### AI Service Usage

- `AzureOpenAiPromptGenerationService.cs` (lines 1–55): Uses `Microsoft.Extensions.AI.IChatClient` for streaming prompt generation and title generation. Not the embedding API.
- Speech transcription: `AzureSpeechTranscriptionService` connects to Azure AI Speech Service via STS token exchange.
- **No embedding generation service exists** in the codebase.

### 5. Frontend App

#### Tech Stack

- React 19, TypeScript 5.9, Vite 8.
- UI framework: **Shadcn/UI** (New York style) + **TailwindCSS v4**.
- Routing: React Router v7 with `BrowserRouter`.
- Icons: Lucide React.
- Toasts: Sonner.
- Components.json at root: Shadcn configuration.

#### Component Structure — `prompt-babbler-app/src/components/`

```
components/
├── babbles/          # BabbleBubbles, BabbleCard, BabbleEditor, BabbleList, BabbleListItem, BabbleListSection, DeleteBabbleDialog
├── layout/           # Header, PageLayout, AuthGuard, UserMenu, ThemeProvider, ErrorBoundary, BrowserCheck, AccessCodeDialog, StorageWarning
├── prompts/          # Prompt-related components
├── recording/        # Recording UI
├── settings/         # Settings components
├── templates/        # Template management
└── ui/               # Shadcn UI primitives: button, card, dialog, dropdown-menu, input, select, separator, skeleton, alert-dialog, badge, checkbox, label, scroll-area, textarea, tag-input, tag-list, error-banner
```

#### API Client — `prompt-babbler-app/src/services/api-client.ts` (lines 1–300+)

- Central `fetchJson<T>()` helper with error handling, HTML response detection, access code header injection.
- API base URL from Vite `__API_BASE_URL__` (Aspire service discovery).
- Auth token passed as `Bearer` header.
- All endpoints typed: `getBabbles`, `getBabble`, `createBabble`, `updateBabble`, `deleteBabble`, `pinBabble`, `generatePrompt`, `generateTitle`, `getTemplates`, etc.
- `getBabbles()` options: `continuationToken`, `pageSize`, `search`, `sortBy`, `sortDirection`, `isPinned`.
- **No semantic search API function exists** in the client.

#### Existing Search — `prompt-babbler-app/src/components/babbles/BabbleListSection.tsx` (lines 1–100)

- Search input in the "Older Babbles" list section (below the pinned bubbles).
- Debounced text input (300ms) that filters via `search` query parameter on `GET /api/babbles`.
- Currently does **title-only substring matching** via the backend.
- Uses Shadcn `Input` component with `Search` Lucide icon.
- Combined with sort controls (Date Created / Title, ASC/DESC) via `DropdownMenu`.
- Infinite scroll with `IntersectionObserver`.

#### Layout and Search Placement

- `Header.tsx`: Fixed header with nav items (Home, Record, Templates) + UserMenu. **No search in header currently.**
- `PageLayout.tsx`: Simple wrapper with `Header` + centered `main` content area (max-w-5xl).
- `HomePage.tsx`: Two sections — BabbleBubbles (pinned/recent top-6 cards) + BabbleListSection (filtered/sorted list with search).
- Routes: `/` (Home), `/record`, `/record/:babbleId`, `/babble/:id`, `/templates`, `/settings`.

#### Auth Context

- `authConfig.ts`: MSAL configuration with `__MSAL_CLIENT_ID__` and `__MSAL_TENANT_ID__` injected by Vite.
- `isAuthConfigured` export: `!!clientId` — true when MSAL client ID is configured.
- `useAuthToken` hook: acquires silent MSAL tokens for API calls.
- `AuthGuard` component: wraps pages requiring authentication.

#### Hooks — `prompt-babbler-app/src/hooks/`

- `useBabbles.ts`: Main hook for babble list management. Two parallel data flows: "bubbles" (pinned+recent top 6) and "list" (filtered/sorted/paginated). State: `search`, `sortBy`, `sortDirection`. Fetches with auth token.
- `useGeneratedPrompts.ts`, `usePromptGeneration.ts`, `useTemplates.ts`, `useSettings.ts`, `useUserSettings.ts`, `useTranscription.ts`, `useAudioRecording.ts`, `useTheme.ts`, `useAccessCode.ts`, `useLocalStorage.ts`.

#### TypeScript Types — `prompt-babbler-app/src/types/index.ts`

```typescript
export interface Babble {
  id: string;
  title: string;
  text: string;
  tags?: string[];
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
}
```

No vector/embedding fields in the frontend type.

### 6. Aspire/Infrastructure Configuration

#### azure.yaml

- API: `containerapp`, language `dotnet`, Docker build.
- Frontend: `staticwebapp`, language `js`, dist output.

#### model-deployments.json — `infra/model-deployments.json`

```json
[
  {
    "model": {
      "format": "OpenAI",
      "name": "gpt-5.3-chat",
      "version": "2026-03-03"
    },
    "name": "chat",
    "sku": { "name": "GlobalStandard", "capacity": 50 },
    "raiPolicyName": "PromptBabblerContentPolicy"
  }
]
```

**Only 1 model deployment** — chat model. **No embedding model deployed.**

#### Bicep Infrastructure — `infra/main.bicep`

- Cosmos DB: AVM module `document-db/database-account:0.19.0`, serverless, 4 containers with simple partition key definitions. No vector indexing policy.
- AI Foundry (Cognitive Services): Hosts LLM chat + Speech. Model deployments loaded from `model-deployments.json`.
- Private endpoints for Cosmos DB and Cognitive Services.
- RBAC roles: Cognitive Services OpenAI User, Cognitive Services Speech User, Cosmos DB Built-in Data Contributor.

### 7. Project Conventions

#### Build Configuration — `prompt-babbler-service/Directory.Build.props`

- Target framework: `net10.0`.
- Nullable: enabled.
- TreatWarningsAsErrors: true.
- ImplicitUsings: enabled.
- Testing platform: `TestingPlatformDotnetTestSupport`.

#### Package Versions — `prompt-babbler-service/Directory.Packages.props`

| Package | Version | Usage |
|---------|---------|-------|
| `Microsoft.Azure.Cosmos` | 3.58.0 | Cosmos DB SDK |
| `Azure.AI.OpenAI` | 2.1.0 | Azure OpenAI SDK |
| `Microsoft.Extensions.AI.OpenAI` | 10.5.0 | MEAI abstraction |
| `Azure.Identity` | 1.21.0 | Azure credentials |
| `Microsoft.CognitiveServices.Speech` | 1.49.1 | Speech SDK |
| `Microsoft.Identity.Web` | 4.7.0 | Entra ID auth |
| `Aspire.Hosting.Foundry` | 13.2.4-preview.1.26224.4 | Aspire AI Foundry |
| `Aspire.Microsoft.Azure.Cosmos` | 13.2.4 | Aspire Cosmos integration |
| `MSTest.TestFramework` | 4.2.1 | Testing |
| `FluentAssertions` | 8.9.0 | Test assertions |
| `NSubstitute` | 5.3.0 | Mocking |

#### Test Structure

```
tests/
├── unit/
│   ├── Api.UnitTests/
│   ├── Domain.UnitTests/
│   └── Infrastructure.UnitTests/
│       └── Services/
│           ├── BabbleServiceTests.cs
│           ├── CosmosBabbleRepositoryTests.cs
│           └── ... (per-service test files)
└── integration/
    ├── Api.IntegrationTests/
    ├── Infrastructure.IntegrationTests/
    └── Orchestration.IntegrationTests/
```

- Unit tests mirror service structure. Each service/repository has its own test file.
- MSTest SDK with FluentAssertions and NSubstitute.
- Test filter category: `Unit` and `Integration`.
- Coverage: Cobertura XML output.

#### Architecture Pattern

- **Clean Architecture** with strict dependency direction: Domain → Infrastructure → Api.
- Domain: pure records + interfaces, no external dependencies.
- Infrastructure: Azure SDK implementations.
- Api: ASP.NET Core controllers, DI registration, middleware.
- Orchestration: Aspire AppHost + ServiceDefaults (separate from application logic).

## Key Discoveries

### No Existing Vector/Embedding Infrastructure

- The Babble entity has no embedding fields.
- No embedding model is deployed (only `gpt-5.3-chat`).
- No `IEmbeddingService` interface or implementation exists.
- Cosmos DB containers have no vector indexing policy.
- The `Microsoft.Extensions.AI` package is already in use (`IChatClient`), which also provides `IEmbeddingGenerator<,>` abstraction.

### Existing Search is Title-Only

- `GET /api/babbles?search=...` does `CONTAINS(LOWER(c.title), @search)` — substring match on title only.
- Frontend debounces search input and passes to the API's `search` query parameter.
- The existing search UI is in `BabbleListSection.tsx` (the "Older Babbles" section on the home page).

### AI Service Integration Pattern

- Azure OpenAI accessed via `AzureOpenAIClient` from `Azure.AI.OpenAI` SDK.
- Wrapped with `Microsoft.Extensions.AI` `IChatClient` abstraction.
- Registered as singleton in DI from `Program.cs`.
- Connection string from Aspire: `ConnectionStrings:ai-foundry`.
- Account endpoint derived by stripping `/api/projects/{name}` from the project endpoint.
- TokenCredential: `DefaultAzureCredential` in dev, `ManagedIdentityCredential` in production.

### Single-User vs Multi-User

- Determined by `AzureAd:ClientId` presence.
- Anonymous mode: all data under `_anonymous` userId partition.
- Multi-user mode: data partitioned by Entra object ID.
- All queries are scoped to `PartitionKey(userId)` — search would need to follow this pattern.

### Cosmos DB Serverless

- Serverless mode (no provisioned throughput).
- AVM Bicep module: `document-db/database-account:0.19.0`.
- No existing indexing policy customization beyond defaults.
- Vector search requires adding a vector indexing policy and embedding property.

## Follow-on Questions

- What embedding model should be deployed alongside the chat model? (text-embedding-3-large or text-embedding-3-small)
- What embedding dimensions to use? (trade-off between quality and storage/performance)
- Should semantic search replace the existing title-only search or complement it as an additional mode?
- Should embedding generation be synchronous (in the create/update flow) or asynchronous (background processing)?
- What Cosmos DB vector search distance function to use? (cosine, dotProduct, euclidean)
- Should the search endpoint be a new route (e.g., `/api/babbles/search`) or enhance the existing `GET /api/babbles` with a `semanticSearch` parameter?

## Clarifying Questions

None — all questions were answerable through codebase analysis.
