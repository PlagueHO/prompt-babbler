<!-- markdownlint-disable-file -->
# Review Log: Babble Semantic Search

| Field | Value |
|---|---|
| Review Date | 2026-04-26 |
| Plan | .copilot-tracking/plans/2026-04-26/babble-search-semantic-plan.instructions.md |
| Changes Log | .copilot-tracking/changes/2026-04-26/babble-search-semantic-changes.md |
| Research | .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md |
| Status | ⚠️ NEEDS REWORK |

## Severity Summary

| Severity | Count |
|---|---|
| Critical | 1 |
| Major | 3 |
| Minor | 5 |

## Phase 1: Artifact Discovery

- Plan: Located at `.copilot-tracking/plans/2026-04-26/babble-search-semantic-plan.instructions.md` (7 phases, all marked complete)
- Changes: Located at `.copilot-tracking/changes/2026-04-26/babble-search-semantic-changes.md` (10 added, 12+ modified)
- Research: Located at `.copilot-tracking/research/2026-04-26/babble-search-semantic-research.md`

## Phase 2: RPI Validation

RPI Validators spawned for three parallel runs covering phases 1-2, 3-4, and 5-6.

### CRITICAL FINDING: Changes Log Does Not Match Codebase

**The RPI Validators reported PASS for all phases, but they validated the changes log contents rather than the actual codebase state.** Direct file inspection reveals that **all claimed modifications to existing files were NOT implemented.** Only the new files were created.

#### Files that exist (new files — CREATED correctly)

| File | Status |
|---|---|
| `prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs` | ✅ Created, compiles |
| `prompt-babbler-service/src/Domain/Interfaces/IEmbeddingService.cs` | ✅ Created, compiles |
| `prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs` | ✅ Created, compiles |
| `prompt-babbler-service/src/Api/Models/Responses/BabbleSearchResponse.cs` | ✅ Created, compiles |
| `prompt-babbler-app/src/components/search/SearchCommand.tsx` | ✅ Created |
| `prompt-babbler-app/src/hooks/useSemanticSearch.ts` | ✅ Created |
| `prompt-babbler-app/src/components/ui/command.tsx` | ✅ Created |
| `infra/cosmos-babbles-vector-container.bicep` | ✅ Created, correct vector policies |
| `prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/EmbeddingServiceTests.cs` | ✅ Created |
| `prompt-babbler-app/tests/hooks/useSemanticSearch.test.ts` | ✅ Created |
| `prompt-babbler-app/tests/components/search/SearchCommand.test.tsx` | ✅ Created |

#### Files that were NOT modified (changes log claims they were)

| File | Claimed Change | Actual State |
|---|---|---|
| `prompt-babbler-service/src/Domain/Models/Babble.cs` | Added `ContentVector` property | ❌ No `ContentVector` property |
| `prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs` | Added `SearchByVectorAsync` | ❌ No `SearchByVectorAsync` method |
| `prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs` | Added `SearchAsync` | ❌ No `SearchAsync` method |
| `prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs` | Added vector search query | ❌ No `VectorDistance` query |
| `prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs` | Added IEmbeddingService, SearchAsync, embedding in create/update | ❌ No embedding logic, no SearchAsync |
| `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs` | Registered IEmbeddingService | ❌ No embedding registration |
| `prompt-babbler-service/src/Api/Controllers/BabbleController.cs` | Added Search endpoint | ❌ No Search action (only existing `search` param on GET) |
| `prompt-babbler-service/src/Api/Program.cs` | Added IEmbeddingGenerator registration | ❌ No IEmbeddingGenerator |
| `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs` | Added embedding model deployment | ❌ No embedding deployment |
| `infra/model-deployments.json` | Added text-embedding-3-small model | ❌ Only gpt-5.3-chat |
| `infra/main.bicep` | Added EnableNoSQLVectorSearch + vector container module | ❌ Neither present |
| `prompt-babbler-app/src/types/index.ts` | Added BabbleSearchResultItem/BabbleSearchResponse | ❌ Not present |
| `prompt-babbler-app/src/services/api-client.ts` | Added searchBabbles function | ❌ Not present |
| `prompt-babbler-app/src/components/layout/Header.tsx` | Added search trigger button | ❌ Not present |
| `prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/BabbleServiceTests.cs` | Added 6 search/embedding tests | ❌ Not verified (existing tests still pass) |
| `prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs` | Added 6 search endpoint tests | ❌ Not verified |

### RPI Validation Sub-Reports

- `.copilot-tracking/reviews/rpi/2026-04-26/babble-search-semantic-plan-001-validation.md` — Phases 1 & 2 (Reported PASS, but based on changes log not actual code)
- `.copilot-tracking/reviews/rpi/2026-04-26/babble-search-semantic-plan-002-validation.md` — Phases 3 & 4 (Reported PASS, but based on changes log not actual code)
- `.copilot-tracking/reviews/rpi/2026-04-26/babble-search-semantic-plan-003-validation.md` — Phases 5 & 6 (Reported PARTIAL with 1 Major, 3 Minor)

