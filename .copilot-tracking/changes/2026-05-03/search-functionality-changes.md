<!-- markdownlint-disable-file -->
# Release Changes: Semantic Search Functionality

**Related Plan**: search-functionality-plan.instructions.md
**Implementation Date**: 2026-05-03

## Summary

Wire together the partially-implemented semantic search feature by adding embedding generation on babble create/update, a vector search endpoint, Aspire embedding model deployment, and mounting the existing SearchCommand component in the React app.

## Changes

### Added

* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs - Added `embeddingDeployment` (text-embedding-3-small) with `.WithReference()` and `.WaitFor()` on API service

### Modified

* prompt-babbler-service/src/Domain/Models/Babble.cs - Added `ContentVector` property (`float[]?`) with `[JsonPropertyName("contentVector")]` and `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`
* prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs - Added `SearchByVectorAsync` method signature
* prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs - Added `SearchAsync` method signature
* prompt-babbler-service/src/Infrastructure/DependencyInjection.cs - Registered `IEmbeddingService` → `EmbeddingService` singleton
* prompt-babbler-service/src/Api/Program.cs - Registered `IEmbeddingGenerator<string, Embedding<float>>` from Azure OpenAI embedding client
* prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs - Added `IEmbeddingService` injection, embedding generation in CreateAsync/UpdateAsync (with graceful fallback), and `SearchAsync` implementation
* prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs - Implemented `SearchByVectorAsync` using Cosmos DB `VectorDistance` SQL
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs - Added `[HttpGet("search")]` endpoint with input validation and response mapping
* prompt-babbler-app/src/App.tsx - Mounted `<SearchCommand />` inside BrowserRouter
* prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/BabbleServiceTests.cs - Fixed existing tests for new embedding flow, added 4 new embedding/search tests
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs - Added 5 new search endpoint tests

### Removed

* None

## Additional or Deviating Changes

* Fixed existing `CreateAsync_DelegatesToRepository` and `UpdateAsync_ExistingBabble_DelegatesToRepository` tests that were broken by embedding injection (changed mock setup from exact reference match to `Arg.Any<Babble>()`)
  * Reason: Phase 2 modified BabbleService to create new record instances with `ContentVector`, breaking reference equality

## Release Summary

Total files affected: 11 (0 created, 11 modified, 0 removed)

**Domain Layer:**
- `Babble.cs` — nullable `ContentVector` property for 1536-dim embedding vectors
- `IBabbleRepository.cs` — `SearchByVectorAsync` interface method
- `IBabbleService.cs` — `SearchAsync` interface method

**Infrastructure:**
- `DependencyInjection.cs` — `IEmbeddingService` DI registration
- `BabbleService.cs` — Embedding generation on create/update with try-catch fallback; search delegation
- `CosmosBabbleRepository.cs` — Vector search via `VectorDistance` Cosmos SQL with parameterized query
- `EmbeddingService.cs` — Already existed (no changes needed)

**API Layer:**
- `Program.cs` — `IEmbeddingGenerator` registered from Azure OpenAI client
- `BabbleController.cs` — `GET /api/babbles/search?query=X&topK=N` endpoint

**Orchestration:**
- `AppHost.cs` — `text-embedding-3-small` model deployment added to Aspire

**Frontend:**
- `App.tsx` — `SearchCommand` mounted (Ctrl+K search dialog)

**Tests:**
- 9 new unit tests added (4 service, 5 controller)
- 2 existing tests fixed for compatibility
- All 228 backend unit tests pass
- All 126 frontend tests pass

**Validation:**
- `dotnet format` — no violations
- `dotnet build` — zero errors
- `pnpm lint` — no errors
- `pnpm build` — succeeds
