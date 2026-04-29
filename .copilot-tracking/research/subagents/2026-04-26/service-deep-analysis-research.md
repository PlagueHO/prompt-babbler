# Service Deep Analysis for Vector Search Integration

**Status:** Complete
**Date:** 2026-04-26
**Scope:** Full analysis of prompt-babbler-service codebase to identify exactly what must change for vector search integration.

---

## 1. Domain Model — Babble Entity

**File:** `prompt-babbler-service/src/Domain/Models/Babble.cs` (lines 1–31)

```csharp
public sealed record Babble
{
    [JsonPropertyName("id")]       public required string Id { get; init; }
    [JsonPropertyName("userId")]   public required string UserId { get; init; }
    [JsonPropertyName("title")]    public required string Title { get; init; }
    [JsonPropertyName("text")]     public required string Text { get; init; }
    [JsonPropertyName("createdAt")]public required DateTimeOffset CreatedAt { get; init; }
    [JsonPropertyName("tags")]     public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("updatedAt")]public required DateTimeOffset UpdatedAt { get; init; }
    [JsonPropertyName("isPinned")] public bool IsPinned { get; init; }
}
```

**Key observations:**

- C# `sealed record` with `init` properties — immutable by design; uses `with` expressions for updates.
- No `Embedding` or vector property exists today.
- All properties use `[JsonPropertyName]` for explicit serialization control.
- For vector search, a new property like `ReadOnlyMemory<float>? Embedding` will need to be added.

### Other Domain Models

| Model | File | Partition Key |
|---|---|---|
| `GeneratedPrompt` | `src/Domain/Models/GeneratedPrompt.cs` | `babbleId` |
| `PromptTemplate` | `src/Domain/Models/PromptTemplate.cs` | `userId` |
| `UserProfile` | `src/Domain/Models/UserProfile.cs` | `userId` |
| `AccessControlStatusResponse` | `src/Domain/Models/AccessControlStatusResponse.cs` | N/A |
| `AccessControlOptions` | `src/Domain/Configuration/AccessControlOptions.cs` | N/A (config) |

---

## 2. Domain Interfaces

### IBabbleRepository

**File:** `prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs`

```csharp
public interface IBabbleRepository
{
    Task<(IReadOnlyList<Babble> Items, string? ContinuationToken)> GetByUserAsync(
        string userId,
        string? continuationToken = null,
        int pageSize = 20,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null,
        bool? isPinned = null,
        CancellationToken cancellationToken = default);

    Task<Babble?> GetByIdAsync(string userId, string babbleId, CancellationToken cancellationToken = default);
    Task<Babble> CreateAsync(Babble babble, CancellationToken cancellationToken = default);
    Task<Babble> UpdateAsync(Babble babble, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, string babbleId, CancellationToken cancellationToken = default);
    Task<Babble> SetPinAsync(string userId, string babbleId, bool isPinned, CancellationToken cancellationToken = default);
}
```

**Key observations:**

- The `search` parameter in `GetByUserAsync` is for **text search** only — currently does `CONTAINS(LOWER(c.title), @search)` (title only, not text body).
- No vector search method exists. A new method like `SearchByVectorAsync` or `VectorSearchAsync` would be needed.
- Alternatively, the existing `GetByUserAsync` could be extended, but a separate method is cleaner.

### IBabbleService

**File:** `prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs`

- Mirrors `IBabbleRepository` exactly (thin pass-through pattern).
- Same `search` parameter structure.
- Will need a corresponding vector search method added.

### IPromptGenerationService

**File:** `prompt-babbler-service/src/Domain/Interfaces/IPromptGenerationService.cs`

```csharp
public interface IPromptGenerationService
{
    IAsyncEnumerable<string> GeneratePromptStreamAsync(...);
    Task<string> GenerateTitleAsync(string babbleText, CancellationToken cancellationToken = default);
}
```

- Uses `IChatClient` from `Microsoft.Extensions.AI` (MEAI) — already in the codebase.
- An `IEmbeddingGenerator<string, Embedding<float>>` from MEAI would follow the same pattern.

### Other Interfaces

