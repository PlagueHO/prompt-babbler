# Source File Verification Research

## Research Topics

Verify current state of 12 source files for search functionality implementation planning.

---

## 1. `prompt-babbler-service/src/Domain/Models/Babble.cs`

**Total lines:** 31  
**Properties (all with `[JsonPropertyName]` attributes):**

| Line | Property | Type |
|------|----------|------|
| 8-9 | `Id` | `required string` (init) |
| 11-12 | `UserId` | `required string` (init) |
| 14-15 | `Title` | `required string` (init) |
| 17-18 | `Text` | `required string` (init) |
| 20-21 | `CreatedAt` | `required DateTimeOffset` (init) |
| 23-24 | `Tags` | `IReadOnlyList<string>?` (init) |
| 26-27 | `UpdatedAt` | `required DateTimeOffset` (init) |
| 29-30 | `IsPinned` | `bool` (init) |

**Key observations:**

- No `Embedding` or vector property exists yet.
- Sealed record, immutable with `init` setters.
- If embeddings are needed, a new property (e.g., `float[]? Embedding`) would be added after line 30.

---

## 2. `prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs`

**Total lines:** 27  
**Methods:**

| Line | Method |
|------|--------|
| 7-15 | `GetByUserAsync(userId, continuationToken?, pageSize, search?, sortBy?, sortDirection?, isPinned?, ct)` → `(IReadOnlyList<Babble>, string?)` |
| 17 | `GetByIdAsync(userId, babbleId, ct)` → `Babble?` |
| 19 | `CreateAsync(babble, ct)` → `Babble` |
| 21 | `UpdateAsync(babble, ct)` → `Babble` |
| 23 | `DeleteAsync(userId, babbleId, ct)` → `Task` |
| 25 | `SetPinAsync(userId, babbleId, isPinned, ct)` → `Babble` |

**Key observations:**

- No vector/semantic search method exists yet.
- `GetByUserAsync` already accepts a `search` parameter (used for title substring search).
- A new method like `SearchByVectorAsync` would be added for semantic search.

---

## 3. `prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs`

**Total lines:** 27  
**Methods (mirror repository but `UpdateAsync` takes `userId` parameter):**

| Line | Method |
|------|--------|
| 7-15 | `GetByUserAsync(userId, continuationToken?, pageSize, search?, sortBy?, sortDirection?, isPinned?, ct)` → `(IReadOnlyList<Babble>, string?)` |
| 17 | `GetByIdAsync(userId, babbleId, ct)` → `Babble?` |
| 19 | `CreateAsync(babble, ct)` → `Babble` |
| 21 | `UpdateAsync(userId, babble, ct)` → `Babble` |
| 23 | `DeleteAsync(userId, babbleId, ct)` → `Task` |
| 25 | `SetPinAsync(userId, babbleId, isPinned, ct)` → `Babble` |

**Key observations:**

- Same as repository — no semantic search method yet.
- `UpdateAsync` signature differs from repository (includes `userId` for auth check).

---

## 4. `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs`

**Total lines:** 87  
**Registrations:**

| Lines | Registration |
|-------|-------------|
| 18 | `IPromptBuilder` → `PromptBuilder` (Singleton) |
| 19 | `IPromptGenerationService` → `AzureOpenAiPromptGenerationService` (Transient) |
| 20 | `ITemplateValidationService` → `TemplateValidationService` (Transient) |
| 24-30 | `IRealtimeTranscriptionService` → `AzureSpeechTranscriptionService` (Singleton, factory) |
| 33-39 | `ITranscriptionClientWrapper` → `TranscriptionClientWrapper` (Singleton, factory) |
| 41 | `IFileTranscriptionService` → `AzureFastTranscriptionService` (Singleton) |
| 44 | `AddMemoryCache()` |
| 45-49 | `IPromptTemplateRepository` → `CosmosPromptTemplateRepository` (Singleton, factory) |
| 50 | `IPromptTemplateService` → `PromptTemplateService` (Singleton) |
| 51 | `BuiltInTemplateSeedingService` (HostedService) |
| 54-58 | `IBabbleRepository` → `CosmosBabbleRepository` (Singleton, factory) |
| 59 | `IBabbleService` → `BabbleService` (Singleton) |
| 62-66 | `IGeneratedPromptRepository` → `CosmosGeneratedPromptRepository` (Singleton, factory) |
| 67 | `IGeneratedPromptService` → `GeneratedPromptService` (Singleton) |
| 70-74 | `IUserRepository` → `CosmosUserRepository` (Singleton, factory) |
| 75 | `IUserService` → `UserService` (Singleton) |

**Key observations:**

- No `IEmbeddingService` registration exists yet.
- Method signature: `AddInfrastructure(services, speechRegion, aiServicesEndpoint)`.
- `IEmbeddingService` → `EmbeddingService` registration would be added (likely around line 59, near the babble registrations).

---