## Phase 3: Quality Validation

### Validation Commands

| Command | Status |
|---|---|
| `dotnet format --verify-no-changes` | ❌ FAIL — formatting differences in 11 files |
| `dotnet build --configuration Release` | ✅ PASS |
| `dotnet test --filter TestCategory=Unit` | ✅ PASS — 208 tests (existing count, no new search tests) |
| `pnpm lint` | ✅ PASS |
| `pnpm test -- --run` | ✅ PASS — 112 tests |
| `pnpm run build` | ✅ PASS |

**Note:** Build and tests pass because the new files are self-contained and the existing files were never modified. The new domain models/interfaces are not wired into the codebase.

### Implementation Quality Findings

Quality validation report: `.copilot-tracking/reviews/2026-04-26/babble-search-semantic-plan-quality-validation.md`

These findings were identified by the Implementation Validator but are partially moot given the incomplete modifications. They remain relevant for when the modifications are completed:

| ID | Severity | Summary |
|---|---|---|
| IV-001 | Critical | Bicep duplicate `babbles` container — AVM creates without vector policy, then vector module fails (immutable policy) |
| IV-002 | Major | `useSemanticSearch` race condition — AbortController signal never passed to fetch |
| IV-003 | Major | Embedding failure blocks babble CRUD — no try/catch around IEmbeddingGenerator call |
| IV-004 | Major | Text search fallback ignores `topK` — uses default `pageSize=20` |
| IV-005 | Minor | Text search loads `contentVector` (~6KB/doc) unnecessarily |
| IV-006 | Minor | `SearchCommand` ignores `error` state — silent failure |
| IV-007 | Minor | Dialog doesn't reset query on close — stale results persist |
| IV-008 | Minor | `Header.tsx` uses synthetic `dispatchEvent` for dialog trigger |
| IV-009 | Minor | No boundary test for text/vector routing at 15 chars with 2 words |

## Phase 4: Review Completion

### Overall Status: ⚠️ NEEDS REWORK

The implementation is **fundamentally incomplete**. While 11 new files were correctly created (domain models, interfaces, services, frontend components, and tests), **none of the 14+ claimed modifications to existing files were actually applied**. The changes log inaccurately reports these modifications as complete.

### What was completed

- New domain models: `BabbleSearchResult`, `IEmbeddingService`
- New infrastructure: `EmbeddingService`
- New API response model: `BabbleSearchResponse`
- New Bicep module: `cosmos-babbles-vector-container.bicep` (correctly configured with vector policies)
- New frontend components: `SearchCommand`, `useSemanticSearch`, `command.tsx` (shadcn)
- New test files: `EmbeddingServiceTests`, `useSemanticSearch.test`, `SearchCommand.test`

### What is missing (must be completed)

**Backend modifications (all required to wire the feature together):**
1. `Babble.cs` — Add `ContentVector` property (`ReadOnlyMemory<float>?`)
2. `IBabbleRepository.cs` — Add `SearchByVectorAsync` method
3. `IBabbleService.cs` — Add `SearchAsync` method
4. `CosmosBabbleRepository.cs` — Implement `SearchByVectorAsync` with `VectorDistance` query
5. `BabbleService.cs` — Add `IEmbeddingService` dependency, embedding in create/update, `SearchAsync` implementation
6. `DependencyInjection.cs` — Register `IEmbeddingService` as Singleton
7. `BabbleController.cs` — Add Search endpoint
8. `Program.cs` — Register `IEmbeddingGenerator`
9. `BabbleServiceTests.cs` — Add search/embedding tests
10. `BabbleControllerTests.cs` — Add search endpoint tests

**Infrastructure modifications:**
11. `AppHost.cs` — Add embedding model deployment
12. `model-deployments.json` — Add text-embedding-3-small entry
13. `main.bicep` — Add `EnableNoSQLVectorSearch` capability, remove `babbles` from AVM containers, add vector container module reference

**Frontend modifications:**
14. `types/index.ts` — Add `BabbleSearchResultItem` and `BabbleSearchResponse` interfaces
15. `api-client.ts` — Add `searchBabbles` function
16. `Header.tsx` — Add search trigger button with `SearchCommand`

### Follow-Up Work

**From deferred scope:** None identified in the plan.

**Discovered during review:**
1. Fix `dotnet format` issues across 11 files
2. When implementing IV-001 fix: remove `babbles` from AVM `containers` array before adding vector module reference
3. When implementing BabbleService embedding: decide on IV-003 (try/catch vs intentional failure)
4. When implementing SearchAsync: pass `topK` to text search path (IV-004)
5. Thread `AbortSignal` through `searchBabbles` → `fetchJson` → `fetch` (IV-002)
6. Add missing frontend test cases for `SearchCommand` (Escape close, result rendering, navigation, loading)
7. Add missing `useSemanticSearch` test cases (debounce timing, AbortController cancellation)
8. Add boundary test for text/vector routing threshold (IV-009)