| Interface | File | Notes |
|---|---|---|
| `IGeneratedPromptRepository` | `src/Domain/Interfaces/IGeneratedPromptRepository.cs` | Has `DeleteByBabbleAsync` for cascade delete |
| `IPromptTemplateRepository` | `src/Domain/Interfaces/IPromptTemplateRepository.cs` | — |
| `IUserRepository` | `src/Domain/Interfaces/IUserRepository.cs` | — |
| `IUserService` | `src/Domain/Interfaces/IUserService.cs` | — |
| `IPromptBuilder` | `src/Domain/Interfaces/IPromptBuilder.cs` | — |
| `IRealtimeTranscriptionService` | `src/Domain/Interfaces/ITranscriptionService.cs` | — |

---

## 3. Infrastructure — Cosmos DB Repository

### CosmosBabbleRepository

**File:** `prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs` (165 lines)

**Constants:**

```csharp
public const string DatabaseName = "prompt-babbler";
public const string ContainerName = "babbles";
```

**Constructor:**

```csharp
public CosmosBabbleRepository(CosmosClient cosmosClient, ILogger<CosmosBabbleRepository> logger)
{
    _container = cosmosClient.GetContainer(DatabaseName, ContainerName);
    _logger = logger;
}
```

- Receives `CosmosClient` via DI (singleton), gets the `babbles` container.
- No `Database` object is held — container is accessed directly.

### Current Text Search Query (lines 37–80)

```csharp
var queryText = new StringBuilder("SELECT * FROM c WHERE c.userId = @userId");

if (!string.IsNullOrEmpty(search))
{
    queryText.Append(" AND CONTAINS(LOWER(c.title), @search)");
}

if (isPinned.HasValue)
{
    queryText.Append(" AND c.isPinned = @isPinned");
}

var orderByField = sortBy == "title" ? "c.title" : "c.createdAt";
var orderByDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
queryText.Append($" ORDER BY {orderByField} {orderByDirection}");
```

**Critical observations:**

- Search is **case-insensitive title-only** using `CONTAINS(LOWER(c.title), @search)`.
- Text body (`c.text`) is NOT searched.
- Uses `QueryDefinition` with parameterized queries (safe from injection).
- Paginated via `MaxItemCount` + `ContinuationToken`.
- Partition key is always `userId` — all queries are scoped to a single user.
- No cross-partition queries exist.

### Query Options Pattern

```csharp
var options = new QueryRequestOptions
{
    PartitionKey = new PartitionKey(userId),
    MaxItemCount = pageSize,
};
```

### Other CRUD Operations

- `GetByIdAsync` — point read by `id` + partition key `userId`.
- `CreateAsync` — `CreateItemAsync` with partition key from `babble.UserId`.
- `UpdateAsync` — `ReplaceItemAsync` (full document replace).
- `SetPinAsync` — reads then replaces with `with { IsPinned = ..., UpdatedAt = ... }`.
- `DeleteAsync` — reads then deletes (validates existence first).

---

## 4. Infrastructure — BabbleService

**File:** `prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs` (75 lines)

- Thin service layer wrapping `IBabbleRepository`.
- `UpdateAsync` adds existence check before delegating to repository.
- `DeleteAsync` performs cascade delete: removes all `GeneratedPrompt` for the babble first, then the babble itself.
- No business logic transformations.
- For vector search, the service layer will need to:
  1. Accept the search query.
  1. Generate an embedding via `IEmbeddingGenerator`.
  1. Pass the embedding vector to the repository for Cosmos DB vector search.
  - OR: The repository does the embedding generation (less clean architecturally).

---

## 5. Infrastructure — AI Services

### AzureOpenAiPromptGenerationService

**File:** `prompt-babbler-service/src/Infrastructure/Services/AzureOpenAiPromptGenerationService.cs` (56 lines)

```csharp
public sealed class AzureOpenAiPromptGenerationService(
    IChatClient chatClient,
    IPromptBuilder promptBuilder) : IPromptGenerationService
```

- Uses **primary constructor** pattern.
- `IChatClient` from `Microsoft.Extensions.AI` — the MEAI abstraction.
- Two methods: `GeneratePromptStreamAsync` (SSE streaming) and `GenerateTitleAsync`.
- The `IChatClient` is registered in `Program.cs` using `AzureOpenAIClient` from `Azure.AI.OpenAI`.