## 5. `prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs`

**Total lines:** 72  
**Constructor (lines 14-20):** Injects `IBabbleRepository`, `IGeneratedPromptRepository`, `ILogger<BabbleService>`.

**Key methods:**

| Lines | Method | Notes |
|-------|--------|-------|
| 44-47 | `CreateAsync` | Simple pass-through to repository |
| 49-58 | `UpdateAsync` | Checks existence first, then delegates to repository |
| 64-70 | `DeleteAsync` | Cascade deletes generated prompts first |

**Key observations:**

- `CreateAsync` does NOT generate embeddings — just passes through.
- `UpdateAsync` does NOT regenerate embeddings on text change.
- No `IEmbeddingService` dependency injected yet.
- To add embeddings: inject `IEmbeddingService`, call in `CreateAsync` and `UpdateAsync`.

---

## 6. `prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs`

**Total lines:** 157  
**Query pattern in `GetByUserAsync` (lines 26-82):**

- Uses `StringBuilder` to build parameterized Cosmos SQL queries.
- Supports `CONTAINS(LOWER(c.title), @search)` for title text search.
- Supports `ORDER BY` with dynamic field (`c.title` or `c.createdAt`) and direction.
- Supports filtering by `isPinned`.
- Uses `QueryRequestOptions` with `PartitionKey` and `MaxItemCount`.
- Returns continuation token for pagination.

**Other methods:**

| Lines | Method |
|-------|--------|
| 84-97 | `GetByIdAsync` — ReadItemAsync with NotFound handling |
| 99-110 | `CreateAsync` — CreateItemAsync |
| 112-122 | `UpdateAsync` — ReplaceItemAsync |
| 124-143 | `SetPinAsync` — Read + replace with new IsPinned/UpdatedAt |
| 145-157 | `DeleteAsync` — Existence check + DeleteItemAsync |

**Key observations:**

- Container name: `"babbles"`, database: `"prompt-babbler"`, partition key: `/userId`.
- No vector search query exists yet.
- A new `SearchByVectorAsync` method would use Cosmos DB vector search (VectorDistance function) or a cross-partition query.

---

## 7. `prompt-babbler-service/src/Api/Program.cs`

**Total lines:** ~260  
**Lines 60-120 (OpenAI client registration):**

| Lines | Content |
|-------|---------|
| 61-62 | Reads `Azure:TenantId` and `ai-foundry` connection string |
| 63 | `isAiConfigured` check |
| 64-73 | Creates `runtimeTokenCredential` (DefaultAzureCredential in dev, ManagedIdentity in prod) |
| 75-118 | If AI configured: parses endpoint, strips `/api/projects/...`, creates `AzureOpenAIClient`, gets `chatClient`, registers as `IChatClient` and `AzureOpenAIClient` singletons |

**Lines 120-156 (TokenCredential + Infrastructure):**

| Lines | Content |
|-------|---------|
| 120-122 | Registers `TokenCredential` singleton |
| 124-151 | Parses AI Services endpoint for Speech from connection strings |
| 153-155 | Calls `builder.Services.AddInfrastructure(speechRegion, aiServicesEndpoint)` |

**Key observations:**

- `AzureOpenAIClient` is registered as a singleton in DI (line ~116).
- No `IEmbeddingGenerator` registration exists.
- An embedding client registration (e.g., `openAiClient.GetEmbeddingClient("embedding").AsIEmbeddingGenerator()`) would be added near lines 115-117, inside the `if (isAiConfigured)` block.

---

## 8. `prompt-babbler-service/src/Api/Controllers/BabbleController.cs`

**Total lines:** ~498  
**Endpoints:**

| Lines | Method | Route | Description |
|-------|--------|-------|-------------|
| 44-80 | `GetBabbles` | `GET /api/babbles` | Paginated list with search, sort, pin filter |
| 82-93 | `GetBabble` | `GET /api/babbles/{id}` | Single babble by ID |
| 95-122 | `CreateBabble` | `POST /api/babbles` | Create from JSON body |
| 124-151 | `UpdateBabble` | `PUT /api/babbles/{id}` | Update existing babble |
| 153-170 | `PinBabble` | `PATCH /api/babbles/{id}/pin` | Toggle pin |
| 172-184 | `DeleteBabble` | `DELETE /api/babbles/{id}` | Delete with cascade |
| 186-274 | `GeneratePrompt` | `POST /api/babbles/{id}/generate` | SSE streaming prompt generation |
| 276-320 | `GenerateTitle` | `POST /api/babbles/{id}/generate-title` | AI title generation |
| 337-409 | `UploadAudio` | `POST /api/babbles/upload` | Audio file upload + transcription |

**Injected dependencies (constructor lines 27-35):**

- `IBabbleService`
- `IPromptGenerationService`
- `IPromptTemplateService`
- `IGeneratedPromptService`
- `IFileTranscriptionService`
- `ILogger<BabbleController>`

