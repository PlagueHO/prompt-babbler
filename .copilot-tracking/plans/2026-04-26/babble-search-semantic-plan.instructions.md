---
applyTo: '.copilot-tracking/changes/2026-04-26/babble-search-semantic-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: Babble Semantic Search

## Overview

Add end-to-end semantic search: Azure OpenAI embeddings stored in Cosmos DB with vector search, exposed through a new backend API endpoint, and surfaced via a Command Palette (cmdk) live-search frontend component.

## Objectives

### User Requirements

- Add a `contentVector` embedding property to the Babble domain model — Source: task research, Task Implementation Requests
- Deploy an Azure OpenAI `text-embedding-3-small` embedding model — Source: task research, Task Implementation Requests
- Configure Cosmos DB `babbles` container with vector embedding policy and vector index — Source: task research, Task Implementation Requests
- Generate embeddings via `IEmbeddingGenerator<string, Embedding<float>>` (MEAI) when babbles are created/updated — Source: task research, Task Implementation Requests
- Create a `GET /api/babbles/search?q=...` semantic search API endpoint — Source: task research, Task Implementation Requests
- Build a Command Palette (cmdk) live-search component in the React frontend — Source: task research, Task Implementation Requests
- Support single-user vs multi-user search modes — Source: task research, Task Implementation Requests
- Route queries by complexity: text search for 1-2 word queries, vector search for 3+ words — Source: task research, Task Implementation Requests
- Use live debounced search (300ms) with minimum 2-character threshold — Source: task research, Task Implementation Requests

### Derived Objectives

- Create `IEmbeddingService` interface in Domain and implementation in Infrastructure — Derived from: Clean Architecture pattern; isolates embedding logic from service layer
- Create `BabbleSearchResult` domain model and `BabbleSearchResponse` API response model — Derived from: search results require similarity score not present on Babble entity
- Add `SearchByVectorAsync` to `IBabbleRepository` — Derived from: repository pattern requires vector search abstraction
- Register `IEmbeddingGenerator<string, Embedding<float>>` in DI container — Derived from: follows existing `IChatClient` registration pattern
- Add `EnableNoSQLVectorSearch` capability to Cosmos DB Bicep — Derived from: required by Azure for vector search feature activation
- Create `useSemanticSearch` React hook — Derived from: follows existing manual hook pattern (no React Query in codebase)
- Return 200-char snippets + relevance score from search API — Derived from: 95% bandwidth reduction for live search UX

## Context Summary

### Project Files

- prompt-babbler-service/src/Domain/Models/Babble.cs — Sealed record, 8 properties, no vector field
- prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs — `GetByUserAsync` with title-only substring search
- prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs — Mirrors repository, thin pass-through
- prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs — Database `prompt-babbler`, container `babbles`, partition `/userId`
- prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs — Thin service wrapping repository with cascade delete
- prompt-babbler-service/src/Infrastructure/DependencyInjection.cs — Singleton Cosmos repos, no embedding registration
- prompt-babbler-service/src/Infrastructure/Services/AzureOpenAiPromptGenerationService.cs — Uses `IChatClient` from MEAI; pattern for `IEmbeddingGenerator`
- prompt-babbler-service/src/Api/Controllers/BabbleController.cs — REST at `/api/babbles`, CRUD + prompt gen, user ID via `GetUserIdOrAnonymous()`
- prompt-babbler-service/src/Api/Program.cs — `AzureOpenAIClient` singleton, `IChatClient` from chat deployment
- prompt-babbler-service/src/Api/Models/Requests/CreateBabbleRequest.cs — Title 1-200 chars, Text 1-50000 chars
- prompt-babbler-service/src/Api/Models/Responses/BabbleResponse.cs — No embedding exposed
- prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs — AI Foundry with `chat` model, Cosmos DB with 4 containers
- prompt-babbler-service/Directory.Packages.props — `Microsoft.Azure.Cosmos` 3.58.0, `Azure.AI.OpenAI` 2.1.0, `Microsoft.Extensions.AI.OpenAI` 10.5.0
- infra/model-deployments.json — Single model: gpt-5.3-chat
- infra/main.bicep — AVM `document-db/database-account:0.19.0`, serverless, no vector indexing
- prompt-babbler-app/src/components/babbles/BabbleListSection.tsx — Inline search with 300ms debounce
- prompt-babbler-app/src/services/api-client.ts — Central `fetchJson` helper
- prompt-babbler-app/src/hooks/useBabbles.ts — Manual useState/useCallback/useEffect pattern
- prompt-babbler-app/src/components/layout/Header.tsx — Fixed header with nav + UserMenu
- prompt-babbler-app/package.json — React 19, Tailwind CSS 4, shadcn 4.0

