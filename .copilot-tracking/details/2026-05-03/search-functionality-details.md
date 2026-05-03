<!-- markdownlint-disable-file -->
# Implementation Details: Semantic Search Functionality

## Context Reference

Sources: .copilot-tracking/research/2026-05-03/search-functionality-research.md, .copilot-tracking/research/subagents/2026-05-03/source-file-verification.md

## Implementation Phase 1: Domain Layer Changes

<!-- parallelizable: true -->

### Step 1.1: Add `ContentVector` property to `Babble` model

Add a nullable `float[]?` property to the `Babble` record for storing embedding vectors. Use `[JsonPropertyName("contentVector")]` to match the Cosmos DB vector index path. Use `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` to exclude from serialization when null.

Files:
* prompt-babbler-service/src/Domain/Models/Babble.cs - Add `ContentVector` property after existing properties (after line 30)

Discrepancy references:
* None — directly implements user requirement

Success criteria:
* `Babble` record has `ContentVector` as `float[]?` with `init` setter
* Property has `[JsonPropertyName("contentVector")]` attribute
* Property has `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` to avoid serializing 1536 floats

Context references:
* .copilot-tracking/research/2026-05-03/search-functionality-research.md (Lines 256-266) - Implementation table
* infra/cosmos-babbles-vector-container.bicep - Vector path is `/contentVector`, Float32, 1536 dims

Dependencies:
* None

### Step 1.2: Add `SearchByVectorAsync` method to `IBabbleRepository`

Add a method signature for vector-based search to the repository interface.

Files:
* prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs - Add method after existing methods (after line 25)

Discrepancy references:
* None — derived from architecture pattern

Success criteria:
* Method signature: `Task<IReadOnlyList<BabbleSearchResult>> SearchByVectorAsync(string userId, ReadOnlyMemory<float> vector, int topN, CancellationToken cancellationToken = default)`
* Returns `IReadOnlyList<BabbleSearchResult>` (existing model)

Context references:
* prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs - Existing result type
* prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs (Lines 1-27) - Current interface

Dependencies:
* Step 1.1 (Babble model has ContentVector for deserialization)

### Step 1.3: Add `SearchAsync` method to `IBabbleService`

Add a search method signature to the service interface that accepts a text query (not a vector).

Files:
* prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs - Add method after existing methods (after line 25)

Discrepancy references:
* None — derived from architecture pattern

Success criteria:
* Method signature: `Task<IReadOnlyList<BabbleSearchResult>> SearchAsync(string userId, string query, int topN, CancellationToken cancellationToken = default)`
* Takes text query (service handles embedding)

Context references:
* prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs (Lines 1-27) - Current interface
* prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs - Return type

Dependencies:
* None (interface only)

## Implementation Phase 2: Infrastructure and DI Registration

<!-- parallelizable: false -->

### Step 2.1: Register `IEmbeddingService` in `DependencyInjection.cs`

Add the `IEmbeddingService` → `EmbeddingService` singleton registration alongside other service registrations.

Files:
* prompt-babbler-service/src/Infrastructure/DependencyInjection.cs - Add registration after line 59 (near existing service registrations)

Discrepancy references:
* None — directly implements user requirement

Success criteria:
* `services.AddSingleton<IEmbeddingService, EmbeddingService>()` added
* Registration is near other service registrations for consistency

Context references:
* prompt-babbler-service/src/Infrastructure/DependencyInjection.cs (Lines 55-65) - Existing registrations
* prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs - Implementation class

Dependencies:
* None (EmbeddingService already exists)

### Step 2.2: Register `IEmbeddingGenerator` in `Program.cs`

Register the `IEmbeddingGenerator<string, Embedding<float>>` singleton using the existing `AzureOpenAIClient` singleton. Place inside the `if (isAiConfigured)` block near the existing chat client registration.

Files:
* prompt-babbler-service/src/Api/Program.cs - Add after existing AI client registrations (lines 115-117 area, inside `if (isAiConfigured)` block)

Discrepancy references:
* None — directly implements user requirement

Success criteria:
* `IEmbeddingGenerator<string, Embedding<float>>` registered as singleton
* Uses `openAiClient.GetEmbeddingClient("embedding").AsIEmbeddingGenerator()` pattern
* Only registered when AI is configured (`isAiConfigured` guard)

Context references:
* prompt-babbler-service/src/Api/Program.cs (Lines 95-120) - Existing AI registration pattern
* .copilot-tracking/research/2026-05-03/search-functionality-research.md (Lines 115-120) - Registration pattern example

Dependencies:
* Step 2.3 (AppHost needs embedding model deployment for the client to connect to)

### Step 2.3: Add embedding model deployment to `AppHost.cs`