### How IChatClient is registered (Program.cs lines 80–100)

```csharp
var openAiClient = new AzureOpenAIClient(accountEndpoint, runtimeTokenCredential);
var chatClient = openAiClient.GetChatClient("chat").AsIChatClient();
builder.Services.AddSingleton<IChatClient>(chatClient);
builder.Services.AddSingleton(openAiClient);
```

**Key observations for embedding integration:**

- `AzureOpenAIClient` is already registered as a singleton.
- For embeddings, we can do: `openAiClient.GetEmbeddingClient("embedding-deployment").AsIEmbeddingGenerator()`.
- This follows the exact same pattern as chat client registration.
- The `AzureOpenAIClient` authenticates via `TokenCredential` (DefaultAzureCredential in dev, ManagedIdentityCredential in production).

---

## 6. Infrastructure — Dependency Injection

**File:** `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs` (72 lines)

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    string speechRegion,
    string aiServicesEndpoint)
```

Registrations:

| Service | Implementation | Lifetime |
|---|---|---|
| `IPromptBuilder` | `PromptBuilder` | Singleton |
| `IPromptGenerationService` | `AzureOpenAiPromptGenerationService` | Transient |
| `ITemplateValidationService` | `TemplateValidationService` | Transient |
| `IRealtimeTranscriptionService` | `AzureSpeechTranscriptionService` | Singleton |
| `IPromptTemplateRepository` | `CosmosPromptTemplateRepository` | Singleton |
| `IPromptTemplateService` | `PromptTemplateService` | Singleton |
| `IBabbleRepository` | `CosmosBabbleRepository` | Singleton |
| `IBabbleService` | `BabbleService` | Singleton |
| `IGeneratedPromptRepository` | `CosmosGeneratedPromptRepository` | Singleton |
| `IGeneratedPromptService` | `GeneratedPromptService` | Singleton |
| `IUserRepository` | `CosmosUserRepository` | Singleton |
| `IUserService` | `UserService` | Singleton |

**Key observations:**

- All Cosmos repositories are **Singleton** — created once with the `CosmosClient`.
- The `addInfrastructure` method does NOT register `IChatClient` or `IEmbeddingGenerator` — those are registered in `Program.cs`.
- For vector search, `IEmbeddingGenerator<string, Embedding<float>>` should be registered in `Program.cs` alongside `IChatClient`.

---

## 7. API Layer

### BabbleController

**File:** `prompt-babbler-service/src/Api/Controllers/BabbleController.cs` (411 lines)

**Route:** `[Route("api/babbles")]`
**Auth:** `[Authorize]`, `[RequiredScope("access_as_user")]`

**Endpoints:**

| Method | Route | Description | Lines |
|---|---|---|---|
| GET | `/api/babbles` | List babbles (paginated, with search/sort/filter) | 47–80 |
| GET | `/api/babbles/{id}` | Get single babble | 82–93 |
| POST | `/api/babbles` | Create babble | 95–122 |
| PUT | `/api/babbles/{id}` | Update babble | 124–150 |
| PATCH | `/api/babbles/{id}/pin` | Pin/unpin babble | 152–170 |
| DELETE | `/api/babbles/{id}` | Delete babble | 172–186 |
| POST | `/api/babbles/{id}/generate` | Generate prompt (SSE) | 188–270 |
| POST | `/api/babbles/{id}/generate-title` | Generate title | 272–300 |

**Search endpoint (GET /api/babbles) query parameters:**

```csharp
[FromQuery] string? search = null,        // max 200 chars, title CONTAINS search
[FromQuery] string? sortBy = null,         // "createdAt" or "title"
[FromQuery] string? sortDirection = null,  // "asc" or "desc"
[FromQuery] bool? isPinned = null,
```

**User context:**

```csharp
var userId = User.GetUserIdOrAnonymous();
```

- Uses `ClaimsPrincipalExtensions.GetUserIdOrAnonymous()` — returns Entra ID object ID or `"_anonymous"`.
- All operations scoped to the authenticated user's userId.

### API Request/Response Models

**CreateBabbleRequest** (`src/Api/Models/Requests/CreateBabbleRequest.cs`):

```csharp
public sealed record CreateBabbleRequest
{
    public required string Title { get; init; }       // 1–200 chars
    public required string Text { get; init; }        // 1–50000 chars
    public IReadOnlyList<string>? Tags { get; init; } // max 20 tags, each max 50 chars
    public bool? IsPinned { get; init; }
}
```

**BabbleResponse** (`src/Api/Models/Responses/BabbleResponse.cs`):

```csharp
public sealed record BabbleResponse
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public required string CreatedAt { get; init; }   // ISO 8601 string
    public required string UpdatedAt { get; init; }   // ISO 8601 string
    public required bool IsPinned { get; init; }
}
```

**ToResponse mapping** (line 400):

```csharp
private static BabbleResponse ToResponse(Babble babble) => new()
{
    Id = babble.Id,
    Title = babble.Title,
    Text = babble.Text,
    Tags = babble.Tags,
    CreatedAt = babble.CreatedAt.ToString("o"),
    UpdatedAt = babble.UpdatedAt.ToString("o"),
    IsPinned = babble.IsPinned,
};
```

**Key observations for vector search:**

- The response model does NOT expose embeddings (and should not — embeddings are internal).
- A new search endpoint or query parameter for vector/semantic search could be added.
- A relevance score (`SimilarityScore`) might be added to the response for vector search results.

### PagedResponse

```csharp
public sealed record PagedResponse<T>
{
    public required IEnumerable<T> Items { get; init; }
    public string? ContinuationToken { get; init; }
}
```

---

## 8. API Program.cs — Full Registration Flow

**File:** `prompt-babbler-service/src/Api/Program.cs` (290 lines)

### Registration order

1. **Service defaults** — `builder.AddServiceDefaults()` (Aspire integration)
1. **Access control options** — `Configure<AccessControlOptions>`
1. **Cosmos DB** — `builder.AddAzureCosmosClient("cosmos", ...)` via Aspire integration
   - Uses `ManagedIdentityCredential` in production, `DefaultAzureCredential` in dev
   - Configures `System.Text.Json` serializer (NOT Newtonsoft)
1. **Controllers** — `builder.Services.AddControllers()` with camelCase JSON
1. **AI Foundry** — Parses `ConnectionStrings:ai-foundry`, creates `AzureOpenAIClient`, registers `IChatClient`
   - Chat deployment name: `"chat"` (hardcoded)
   - `AzureOpenAIClient` is also registered as singleton
1. **TokenCredential** — registered as singleton for Speech SDK and other Azure SDK clients
1. **Infrastructure** — `builder.Services.AddInfrastructure(speechRegion, aiServicesEndpoint)`
1. **Health checks** — Cosmos, AI Foundry, Managed Identity
1. **Authentication** — Entra ID JWT Bearer or anonymous mode
1. **CORS** — localhost in dev, configured origins in prod
1. **Middleware pipeline** — ExceptionHandler → CORS → AccessCode → WebSockets → Auth → Authorization → Controllers

### Important for embedding registration

The `AzureOpenAIClient` singleton is already registered (line ~100). To add embedding support:

```csharp
// Already exists:
builder.Services.AddSingleton(openAiClient);