### References

- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md — Primary research document
- .copilot-tracking/research/subagents/2026-04-26/cosmos-serverless-vector-support-research.md — Cosmos DB serverless vector search validation
- .copilot-tracking/research/subagents/2026-04-26/codebase-structure-research.md — Codebase analysis
- .copilot-tracking/research/subagents/2026-04-26/frontend-search-research.md — Frontend component research
- .copilot-tracking/research/subagents/2026-04-26/search-ux-thresholds-research.md — Search UX threshold research
- .copilot-tracking/research/subagents/2026-04-26/service-deep-analysis-research.md — Service layer analysis

### Standards References

- prompt-babbler-service/Directory.Build.props — net10.0, nullable enabled, TreatWarningsAsErrors
- prompt-babbler-service/Directory.Packages.props — Centralized package management

## Implementation Checklist

### [x] Implementation Phase 1: Domain Model and Interfaces

<!-- parallelizable: false -->

- [x] Step 1.1: Add `ContentVector` property to `Babble` record
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 10-31)
- [x] Step 1.2: Create `BabbleSearchResult` domain model
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 33-50)
- [x] Step 1.3: Create `IEmbeddingService` interface
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 52-71)
- [x] Step 1.4: Add `SearchByVectorAsync` to `IBabbleRepository`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 73-92)
- [x] Step 1.5: Add `SearchAsync` to `IBabbleService`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 94-111)

### [x] Implementation Phase 2: Infrastructure — Embedding and Repository

<!-- parallelizable: false -->

- [x] Step 2.1: Create `EmbeddingService` implementation
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 115-148)
- [x] Step 2.2: Add `SearchByVectorAsync` to `CosmosBabbleRepository`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 150-192)
- [x] Step 2.3: Integrate embedding generation into `BabbleService` create/update flows
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 194-234)
- [x] Step 2.4: Add `SearchAsync` to `BabbleService`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 236-263)
- [x] Step 2.5: Register `IEmbeddingService` and `IEmbeddingGenerator` in DI
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 265-289)
- [x] Step 2.6: Validate phase changes
  - Run `dotnet build PromptBabbler.slnx` in prompt-babbler-service/

### [x] Implementation Phase 3: API Endpoint

<!-- parallelizable: false -->

- [x] Step 3.1: Create `BabbleSearchResponse` API response model
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 293-318)
- [x] Step 3.2: Add `Search` action to `BabbleController`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 320-367)
- [x] Step 3.3: Register `IEmbeddingGenerator` in `Program.cs`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 369-396)
- [x] Step 3.4: Validate phase changes
  - Run `dotnet build PromptBabbler.slnx` in prompt-babbler-service/

### [x] Implementation Phase 4: Infrastructure — Aspire and Bicep

<!-- parallelizable: true -->

- [x] Step 4.1: Add embedding model deployment to Aspire AppHost
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 400-432)
- [x] Step 4.2: Add embedding model to `model-deployments.json`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 434-455)
- [x] Step 4.3: Add `EnableNoSQLVectorSearch` capability and vector policy to `main.bicep`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 457-498)
- [x] Step 4.4: Validate Bicep builds
  - Run `az bicep build --file infra/main.bicep`

### [x] Implementation Phase 5: Frontend — Search Component

<!-- parallelizable: true -->