Add an embedding model deployment (`text-embedding-3-small`) to the Aspire AppHost configuration and pass it as a reference to the API project.

Files:
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs - Add after existing model deployment (after line 22), and add `.WithReference()` to API project

Discrepancy references:
* DR-01: AppHost currently has no embedding deployment — this adds one

Success criteria:
* `foundryProject.AddModelDeployment("embedding", ...)` added with `text-embedding-3-small` model
* Embedding deployment referenced by API project via `.WithReference(embeddingDeployment)`
* Configuration values use `builder.Configuration["MicrosoftFoundry:embeddingModelName"]` with fallback
* SKU: Standard, Capacity: 120

Context references:
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs (Lines 15-25) - Existing deployment pattern
* .copilot-tracking/research/2026-05-03/search-functionality-research.md (Lines 166-175) - Configuration example

Dependencies:
* None (Aspire infrastructure)

### Step 2.4: Implement embedding generation in `BabbleService`

Inject `IEmbeddingService` into `BabbleService`, generate embeddings in `CreateAsync` and `UpdateAsync`, handle failures gracefully.

Files:
* prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs - Add `IEmbeddingService` field and constructor parameter, modify `CreateAsync` (line 44) and `UpdateAsync` (line 49)

Discrepancy references:
* None — directly implements user requirement

Success criteria:
* `IEmbeddingService` injected via constructor, stored in `_embeddingService` field
* `CreateAsync` generates embedding from `babble.Text`, creates `babble with { ContentVector = vector.ToArray() }`
* `UpdateAsync` regenerates embedding when text changes
* Both methods wrap embedding call in try-catch; on failure, log warning and save without vector
* `SearchAsync` implemented: calls `_embeddingService.GenerateEmbeddingAsync(query)`, then `_babbleRepository.SearchByVectorAsync(userId, vector, topN)`

Context references:
* prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs (Lines 41-58) - Current Create/Update methods
* prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs - GenerateEmbeddingAsync signature
* .copilot-tracking/research/2026-05-03/search-functionality-research.md (Lines 195-210) - Complete example

Dependencies:
* Step 1.1 (Babble has ContentVector)
* Step 1.3 (IBabbleService has SearchAsync)
* Step 2.1 (IEmbeddingService registered)

### Step 2.5: Implement `SearchByVectorAsync` in `CosmosBabbleRepository`

Add the vector search query method using Cosmos DB `VectorDistance` function.

Files:
* prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs - Add new method after existing query methods (after line 82)

Discrepancy references:
* None — directly implements derived objective

Success criteria:
* Method matches `IBabbleRepository.SearchByVectorAsync` signature
* Uses Cosmos SQL: `SELECT TOP @topN ... VectorDistance(c.contentVector, @embedding) AS SimilarityScore FROM c WHERE c.userId = @userId ORDER BY VectorDistance(c.contentVector, @embedding)`
* Returns `IReadOnlyList<BabbleSearchResult>` with deserialized Babble and similarity score
* Uses parameterized query (no SQL injection)
* Handles empty results gracefully

Context references:
* prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs (Lines 60-82) - Existing query pattern with QueryDefinition
* .copilot-tracking/research/2026-05-03/search-functionality-research.md (Lines 125-132) - Vector query SQL
* infra/cosmos-babbles-vector-container.bicep - Vector index: `/contentVector`, quantizedFlat, Cosine

Dependencies:
* Step 1.1 (Babble has ContentVector for deserialization)
* Step 1.2 (IBabbleRepository has SearchByVectorAsync)

## Implementation Phase 3: API Layer

<!-- parallelizable: false -->

### Step 3.1: Add search endpoint to `BabbleController`

Add `[HttpGet("search")]` endpoint that accepts `query` and `topK` parameters, calls `IBabbleService.SearchAsync`, and returns `BabbleSearchResponse`.

Files:
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs - Add new endpoint method after existing GET endpoints

Discrepancy references:
* None — directly implements user requirement

Success criteria:
* Endpoint: `[HttpGet("search")]` on the existing `BabbleController`
* Parameters: `[FromQuery] string query`, `[FromQuery] int topK = 10`
* Has `[Authorize]` and `[RequiredScope("access_as_user")]` (inherited from controller)
* Validates: query is not null/empty (returns BadRequest), topK between 1-50 (returns BadRequest)
* Extracts userId from claims (same pattern as existing endpoints)
* Calls `_babbleService.SearchAsync(userId, query, topK, cancellationToken)`
* Maps results to `BabbleSearchResponse` using existing DTO
* Returns `Ok(response)`

Context references:
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs - Existing endpoint patterns
* prompt-babbler-service/src/Api/Models/Responses/BabbleSearchResponse.cs - Existing response DTO
* prompt-babbler-app/src/services/api-client.ts (Line 355) - Frontend calls `GET /api/babbles/search?query=X&topK=N`