// Would add:
var embeddingClient = openAiClient.GetEmbeddingClient("embedding-deployment")
    .AsIEmbeddingGenerator();
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(embeddingClient);
```

---

## 9. Orchestration — Aspire AppHost

**File:** `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs` (73 lines)

### Resources Defined

| Resource | Aspire Method | Config Key |
|---|---|---|
| AI Foundry Host | `builder.AddFoundry("foundry")` | — |
| AI Foundry Project | `foundry.AddProject("ai-foundry")` | — |
| Chat Model Deployment | `foundryProject.AddModelDeployment("chat", ...)` | `MicrosoftFoundry:chatModelName` / `chatModelVersion` |
| Cosmos DB | `builder.AddAzureCosmosDB("cosmos")` with emulator | — |
| Cosmos Database | `cosmos.AddCosmosDatabase("prompt-babbler")` | — |
| Containers | 4 containers (see below) | — |

### Cosmos Containers in AppHost

```csharp
var promptTemplatesContainer = cosmosDb.AddContainer("prompt-templates", "/userId");
var babblesContainer = cosmosDb.AddContainer("babbles", "/userId");
var generatedPromptsContainer = cosmosDb.AddContainer("generated-prompts", "/babbleId");
var usersContainer = cosmosDb.AddContainer("users", "/userId");
```

**Key observations:**

- No indexing policies, vector embedding policies, or vector indexes are defined.
- The Aspire Cosmos DB integration creates containers with **default indexing policy** only.
- For vector search, the `babbles` container needs:
  1. A `VectorEmbeddingPolicy` defining the embedding path, dimensions, data type, and distance function.
  1. A vector index in the indexing policy.
  1. The Aspire `AddContainer` API may not support vector policies directly — might need post-creation configuration or Bicep-level changes.

### Model Deployment

```csharp
var chatDeployment = foundryProject.AddModelDeployment(
    "chat",
    builder.Configuration["MicrosoftFoundry:chatModelName"] ?? "gpt-5.3-chat",
    builder.Configuration["MicrosoftFoundry:chatModelVersion"] ?? "2026-03-03",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });
```

**For vector search, a new embedding model deployment will need to be added:**

```csharp
var embeddingDeployment = foundryProject.AddModelDeployment(
    "embedding",
    "text-embedding-3-small",  // or text-embedding-3-large
    "1",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "Standard";
        deployment.SkuCapacity = 50;
    });
```

### API Service References

```csharp
var apiService = builder.AddProject<Projects.PromptBabbler_Api>("api")
    .WithReference(foundry)
    .WithReference(foundryProject)
    .WithReference(chatDeployment)
    .WithReference(cosmos)
    .WithReference(promptTemplatesContainer)
    .WithReference(babblesContainer)
    .WithReference(generatedPromptsContainer)
    .WithReference(usersContainer)
    .WaitFor(chatDeployment)
    .WaitFor(cosmos)
    // ... environment variables
```

- The new embedding deployment would need `.WithReference(embeddingDeployment)` and `.WaitFor(embeddingDeployment)`.

---

## 10. Package Management

### Directory.Packages.props — Current Versions

**File:** `prompt-babbler-service/Directory.Packages.props`

| Package | Version | Relevance |
|---|---|---|
| `Microsoft.Azure.Cosmos` | **3.58.0** | Cosmos DB SDK — needs vector search support (added in 3.36.0+) |
| `Azure.AI.OpenAI` | **2.1.0** | Azure OpenAI SDK — has embedding client support |
| `Microsoft.Extensions.AI.OpenAI` | **10.5.0** | MEAI bridge — has `AsIEmbeddingGenerator()` extension |
| `Aspire.Microsoft.Azure.Cosmos` | **13.2.4** | Aspire Cosmos integration |
| `Aspire.Hosting.Azure.CosmosDB` | **13.2.4** | Aspire Cosmos hosting |
| `Aspire.Hosting.Foundry` | **13.2.4-preview.1.26224.4** | Aspire AI Foundry hosting |
| `Azure.Identity` | **1.21.0** | Azure authentication |
| `Microsoft.Identity.Web` | **4.7.0** | Entra ID JWT validation |

**Key observations:**

- `Microsoft.Azure.Cosmos` 3.58.0 **supports vector search** — the `VectorEmbeddingPolicy` and `VectorIndexPath` APIs were added in SDK v3.36.0.
- `Azure.AI.OpenAI` 2.1.0 includes `GetEmbeddingClient()`.
- `Microsoft.Extensions.AI.OpenAI` 10.5.0 includes `AsIEmbeddingGenerator()` MEAI extension.
- **No new packages are needed** — all required SDK features are already available in the current versions.

### Directory.Build.props

**File:** `prompt-babbler-service/Directory.Build.props`

```xml
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
<AzureCosmosDisableNewtonsoftJsonCheck>true</AzureCosmosDisableNewtonsoftJsonCheck>
```

- .NET 10, nullable enabled, warnings as errors.
- `AzureCosmosDisableNewtonsoftJsonCheck` — confirms System.Text.Json is used with Cosmos.

### Project References

| Project | References | Packages (notable) |
|---|---|---|
| `Domain` | None | None (pure models/interfaces) |
| `Infrastructure` | `Domain` | `Microsoft.Azure.Cosmos`, `Microsoft.Extensions.AI.OpenAI`, `Azure.Identity`, `CognitiveServices.Speech` |
| `Api` | `Domain`, `Infrastructure`, `ServiceDefaults` | `Aspire.Microsoft.Azure.Cosmos`, `Azure.AI.OpenAI`, `Microsoft.Identity.Web` |
| `AppHost` | `Api` | `Aspire.Hosting.Foundry`, `Aspire.Hosting.Azure.CosmosDB`, `Aspire.Hosting.JavaScript` |

---

## 11. Infrastructure (Bicep) — Cosmos DB Configuration

**File:** `infra/main.bicep` (lines 364–447)

### Cosmos DB Account

```bicep
module cosmosDbAccount 'br/public:avm/res/document-db/database-account:0.19.0' = {
  params: {
    name: cosmosDbAccountName
    capabilitiesToAdd: ['EnableServerless']
    // ... private endpoints, diagnostics
    sqlDatabases: [
      {
        name: 'prompt-babbler'
        containers: [
          { name: 'prompt-templates', paths: ['/userId'] }
          { name: 'babbles',          paths: ['/userId'] }
          { name: 'generated-prompts', paths: ['/babbleId'] }
          { name: 'users',            paths: ['/userId'] }
        ]
      }
    ]
  }
}
```

**Key observations:**

- Uses AVM module `document-db/database-account:0.19.0`.
- Serverless mode (no provisioned throughput).
- Container definitions only specify partition key paths — **no indexing policies, no vector policies**.
- Default indexing policy (automatic indexing of all properties) is used.
- For vector search in Bicep, the container definition for `babbles` would need to include:
  - `vectorEmbeddingPolicy` with path `/embedding`, dimensions, dataType, distanceFunction.
  - `indexingPolicy` with `vectorIndexes` array.
  - This depends on the AVM module version supporting these properties.

### Model Deployments (Bicep Input)

**File:** `infra/model-deployments.json`

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

- Only one model deployment: `chat` (gpt-5.3-chat).
- For vector search, an embedding model deployment needs to be added:

```json
{
  "model": {
    "format": "OpenAI",
    "name": "text-embedding-3-small",
    "version": "1"
  },
  "name": "embedding",
  "sku": { "name": "Standard", "capacity": 120 }
}
```

---

## 12. Architecture Documentation

**File:** `docs/ARCHITECTURE.md`

### Design Principles

- Clean Architecture with strict dependency direction: Domain → Infrastructure → API.
- Domain has NO external dependencies (pure C# records + interfaces).
- All Cosmos repositories follow the same pattern: `CosmosClient` injected, `GetContainer()` in constructor.

### Data Layer Summary

| Container | Partition Key | Description |
|---|---|---|
| `babbles` | `/userId` | User speech transcriptions |
| `generated-prompts` | `/babbleId` | Generated prompts (child of babble) |
| `prompt-templates` | `/userId` | Prompt templates (built-in use `_builtin` userId) |
| `users` | `/userId` | User profiles and settings |

### Auth Modes

| Mode | Trigger | userId |
|---|---|---|
| Anonymous single-user | `AzureAd:ClientId` empty | `"_anonymous"` |
| Entra ID multi-user | `AzureAd:ClientId` set | Entra object ID |

---

## 13. Test Patterns

### Unit Tests — CosmosBabbleRepositoryTests

**File:** `prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/CosmosBabbleRepositoryTests.cs`

**Pattern:**

- Uses `NSubstitute` to mock `CosmosClient`, `Container`, `ILogger<T>`.
- `Substitute.For<Container>()` — mocks the Cosmos `Container` directly.
- `Substitute.For<CosmosClient>()` — wires `GetContainer()` to return mock container.
- Tests individual repository methods in isolation.
- Uses `FluentAssertions` for assertions.
- `[TestClass]`, `[TestMethod]`, `[TestCategory("Unit")]` — MSTest v4 attributes.

**Helper:**

```csharp
private static Babble CreateBabble(
    string id = "test-babble-id",
    string userId = "test-user-id") => new()
    {
        Id = id, UserId = userId,
        Title = "Test Babble",
        Text = "This is a test babble transcription.",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };
```

### Unit Tests — BabbleServiceTests

**File:** `prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/BabbleServiceTests.cs`

- Mocks `IBabbleRepository` and `IGeneratedPromptRepository`.
- Tests delegation pattern and existence checks.

### Unit Tests — BabbleControllerTests

**File:** `prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs`

- Mocks all services (`IBabbleService`, `IPromptGenerationService`, `IPromptTemplateService`, `IGeneratedPromptService`).
- Sets up `ClaimsPrincipal` with test user object ID.
- Tests HTTP response types and parameter clamping.

### Integration Tests — CustomWebApplicationFactory

**File:** `prompt-babbler-service/tests/integration/Api.IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs`

- Replaces all domain services with NSubstitute mocks.
- Uses `TestAuthHandler` for authenticated requests.
- Pattern: `ReplaceService<T>` removes existing registrations and adds mock.

### Integration Tests — BabbleFixtures

**File:** `prompt-babbler-service/tests/integration/Api.IntegrationTests/Fixtures/BabbleFixtures.cs`

- Shared test data factory with `CreateBabble()` helper.

---

## 14. Data Flow Summary: Babble Lifecycle

### Create Flow

```text
POST /api/babbles
  → BabbleController.CreateBabble()
    → User.GetUserIdOrAnonymous() → userId
    → new Babble { Id = Guid, UserId = userId, ... }
    → IBabbleService.CreateAsync(babble)
      → IBabbleRepository.CreateAsync(babble)
        → Container.CreateItemAsync(babble, partitionKey=userId)
    → 201 Created with BabbleResponse
