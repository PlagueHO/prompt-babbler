<!-- markdownlint-disable-file -->
# Release Changes: Babble Semantic Search

**Related Plan**: babble-search-semantic-plan.instructions.md
**Implementation Date**: 2026-04-26

## Summary

Add end-to-end semantic search: Azure OpenAI embeddings stored in Cosmos DB with vector search, exposed through a new backend API endpoint, and surfaced via a Command Palette (cmdk) live-search frontend component.

## Changes

### Added

* prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs - New sealed record for search results with Babble and SimilarityScore
* prompt-babbler-service/src/Domain/Interfaces/IEmbeddingService.cs - Embedding generation abstraction interface
* prompt-babbler-app/src/components/ui/command.tsx - shadcn/ui Command component (cmdk wrapper)
* prompt-babbler-app/src/hooks/useSemanticSearch.ts - Debounced semantic search hook with 300ms delay, 2-char minimum
* prompt-babbler-app/src/components/search/SearchCommand.tsx - Command Palette dialog with Ctrl+K shortcut, result rendering
* prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/EmbeddingServiceTests.cs - EmbeddingService unit tests (vector return, cancellation token)
* prompt-babbler-app/tests/hooks/useSemanticSearch.test.ts - Hook tests (short query, valid query, error handling)
* prompt-babbler-app/tests/components/search/SearchCommand.test.tsx - Component tests (Ctrl+K shortcut, empty state)

### Modified

* prompt-babbler-service/src/Domain/Models/Babble.cs - Added `ContentVector` property (ReadOnlyMemory<float>?)
* prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs - Added `SearchByVectorAsync` method
* prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs - Added `SearchAsync` method
* prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs - Added SearchByVectorAsync with VectorDistance Cosmos DB query and SUBSTRING snippet truncation
* prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs - Added IEmbeddingService to constructor, embedding generation in create/update, implemented SearchAsync with text/vector routing
* prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs - New EmbeddingService wrapping MEAI IEmbeddingGenerator
* prompt-babbler-service/src/Infrastructure/DependencyInjection.cs - Registered IEmbeddingService as Singleton
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs - Added Search endpoint with query validation and user-scoped search
* prompt-babbler-service/src/Api/Program.cs - Added IEmbeddingGenerator registration
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs - Added text-embedding-3-small model deployment wired to API service
* infra/model-deployments.json - Added embedding model entry with GlobalStandard SKU
* infra/main.bicep - Added EnableNoSQLVectorSearch capability, wired cosmos-babbles-vector-container module reference for vector search (see issue #107 for AVM inline migration)
* prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/BabbleServiceTests.cs - Added mock IEmbeddingService to test constructor
* prompt-babbler-app/src/types/index.ts - Added BabbleSearchResultItem and BabbleSearchResponse interfaces
* prompt-babbler-app/src/services/api-client.ts - Added searchBabbles function using fetchJson helper
* prompt-babbler-app/src/components/layout/Header.tsx - Added search trigger button with ⌘K hint and SearchCommand component
* prompt-babbler-app/src/components/ui/dialog.tsx - Updated by shadcn CLI during command component install
* prompt-babbler-app/package.json - Added cmdk dependency
* prompt-babbler-app/pnpm-lock.yaml - Updated lockfile
* prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/BabbleServiceTests.cs - Added 6 SearchAsync/embedding tests, fixed 2 existing tests for embedding compatibility
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs - Added 6 Search endpoint tests (validation, happy path)

### Removed

## Additional or Deviating Changes

## Release Summary

Total files affected: 22 (10 added, 12 modified, 0 removed)

**Added files:**
- prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs — search result record
- prompt-babbler-service/src/Domain/Interfaces/IEmbeddingService.cs — embedding abstraction
- prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs — MEAI embedding wrapper
- prompt-babbler-service/src/Api/Models/Responses/BabbleSearchResponse.cs — API response model
- prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/EmbeddingServiceTests.cs — embedding unit tests
- prompt-babbler-app/src/components/ui/command.tsx — shadcn/ui Command component
- prompt-babbler-app/src/hooks/useSemanticSearch.ts — debounced search hook
- prompt-babbler-app/src/components/search/SearchCommand.tsx — Command Palette component
- prompt-babbler-app/tests/hooks/useSemanticSearch.test.ts — hook tests
- prompt-babbler-app/tests/components/search/SearchCommand.test.tsx — component tests

**Modified files:**
- prompt-babbler-service/src/Domain/Models/Babble.cs — added ContentVector property
- prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs — added SearchByVectorAsync
- prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs — added SearchAsync
- prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs — vector search query
- prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs — embedding generation + search routing
- prompt-babbler-service/src/Infrastructure/DependencyInjection.cs — IEmbeddingService DI
- prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Search endpoint
- prompt-babbler-service/src/Api/Program.cs — IEmbeddingGenerator registration
- prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs — embedding model deployment
- infra/model-deployments.json — text-embedding-3-small model
- infra/main.bicep — EnableNoSQLVectorSearch + vector container module
- prompt-babbler-app/src/types/index.ts — search result types
- prompt-babbler-app/src/services/api-client.ts — searchBabbles function
- prompt-babbler-app/src/components/layout/Header.tsx — search trigger button

**Validation:** 208 backend unit tests pass, 112 frontend tests pass, dotnet format clean, ESLint clean, Bicep builds, Vite builds.