**Key observations:**

- No search-specific endpoint (search is a query parameter on `GetBabbles`).
- If a dedicated semantic search endpoint is needed, it would be added as a new action (e.g., `POST /api/babbles/search`).
- The `GetBabbles` endpoint already accepts `search` query param — currently does title substring matching.

---

## 9. `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`

**Total lines:** 75  
**Model deployments:**

| Lines | Deployment | Config Key | Default Model | Default Version |
|-------|-----------|------------|---------------|-----------------|
| 13-22 | `chat` | `MicrosoftFoundry:chatModelName` | `gpt-5.3-chat` | `2026-03-03` |

**Cosmos DB containers (lines 38-42):**

- `prompt-templates` (partition: `/userId`)
- `babbles` (partition: `/userId`)
- `generated-prompts` (partition: `/babbleId`)
- `users` (partition: `/userId`)

**Key observations:**

- Only ONE model deployment (`chat`) exists.
- No embedding model deployment is configured.
- An embedding deployment would be added after line 22 (e.g., `foundryProject.AddModelDeployment("embedding", ...)`) with a model like `text-embedding-3-large`.
- The `api` project would need `.WithReference(embeddingDeployment)` and `.WaitFor(embeddingDeployment)`.

---

## 10. `prompt-babbler-app/src/App.tsx`

**Total lines:** 64  
**Route structure (lines 45-52):**

```tsx
<Routes>
  <Route path="/" element={<HomePage />} />
  <Route path="/record" element={<RecordPage />} />
  <Route path="/record/:babbleId" element={<RecordPage />} />
  <Route path="/babble/:id" element={<BabblePage />} />
  <Route path="/templates" element={<TemplatesPage />} />
  <Route path="/settings" element={<SettingsPage />} />
</Routes>
```

**Key observations:**

- No `SearchCommand` component is currently mounted.
- The `PageLayout` wraps all routes (line 44) — a global search command (Cmd+K dialog) would likely be mounted:
  - Inside `<BrowserRouter>` but outside `<Routes>` (between lines 44 and 45, or after line 53).
  - Or inside `PageLayout` itself if it's a layout-level concern.
- Imports are at lines 1-16.

---

## 11. `prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs`

**Total lines:** 25  
**Constructor:** Uses primary constructor syntax — `EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)`

**Method:**

| Lines | Method | Signature |
|-------|--------|-----------|
| 10-23 | `GenerateEmbeddingAsync` | `(string text, CancellationToken ct)` → `ReadOnlyMemory<float>` |

**Implementation details:**

- Calls `embeddingGenerator.GenerateAsync([text], cancellationToken)`.
- Returns `embeddings[0].Vector`.
- Throws `InvalidOperationException` if no results returned.

**Key observations:**

- Already fully implemented and ready to use.
- Depends on `Microsoft.Extensions.AI.IEmbeddingGenerator<string, Embedding<float>>`.
- The `IEmbeddingGenerator` is NOT yet registered in DI (see DependencyInjection.cs and Program.cs findings).

---

## 12. `prompt-babbler-service/src/Domain/Interfaces/IEmbeddingService.cs`

**Total lines:** 8  
**Interface:**

```csharp
namespace PromptBabbler.Domain.Interfaces;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
```

**Key observations:**

- Simple single-method interface.
- Already defined and ready to use.
- No batch method (single text only).

---

## Summary of Gaps for Search Implementation

| Component | Current State | What's Needed |
|-----------|--------------|---------------|
| `Babble.cs` | No embedding property | Add `float[]? Embedding` property |
| `IBabbleRepository` | No vector search | Add vector search method |
| `IBabbleService` | No vector search | Add vector search method |
| `DependencyInjection.cs` | No `IEmbeddingService` registration | Register `IEmbeddingService` → `EmbeddingService` |
| `BabbleService.cs` | No embedding generation in Create/Update | Inject `IEmbeddingService`, generate on create/update |
| `CosmosBabbleRepository.cs` | Only title substring search | Add Cosmos vector search query |
| `Program.cs` | No `IEmbeddingGenerator` DI registration | Register embedding client from `AzureOpenAIClient` |
| `BabbleController.cs` | Search is title-only via query param | Add semantic search endpoint or enhance existing |
| `AppHost.cs` | No embedding model deployment | Add embedding model deployment |
| `App.tsx` | No SearchCommand | Mount search UI component |
| `EmbeddingService.cs` | Fully implemented | Already done |
| `IEmbeddingService.cs` | Fully defined | Already done |

---

## Follow-On Questions (Discovered)

1. What Cosmos DB vector indexing policy is configured on the `babbles` container? (Check `cosmos-babbles-vector-container.bicep`)
1. Is the `babbles` container already provisioned with a vector index, or does infrastructure need updating?
1. What embedding model and dimensions should be used? (text-embedding-3-large = 3072d, text-embedding-3-small = 1536d)