- [x] Step 5.1: Install shadcn/ui Command component
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 502-516)
- [x] Step 5.2: Add `searchBabbles` function to `api-client.ts`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 518-549)
- [x] Step 5.3: Add `BabbleSearchResult` type to frontend types
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 551-574)
- [x] Step 5.4: Create `useSemanticSearch` hook
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 576-623)
- [x] Step 5.5: Create `SearchCommand` component
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 625-690)
- [x] Step 5.6: Add search trigger button to `Header.tsx`
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 692-718)
- [x] Step 5.7: Validate frontend builds and lint
  - Run `pnpm lint` and `pnpm build` in prompt-babbler-app/

### [x] Implementation Phase 6: Unit Tests

<!-- parallelizable: false -->

- [x] Step 6.1: Add `EmbeddingService` unit tests
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 722-746)
- [x] Step 6.2: Add `BabbleService.SearchAsync` unit tests
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 748-772)
- [x] Step 6.3: Add `BabbleController.Search` unit tests
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 774-798)
- [x] Step 6.4: Add frontend `useSemanticSearch` hook tests
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 800-824)
- [x] Step 6.5: Add frontend `SearchCommand` component tests
  - Details: .copilot-tracking/details/2026-04-26/babble-search-semantic-details.md (Lines 826-850)
- [x] Step 6.6: Validate all unit tests pass
  - Run `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit` in prompt-babbler-service/
  - Run `pnpm test` in prompt-babbler-app/

### [x] Implementation Phase 7: Final Validation

<!-- parallelizable: false -->

- [x] Step 7.1: Run full project validation
  - Execute `dotnet format PromptBabbler.slnx --verify-no-changes` in prompt-babbler-service/
  - Execute `dotnet build PromptBabbler.slnx` in prompt-babbler-service/
  - Execute `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit` in prompt-babbler-service/
  - Execute `az bicep build --file infra/main.bicep`
  - Execute `pnpm lint` in prompt-babbler-app/
  - Execute `pnpm build` in prompt-babbler-app/
  - Execute `pnpm test` in prompt-babbler-app/
- [x] Step 7.2: Fix minor validation issues
  - Iterate on lint errors, build warnings, and test failures
  - Apply fixes directly when corrections are straightforward
- [x] Step 7.3: Report blocking issues
  - Document issues requiring additional research
  - Provide next steps and recommended planning

## Planning Log

See .copilot-tracking/plans/logs/2026-04-26/babble-search-semantic-log.md for discrepancy tracking, implementation paths considered, and suggested follow-on work.

## Dependencies

- Azure OpenAI `text-embedding-3-small` model available in AI Foundry
- `Microsoft.Azure.Cosmos` 3.58.0 (already in project — supports vector search)
- `Microsoft.Extensions.AI.OpenAI` 10.5.0 (already in project — provides `IEmbeddingGenerator`)
- shadcn/ui Command component (cmdk dependency, to be installed)
- Cosmos DB NoSQL serverless with `EnableNoSQLVectorSearch` capability

## Success Criteria

- `ContentVector` property exists on `Babble` domain model with `ReadOnlyMemory<float>?` type — Traces to: User Requirement (domain model)
- Embedding generated synchronously on babble create and update via `IEmbeddingService` — Traces to: User Requirement (embedding generation)
- `GET /api/babbles/search?q=...&topK=...` returns 200-char snippets with similarity score — Traces to: User Requirement (search endpoint) + Derived Objective (snippets)
- Queries route to text search (1-2 words) or vector search (3+ words) server-side — Traces to: User Requirement (query routing)
- Command Palette opens with Ctrl+K, live-searches with 300ms debounce and 2-char minimum — Traces to: User Requirement (frontend component + debounce)
- Single-user mode searches `_anonymous` partition; multi-user mode searches Entra object ID partition — Traces to: User Requirement (multi-user support)
- `text-embedding-3-small` model deployed via Aspire AppHost and Bicep — Traces to: User Requirement (model deployment)
- Cosmos DB `babbles` container configured with vector embedding policy and `quantizedFlat` index — Traces to: User Requirement (Cosmos DB config)
- All unit tests pass, lint clean, builds succeed — Traces to: Derived Objective (validation)
