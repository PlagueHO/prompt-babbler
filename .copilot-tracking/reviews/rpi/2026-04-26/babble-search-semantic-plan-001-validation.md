<!-- markdownlint-disable-file -->
# RPI Validation: Phase 1 & 2

**Plan**: babble-search-semantic-plan.instructions.md
**Changes log**: babble-search-semantic-changes.md
**Research**: babble-search-semantic-research.md
**Validation date**: 2026-04-26
**Status**: **Passed**

## Phase 1: Domain Model and Interfaces

### Step 1.1: Add `ContentVector` property to `Babble` record

- **Status**: PASS
- **Evidence**: [Babble.cs](prompt-babbler-service/src/Domain/Models/Babble.cs#L32-L33)
- **Findings**: Property added as `ReadOnlyMemory<float>? ContentVector` with `[JsonPropertyName("contentVector")]`. Nullable, non-required — existing babbles without vectors remain valid. Uses `System.Text.Json.Serialization` import. Matches plan specification exactly.

### Step 1.2: Create `BabbleSearchResult` domain model

- **Status**: PASS
- **Evidence**: [BabbleSearchResult.cs](prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs)
- **Findings**: Sealed record with positional `Babble` and `SimilarityScore` properties. File-scoped namespace `PromptBabbler.Domain.Models`. Implementation matches plan specification verbatim.

### Step 1.3: Create `IEmbeddingService` interface

- **Status**: PASS
- **Evidence**: [IEmbeddingService.cs](prompt-babbler-service/src/Domain/Interfaces/IEmbeddingService.cs)
- **Findings**: Single method `GenerateEmbeddingAsync(string, CancellationToken)` returning `Task<ReadOnlyMemory<float>>`. Returns primitive type — no MEAI dependency leaks into Domain. File-scoped namespace. Matches plan specification exactly.

### Step 1.4: Add `SearchByVectorAsync` to `IBabbleRepository`

- **Status**: PASS
- **Evidence**: [IBabbleRepository.cs](prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs#L29-L33)
- **Findings**: Signature: `Task<IReadOnlyList<BabbleSearchResult>> SearchByVectorAsync(string userId, ReadOnlyMemory<float> embedding, int topK = 10, CancellationToken cancellationToken = default)`. Accepts userId for partition scoping. Returns `IReadOnlyList<BabbleSearchResult>`. Matches plan specification exactly.

### Step 1.5: Add `SearchAsync` to `IBabbleService`

- **Status**: PASS
- **Evidence**: [IBabbleService.cs](prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs#L29-L33)
- **Findings**: Signature: `Task<IReadOnlyList<BabbleSearchResult>> SearchAsync(string userId, string query, int topK = 10, CancellationToken cancellationToken = default)`. Accepts raw query string — service determines routing. Returns consistent `IReadOnlyList<BabbleSearchResult>`. Matches plan specification exactly.

### Phase 1 Convention Check

| Convention | Status | Notes |
|---|---|---|
| File-scoped namespaces | PASS | All Domain files use file-scoped namespaces |
| `sealed` on records | PASS | `Babble`, `BabbleSearchResult` both sealed |
| `[JsonPropertyName]` | PASS | All Babble properties annotated |
| `required init` properties | PASS | All required Babble properties use `required init` |
| `System.Text.Json` only | PASS | No Newtonsoft.Json references in entire project |
| Domain zero infra deps | PASS | [PromptBabbler.Domain.csproj](prompt-babbler-service/src/Domain/PromptBabbler.Domain.csproj) has zero PackageReferences |
| CancellationToken | PASS | All async methods accept and forward CancellationToken |

## Phase 2: Infrastructure — Embedding and Repository

### Step 2.1: Create `EmbeddingService` implementation

- **Status**: PASS
- **Evidence**: [EmbeddingService.cs](prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs)
- **Findings**: Sealed class using primary constructor with `IEmbeddingGenerator<string, Embedding<float>>`. Calls `GenerateAsync` with single-element collection literal `[text]`. Returns `embeddings[0].Vector` as `ReadOnlyMemory<float>`. Implementation matches plan specification verbatim.

### Step 2.2: Add `SearchByVectorAsync` to `CosmosBabbleRepository`

- **Status**: PASS
- **Evidence**: [CosmosBabbleRepository.cs](prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs#L170-L218)
- **Findings**:
  - Uses parameterized `VectorDistance(c.contentVector, @embedding)` query with `TOP @topK`
  - `SUBSTRING(c.text, 0, 200)` for 200-char snippet projection at the Cosmos DB query level
  - Scoped to userId via `WHERE c.userId = @userId` and `PartitionKey`
  - Deserializes via `JsonElement` (System.Text.Json) — manually constructs `Babble` record with `init` properties
  - Handles nullable `tags` and `isPinned` with `TryGetProperty` guards
  - Returns `IReadOnlyList<BabbleSearchResult>` with similarity score
  - Matches plan specification exactly

### Step 2.3: Integrate embedding generation into `BabbleService` create/update flows

- **Status**: PASS
- **Evidence**: [BabbleService.cs](prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs#L51-L56) (CreateAsync), [BabbleService.cs](prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs#L63-L68) (UpdateAsync)
- **Findings**:
  - `IEmbeddingService` added to constructor as `_embeddingService` readonly private field
  - CreateAsync: generates embedding from `$"{babble.Title}\n{babble.Text}"`, sets via `babble with { ContentVector = vector }`
  - UpdateAsync: regenerates embedding from `$"{babble.Title}\n{babble.Text}"`, sets via `babble with { ContentVector = vector }`
  - CancellationToken forwarded to `GenerateEmbeddingAsync` in both paths
  - Matches plan specification

**Minor observation**: Plan details showed primary constructor syntax for BabbleService, but implementation uses traditional constructor with explicit `readonly` private `_camelCase` fields. This is the *better* choice — it follows the project convention ("Inject dependencies via constructor into `readonly` private `_camelCase` fields") and maintains consistency with the pre-existing code style. Not a deviation.

### Step 2.4: Add `SearchAsync` to `BabbleService`

- **Status**: PASS
- **Evidence**: [BabbleService.cs](prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs#L87-L109)
- **Findings**:
  - Routing condition: `words.Length <= 2 && query.Length < 15` → text search; else → vector search. Matches plan specification.
  - Text path: delegates to `_babbleRepository.GetByUserAsync(userId, search: query, ...)`, truncates text to 200 chars via `b.Text[..200]`, wraps in `BabbleSearchResult` with `SimilarityScore = 0.0`
  - Vector path: generates embedding via `_embeddingService.GenerateEmbeddingAsync`, delegates to `_babbleRepository.SearchByVectorAsync`
  - CancellationToken forwarded through both paths
  - Returns consistent `IReadOnlyList<BabbleSearchResult>` regardless of route
  - Matches plan specification exactly

### Step 2.5: Register `IEmbeddingService` as Singleton in DI

- **Status**: PASS
- **Evidence**: [DependencyInjection.cs](prompt-babbler-service/src/Infrastructure/DependencyInjection.cs#L53)
- **Findings**: `services.AddSingleton<IEmbeddingService, EmbeddingService>();` registered alongside other Cosmos-backed services. Singleton lifetime matches repository lifetime. Matches plan specification.

### Phase 2 Convention Check

| Convention | Status | Notes |
|---|---|---|
| `sealed` on all classes | PASS | `EmbeddingService`, `CosmosBabbleRepository`, `BabbleService` all sealed |
| File-scoped namespaces | PASS | All Infrastructure files |
| 4-space indentation | PASS | Consistent throughout |
| `_camelCase` private fields | PASS | `_container`, `_logger`, `_babbleRepository`, `_embeddingService` |
| `readonly` fields | PASS | All injected dependencies are `readonly` |
| CancellationToken chain | PASS | Forwarded through all async methods to repository/service calls |
| `System.Text.Json` only | PASS | `JsonElement` used in SearchByVectorAsync deserialization |
| No Newtonsoft.Json | PASS | Zero references in codebase |

## Summary

| Severity | Count | Details |
|---|---|---|
| Critical | 0 | — |
| Major | 0 | — |
| Minor | 0 | — |

**Overall assessment**: Both phases are fully implemented as specified in the plan. All 10 plan items (5 in Phase 1, 5 in Phase 2) match their specifications. C# conventions are followed consistently: sealed classes, file-scoped namespaces, 4-space indent, `[JsonPropertyName]`, `required init` properties, `_camelCase` readonly fields, CancellationToken forwarding, System.Text.Json only, and Domain layer has zero infrastructure dependencies.

## Recommended Next Validations

- [ ] Validate Phase 3 (API Endpoint): BabbleSearchResponse model, BabbleController.Search action, IEmbeddingGenerator registration in Program.cs
- [ ] Validate Phase 4 (Infrastructure — Aspire and Bicep): embedding model in AppHost, model-deployments.json, EnableNoSQLVectorSearch in main.bicep
- [ ] Validate Phase 6 (Unit Tests): EmbeddingService tests, BabbleService.SearchAsync tests, BabbleController.Search tests, frontend hook/component tests
- [ ] Run unit tests to confirm 208 backend + 112 frontend tests pass as claimed in changes log
