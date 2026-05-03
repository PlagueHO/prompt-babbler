---
applyTo: '.copilot-tracking/changes/2026-05-03/search-functionality-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: Semantic Search Functionality

## Overview

Wire together the partially-implemented semantic search feature by adding embedding generation on babble create/update, a vector search endpoint, Aspire embedding model deployment, and mounting the existing SearchCommand component in the React app.

## Objectives

### User Requirements

* Add `contentVector` property to the `Babble` domain model — Source: Task Implementation Requests
* Register `IEmbeddingService` and `IEmbeddingGenerator` in DI — Source: Task Implementation Requests
* Generate embeddings on babble create/update in `BabbleService` — Source: Task Implementation Requests
* Add a vector search endpoint (`GET /api/babbles/search`) to `BabbleController` — Source: Task Implementation Requests
* Mount the `SearchCommand` component in the React app shell — Source: Task Implementation Requests

### Derived Objectives

* Add `SearchByVectorAsync` to `IBabbleRepository` and implement in `CosmosBabbleRepository` — Derived from: search endpoint requires repository layer vector query
* Add `SearchAsync` to `IBabbleService` — Derived from: controller delegates to service interface per architecture
* Add embedding model deployment to AppHost — Derived from: embedding client needs an Azure OpenAI embedding model deployment to connect to
* Handle embedding failures gracefully (save babble without vector) — Derived from: resilience; embedding service downtime should not break babble creation
* Exclude `ContentVector` from API response serialization — Derived from: 1536 floats = ~6KB per response; unnecessary for clients

## Context Summary

### Project Files

* prompt-babbler-service/src/Domain/Models/Babble.cs - Domain model needing `ContentVector` property
* prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs - Repository interface needing search method
* prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs - Service interface needing search method
* prompt-babbler-service/src/Infrastructure/DependencyInjection.cs - DI container registration
* prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs - Service implementation needing embedding injection
* prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs - Repository needing vector search query
* prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs - Already complete; generates embeddings
* prompt-babbler-service/src/Api/Program.cs - AI client registration; needs embedding generator
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs - REST controller needing search endpoint
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs - Aspire AppHost needing embedding model deployment
* prompt-babbler-app/src/App.tsx - App shell needing SearchCommand mount
* prompt-babbler-app/src/components/search/SearchCommand.tsx - Already complete; just needs mounting

### References

* .copilot-tracking/research/2026-05-03/search-functionality-research.md - Primary research document
* .copilot-tracking/research/subagents/2026-05-03/source-file-verification.md - Source file verification
* infra/cosmos-babbles-vector-container.bicep - Cosmos vector index configuration (already deployed)
* prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs - Existing search result record
* prompt-babbler-service/src/Api/Models/Responses/BabbleSearchResponse.cs - Existing response DTO

### Standards References

* .github/copilot-instructions.md — Sealed classes, naming, security, testing conventions
* AGENTS.md — Modifying an API Endpoint checklist, CI pipeline requirements

## Implementation Checklist

### [x] Implementation Phase 1: Domain Layer Changes

<!-- parallelizable: true -->

* [x] Step 1.1: Add `ContentVector` property to `Babble` model
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 13-35)
* [x] Step 1.2: Add `SearchByVectorAsync` method to `IBabbleRepository`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 37-57)
* [x] Step 1.3: Add `SearchAsync` method to `IBabbleService`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 59-79)

### [x] Implementation Phase 2: Infrastructure and DI Registration

<!-- parallelizable: false -->
<!-- depends on: Phase 1 (interface changes) -->

* [x] Step 2.1: Register `IEmbeddingService` in `DependencyInjection.cs`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 85-105)
* [x] Step 2.2: Register `IEmbeddingGenerator` in `Program.cs`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 107-135)
* [x] Step 2.3: Add embedding model deployment to `AppHost.cs`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 137-170)
* [x] Step 2.4: Implement embedding generation in `BabbleService`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 172-220)
* [x] Step 2.5: Implement `SearchByVectorAsync` in `CosmosBabbleRepository`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 222-275)

### [x] Implementation Phase 3: API Layer

<!-- parallelizable: false -->
<!-- depends on: Phase 2 (service implementation) -->

* [x] Step 3.1: Add search endpoint to `BabbleController`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 281-330)

### [x] Implementation Phase 4: Frontend Mounting

<!-- parallelizable: true -->

* [x] Step 4.1: Mount `SearchCommand` in `App.tsx`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 336-360)

### [x] Implementation Phase 5: Unit Tests

<!-- parallelizable: false -->
<!-- depends on: Phase 2 and 3 -->

* [x] Step 5.1: Add unit tests for `BabbleService` embedding generation
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 366-420)
* [x] Step 5.2: Add unit tests for search endpoint in `BabbleController`
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 422-475)
* [x] Step 5.3: Validate existing tests still pass
  * Details: .copilot-tracking/details/2026-05-03/search-functionality-details.md (Lines 477-490)

### [x] Implementation Phase 6: Validation

<!-- parallelizable: false -->

* [x] Step 6.1: Run full project validation
  * Execute `dotnet format PromptBabbler.slnx --verify-no-changes --severity error`
  * Execute `dotnet build PromptBabbler.slnx`
  * Execute `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit`
  * Execute `cd prompt-babbler-app && pnpm lint && pnpm test && pnpm run build`
* [x] Step 6.2: Fix minor validation issues
  * No issues found — all validations passed on first run
* [x] Step 6.3: Report blocking issues
  * No blocking issues — implementation complete

## Planning Log

See .copilot-tracking/plans/logs/2026-05-03/search-functionality-log.md for discrepancy tracking, implementation paths considered, and suggested follow-on work.

## Dependencies

* .NET 10 SDK
* Azure OpenAI embedding model deployment (text-embedding-3-small, 1536 dimensions)
* `Microsoft.Extensions.AI.OpenAI` 10.5.0 (already in Directory.Packages.props)
* `Azure.AI.OpenAI` 2.1.0 (already in Directory.Packages.props)
* Cosmos DB with vector index on `/contentVector` (already deployed via Bicep)
* pnpm (frontend package manager)

## Success Criteria

* Creating or updating a babble stores a 1536-dim vector in `contentVector` — Traces to: User Requirements (embedding generation)
* `GET /api/babbles/search?query=X&topK=N` returns ranked results with similarity scores — Traces to: User Requirements (search endpoint)
* `Ctrl+K` opens the search dialog in the React app — Traces to: User Requirements (mount SearchCommand)
* All existing unit tests continue to pass — Traces to: research success criteria
* New unit tests cover embedding generation and search endpoint — Traces to: research success criteria
* Embedding failure does not prevent babble creation — Traces to: Derived Objectives (graceful fallback)
* `dotnet format --verify-no-changes` passes — Traces to: CI pipeline requirements
* Frontend lint and build pass — Traces to: CI pipeline requirements