Dependencies:
* Step 2.4 (BabbleService.SearchAsync implemented)

## Implementation Phase 4: Frontend Mounting

<!-- parallelizable: true -->

### Step 4.1: Mount `SearchCommand` in `App.tsx`

Import and render the `SearchCommand` component inside the `<BrowserRouter>` (it uses `useNavigate`), outside `<Routes>`.

Files:
* prompt-babbler-app/src/App.tsx - Add import and render `<SearchCommand />` between lines 44-53 (inside BrowserRouter, after PageLayout or alongside ThemedToaster)

Discrepancy references:
* None — directly implements user requirement

Success criteria:
* `SearchCommand` imported from `@/components/search/SearchCommand`
* `<SearchCommand />` rendered inside `<BrowserRouter>` but outside `<Routes>`
* Named export import (not default)
* `Ctrl+K` opens the search dialog after mounting

Context references:
* prompt-babbler-app/src/App.tsx (Lines 40-55) - BrowserRouter structure
* prompt-babbler-app/src/components/search/SearchCommand.tsx - Component location
* .copilot-tracking/research/2026-05-03/search-functionality-research.md (Lines 150-158) - Mount point example

Dependencies:
* None (component already complete)

## Implementation Phase 5: Unit Tests

<!-- parallelizable: false -->

### Step 5.1: Add unit tests for `BabbleService` embedding generation

Create tests verifying embedding generation on create/update and graceful fallback on failure.

Files:
* prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/BabbleServiceTests.cs - Add new test methods or create file if not exists

Discrepancy references:
* None — implements success criteria

Success criteria:
* Test: `CreateAsync_WithText_GeneratesEmbeddingAndStoresVector`
* Test: `CreateAsync_EmbeddingServiceFails_SavesBabbleWithoutVector`
* Test: `UpdateAsync_WithTextChange_RegeneratesEmbedding`
* Test: `SearchAsync_WithQuery_ReturnsRankedResults`
* Uses NSubstitute for `IEmbeddingService` and `IBabbleRepository` mocks
* Uses FluentAssertions for verification
* Has `[TestCategory("Unit")]` attribute on class

Context references:
* prompt-babbler-service/tests/unit/ - Existing test structure
* .github/copilot-instructions.md - Test naming: `MethodName_Condition_ExpectedResult`

Dependencies:
* Phase 2 (BabbleService implementation complete)

### Step 5.2: Add unit tests for search endpoint in `BabbleController`

Create tests verifying the search endpoint validation, authorization, and response mapping.

Files:
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs - Add new test methods

Discrepancy references:
* None — implements success criteria

Success criteria:
* Test: `Search_ValidQuery_ReturnsOkWithResults`
* Test: `Search_EmptyQuery_ReturnsBadRequest`
* Test: `Search_TopKOutOfRange_ReturnsBadRequest`
* Uses NSubstitute for `IBabbleService` mock
* Uses FluentAssertions
* Has `[TestCategory("Unit")]` attribute on class

Context references:
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/ - Existing controller test pattern

Dependencies:
* Phase 3 (Controller endpoint complete)

### Step 5.3: Validate existing tests still pass

Run full unit test suite to verify no regressions.

Validation commands:
* `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit --configuration Release` - All backend unit tests
* `cd prompt-babbler-app && pnpm test` - All frontend tests

Dependencies:
* Steps 5.1, 5.2 (new tests written)

## Implementation Phase 6: Validation

<!-- parallelizable: false -->

### Step 6.1: Run full project validation

Execute all validation commands for the project:
* `dotnet format PromptBabbler.slnx --verify-no-changes --severity error` - Code formatting
* `dotnet build PromptBabbler.slnx` - Full build
* `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit` - Unit tests
* `cd prompt-babbler-app && pnpm lint` - Frontend lint
* `cd prompt-babbler-app && pnpm test` - Frontend tests
* `cd prompt-babbler-app && pnpm run build` - Frontend build

### Step 6.2: Fix minor validation issues

Iterate on lint errors, build warnings, and test failures. Apply fixes directly when corrections are straightforward and isolated.

### Step 6.3: Report blocking issues

When validation failures require changes beyond minor fixes:
* Document the issues and affected files.
* Provide the user with next steps.
* Recommend additional research and planning rather than inline fixes.
* Avoid large-scale refactoring within this phase.

## Dependencies

* .NET 10 SDK

## Success Criteria

* All 6 phases complete without blocking errors
* All unit tests pass (existing + new)
* `dotnet format` reports no violations
* Frontend lint/build/test pass
* Search endpoint returns ranked results for valid queries