```

### Search Flow (Current)

```text
GET /api/babbles?search=keyword&sortBy=createdAt&sortDirection=desc
  → BabbleController.GetBabbles()
    → userId = User.GetUserIdOrAnonymous()
    → IBabbleService.GetByUserAsync(userId, search=keyword, ...)
      → IBabbleRepository.GetByUserAsync(userId, search=keyword, ...)
        → SQL: SELECT * FROM c WHERE c.userId = @userId
                AND CONTAINS(LOWER(c.title), @search)
                ORDER BY c.createdAt DESC
    → 200 OK with PagedResponse<BabbleResponse>
```

### Update Flow

```text
PUT /api/babbles/{id}
  → BabbleController.UpdateBabble()
    → IBabbleService.GetByIdAsync() → existence check
    → existing with { Title, Text, Tags, UpdatedAt = now }
    → IBabbleService.UpdateAsync(userId, updated)
      → IBabbleRepository.GetByIdAsync() → existence check
      → IBabbleRepository.UpdateAsync()
        → Container.ReplaceItemAsync(updated, id, partitionKey=userId)
    → 200 OK with BabbleResponse
```

---

## 15. What Needs to Change for Vector Search

### Domain Layer Changes

| File | Change |
|---|---|
| `Domain/Models/Babble.cs` | Add `ReadOnlyMemory<float>? Embedding` property with `[JsonPropertyName("embedding")]` |
| `Domain/Interfaces/IBabbleRepository.cs` | Add `VectorSearchAsync(string userId, ReadOnlyMemory<float> queryVector, ...)` method |
| `Domain/Interfaces/IBabbleService.cs` | Add `SemanticSearchAsync(string userId, string query, ...)` method |

### Infrastructure Layer Changes

| File | Change |
|---|---|
| `Infrastructure/Services/CosmosBabbleRepository.cs` | Implement `VectorSearchAsync` using Cosmos DB `VectorDistance()` SQL function |
| `Infrastructure/Services/BabbleService.cs` | Implement `SemanticSearchAsync`: generate embedding via `IEmbeddingGenerator`, then call repository `VectorSearchAsync` |
| `Infrastructure/Services/BabbleService.cs` | Update `CreateAsync` to generate embedding before persisting |
| `Infrastructure/Services/BabbleService.cs` | Update `UpdateAsync` to regenerate embedding when text changes |
| `Infrastructure/DependencyInjection.cs` | No changes needed (embedding generator registered in Program.cs) |

### API Layer Changes

| File | Change |
|---|---|
| `Api/Program.cs` | Register `IEmbeddingGenerator<string, Embedding<float>>` alongside existing `IChatClient` |
| `Api/Controllers/BabbleController.cs` | Add semantic search query parameter or new endpoint |
| `Api/Models/Responses/BabbleResponse.cs` | Optionally add `SimilarityScore` for vector search results |

### Orchestration Changes

| File | Change |
|---|---|
| `Orchestration/AppHost/AppHost.cs` | Add embedding model deployment, add `.WithReference(embeddingDeployment)` to API service |

### Infrastructure (Bicep) Changes

| File | Change |
|---|---|
| `infra/model-deployments.json` | Add embedding model deployment definition |
| `infra/main.bicep` | Add vector embedding policy and vector index to babbles container |

### Test Changes

| File | Change |
|---|---|
| Unit tests for `CosmosBabbleRepository` | Add tests for `VectorSearchAsync` |
| Unit tests for `BabbleService` | Add tests for `SemanticSearchAsync`, embedding generation in create/update |
| Unit tests for `BabbleController` | Add tests for semantic search endpoint |
| Integration tests | Update fixtures and factories |

---

## 16. Clarifying Questions

1. **Embedding dimensions:** Should we use `text-embedding-3-small` (1536 dimensions) or `text-embedding-3-large` (3072 dimensions)? Smaller is cheaper and faster; larger has better quality.
1. **Search UX:** Should vector search replace the existing title-only text search, or be a separate "semantic search" option?
1. **Embedding scope:** Should only `text` be embedded, or should `title + text + tags` be concatenated for embedding?
1. **Distance function:** Cosmos DB supports `cosine`, `euclidean`, and `dotproduct`. Cosine is standard for text embeddings — confirm?

---

## 17. References

- `prompt-babbler-service/src/Domain/Models/Babble.cs` — Babble entity definition
- `prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs` — Repository contract
- `prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs` — Service contract
- `prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs` — Cosmos DB implementation with current text search query
- `prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs` — Service layer implementation
- `prompt-babbler-service/src/Infrastructure/Services/AzureOpenAiPromptGenerationService.cs` — MEAI IChatClient usage pattern
- `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs` — DI registration patterns
- `prompt-babbler-service/src/Api/Controllers/BabbleController.cs` — Full controller with all endpoints
- `prompt-babbler-service/src/Api/Program.cs` — Application startup and AI client registration
- `prompt-babbler-service/src/Api/Extensions/ClaimsPrincipalExtensions.cs` — User identity resolution
- `prompt-babbler-service/src/Api/Models/Requests/CreateBabbleRequest.cs` — Create request model
- `prompt-babbler-service/src/Api/Models/Responses/BabbleResponse.cs` — Response model
- `prompt-babbler-service/src/Api/Models/Responses/PagedResponse.cs` — Pagination wrapper
- `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs` — Aspire resource definitions
- `prompt-babbler-service/Directory.Packages.props` — Package versions
- `prompt-babbler-service/Directory.Build.props` — Build configuration
- `prompt-babbler-service/src/Infrastructure/PromptBabbler.Infrastructure.csproj` — Infrastructure project packages
- `prompt-babbler-service/src/Api/PromptBabbler.Api.csproj` — API project packages
- `prompt-babbler-service/src/Orchestration/AppHost/PromptBabbler.AppHost.csproj` — AppHost packages
- `infra/main.bicep` — Cosmos DB account and container definitions (lines 364–447)
- `infra/model-deployments.json` — AI model deployment configuration
- `docs/ARCHITECTURE.md` — Architecture overview and design principles
- `docs/API.md` — API endpoint reference
- `prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/CosmosBabbleRepositoryTests.cs` — Repository test pattern
- `prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/BabbleServiceTests.cs` — Service test pattern
- `prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs` — Controller test pattern
- `prompt-babbler-service/tests/integration/Api.IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs` — Integration test factory
- `prompt-babbler-service/tests/integration/Api.IntegrationTests/Fixtures/BabbleFixtures.cs` — Test fixtures
