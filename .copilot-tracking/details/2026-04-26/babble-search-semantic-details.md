<!-- markdownlint-disable-file -->
# Implementation Details: Babble Semantic Search

## Context Reference

Sources: .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md, .copilot-tracking/research/subagents/2026-04-26/codebase-structure-research.md, .copilot-tracking/research/subagents/2026-04-26/service-deep-analysis-research.md, .copilot-tracking/research/subagents/2026-04-26/frontend-search-research.md

## Implementation Phase 1: Domain Model and Interfaces

<!-- parallelizable: false -->

### Step 1.1: Add `ContentVector` property to `Babble` record

Add a nullable `ReadOnlyMemory<float>?` property to the existing sealed `Babble` record for storing the embedding vector.

Files:
- prompt-babbler-service/src/Domain/Models/Babble.cs — Add `ContentVector` property with `[JsonPropertyName("contentVector")]`

Discrepancy references:
- None — direct user requirement

Success criteria:
- `Babble` record has `ContentVector` property of type `ReadOnlyMemory<float>?`
- Property has `[JsonPropertyName("contentVector")]` attribute
- Property is nullable (existing babbles without vectors remain valid)
- Project compiles without warnings

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 268-280) — Updated Babble domain model

Dependencies:
- None

### Step 1.2: Create `BabbleSearchResult` domain model

Create a new record to represent a search result with the babble and its similarity score.

Files:
- prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs — NEW file

```csharp
namespace PromptBabbler.Domain.Models;

public sealed record BabbleSearchResult(Babble Babble, double SimilarityScore);
```

Discrepancy references:
- DD-01 — Uses simple domain model; API layer transforms to snippet-based response

Success criteria:
- Record exists in Domain/Models namespace
- Contains `Babble` and `SimilarityScore` properties
- Is a sealed record

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 284-288) — Search result model

Dependencies:
- Step 1.1 (Babble model has ContentVector)

### Step 1.3: Create `IEmbeddingService` interface

Create an interface in the Domain layer that abstracts embedding generation from the MEAI dependency.

Files:
- prompt-babbler-service/src/Domain/Interfaces/IEmbeddingService.cs — NEW file

```csharp
namespace PromptBabbler.Domain.Interfaces;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
```

Discrepancy references:
- None — derived objective from Clean Architecture

Success criteria:
- Interface exists in Domain/Interfaces namespace
- Single method `GenerateEmbeddingAsync` accepting string text
- Returns `ReadOnlyMemory<float>` (not the MEAI `Embedding<float>` type — keeps Domain layer clean)

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 496-501) — Search API file tree

Dependencies:
- None

### Step 1.4: Add `SearchByVectorAsync` to `IBabbleRepository`

Add a vector search method to the existing repository interface.

Files:
- prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs — Add method

```csharp
Task<IReadOnlyList<BabbleSearchResult>> SearchByVectorAsync(
    string userId,
    ReadOnlyMemory<float> embedding,
    int topK = 10,
    CancellationToken cancellationToken = default);
```

Discrepancy references:
- None — direct user requirement

Success criteria:
- Method signature matches the pattern above
- Returns `IReadOnlyList<BabbleSearchResult>` (domain model, not tuples)
- Accepts userId for partition scoping

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 517-521) — Repository method signature

Dependencies:
- Step 1.2 (BabbleSearchResult exists)

### Step 1.5: Add `SearchAsync` to `IBabbleService`

Add a search method to the existing service interface.

Files:
- prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs — Add method

```csharp
Task<IReadOnlyList<BabbleSearchResult>> SearchAsync(
    string userId,
    string query,
    int topK = 10,
    CancellationToken cancellationToken = default);
```

Discrepancy references:
- None — direct user requirement

Success criteria:
- Method signature matches the pattern above
- Accepts raw query string (service determines routing to text vs vector)
- Returns `IReadOnlyList<BabbleSearchResult>`

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 505-507) — Service layer flow

Dependencies:
- Step 1.2 (BabbleSearchResult exists)

## Implementation Phase 2: Infrastructure — Embedding and Repository

<!-- parallelizable: false -->

### Step 2.1: Create `EmbeddingService` implementation

Implement `IEmbeddingService` wrapping the MEAI `IEmbeddingGenerator<string, Embedding<float>>`.

Files:
- prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs — NEW file

```csharp
using Microsoft.Extensions.AI;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Infrastructure.Services;

public sealed class EmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : IEmbeddingService
{
    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var embeddings = await embeddingGenerator.GenerateAsync(
            [text],
            cancellationToken: cancellationToken);

        return embeddings[0].Vector;
    }
}
```

Discrepancy references:
- None

Success criteria:
- Class implements `IEmbeddingService`
- Uses constructor injection for `IEmbeddingGenerator<string, Embedding<float>>`
- Calls `GenerateAsync` with single-element array
- Returns `ReadOnlyMemory<float>` from the first embedding result

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 150-156) — Embedding generation pattern
- prompt-babbler-service/src/Infrastructure/Services/AzureOpenAiPromptGenerationService.cs — Existing MEAI pattern

Dependencies:
- Step 1.3 (IEmbeddingService interface)

### Step 2.2: Add `SearchByVectorAsync` to `CosmosBabbleRepository`

Implement vector search using Cosmos DB `VectorDistance` function.

Files:
- prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs — Add method

The method uses a parameterized Cosmos DB SQL query with `VectorDistance`:

```csharp
public async Task<IReadOnlyList<BabbleSearchResult>> SearchByVectorAsync(
    string userId,
    ReadOnlyMemory<float> embedding,
    int topK = 10,
    CancellationToken cancellationToken = default)
{
    var queryText = @"
        SELECT TOP @topK c.id, c.userId, c.title, c.tags, c.createdAt, c.updatedAt, c.isPinned,
               SUBSTRING(c.text, 0, 200) AS text,
               VectorDistance(c.contentVector, @embedding) AS similarityScore
        FROM c
        WHERE c.userId = @userId
        ORDER BY VectorDistance(c.contentVector, @embedding)";

    var queryDef = new QueryDefinition(queryText)
        .WithParameter("@topK", topK)
        .WithParameter("@embedding", embedding)
        .WithParameter("@userId", userId);

    var results = new List<BabbleSearchResult>();
    using var iterator = _container.GetItemQueryIterator<JsonElement>(
        queryDef,
        requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId) });

    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync(cancellationToken);
        foreach (var item in response)
        {
            var babble = new Babble
            {
                Id = item.GetProperty("id").GetString()!,
                UserId = item.GetProperty("userId").GetString()!,
                Title = item.GetProperty("title").GetString()!,
                Text = item.GetProperty("text").GetString()!,
                Tags = item.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array
                    ? tags.EnumerateArray().Select(t => t.GetString()!).ToList()
                    : null,
                CreatedAt = item.GetProperty("createdAt").GetDateTimeOffset(),
                UpdatedAt = item.GetProperty("updatedAt").GetDateTimeOffset(),
                IsPinned = item.TryGetProperty("isPinned", out var pinned) && pinned.GetBoolean(),
            };
            var score = item.GetProperty("similarityScore").GetDouble();
            results.Add(new BabbleSearchResult(babble, score));
        }
    }

    return results;
}
```

Note: The query uses `SUBSTRING(c.text, 0, 200)` to return only 200-char snippets from Cosmos DB, avoiding transferring full text for search results.

Discrepancy references:
- DD-01 — Implements snippet projection per Scenario 11 design

Success criteria:
- Method uses parameterized `VectorDistance` query
- Scopes to userId partition key
- Returns `IReadOnlyList<BabbleSearchResult>` with snippet text and similarity score
- Uses `SUBSTRING` for 200-char text truncation

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 158-169) — Cosmos DB vector query pattern
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 861-868) — Snippet projection query

Dependencies:
- Step 1.4 (IBabbleRepository.SearchByVectorAsync)
- Step 1.2 (BabbleSearchResult)

### Step 2.3: Integrate embedding generation into `BabbleService` create/update flows

Modify existing `CreateAsync` and `UpdateAsync` methods in `BabbleService` to generate embeddings before saving.

Files:
- prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs — Modify constructor and create/update methods

Changes:
1. Add `IEmbeddingService` to constructor injection
2. In `CreateAsync`: generate embedding from `$"{title}\n{text}"`, set `ContentVector` on babble before calling repository
3. In `UpdateAsync`: regenerate embedding from updated title + text, set `ContentVector` on babble before calling repository

```csharp
// Constructor addition
public sealed class BabbleService(
    IBabbleRepository babbleRepository,
    IGeneratedPromptRepository generatedPromptRepository,
    IEmbeddingService embeddingService) : IBabbleService

// In CreateAsync, before repository call:
var textToEmbed = $"{babble.Title}\n{babble.Text}";
var vector = await embeddingService.GenerateEmbeddingAsync(textToEmbed, cancellationToken);
var babbleWithVector = babble with { ContentVector = vector };
return await babbleRepository.CreateAsync(babbleWithVector, cancellationToken);

// In UpdateAsync, before repository call:
var textToEmbed = $"{updatedBabble.Title}\n{updatedBabble.Text}";
var vector = await embeddingService.GenerateEmbeddingAsync(textToEmbed, cancellationToken);
var babbleWithVector = updatedBabble with { ContentVector = vector };
return await babbleRepository.UpdateAsync(babbleWithVector, cancellationToken);
```

Discrepancy references:
- None — direct user requirement (Scenario 3)

Success criteria:
- `IEmbeddingService` injected into `BabbleService` constructor
- Embedding generated on both create and update paths
- Combined `title\ntext` used as embedding input
- ContentVector set on Babble record via `with` expression before repository call

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 170-178) — Embedding generation on create/update
- prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs — Existing service implementation

Dependencies:
- Step 2.1 (EmbeddingService exists)
- Step 1.1 (Babble has ContentVector)

### Step 2.4: Add `SearchAsync` to `BabbleService`

Implement search routing logic: text search for short queries, vector search for semantic queries.

Files:
- prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs — Add method

```csharp
public async Task<IReadOnlyList<BabbleSearchResult>> SearchAsync(
    string userId,
    string query,
    int topK = 10,
    CancellationToken cancellationToken = default)
{
    var words = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // Route by query complexity: 1-2 words AND < 15 chars → text search, else → vector search
    if (words.Length <= 2 && query.Length < 15)
    {
        // Text search — use existing CONTAINS pattern via repository
        var babbles = await babbleRepository.GetByUserAsync(userId, query, cancellationToken: cancellationToken);
        return babbles.Items
            .Select(b => new BabbleSearchResult(b with { Text = b.Text.Length > 200 ? b.Text[..200] : b.Text }, 0.0))
            .ToList();
    }

    // Vector search — generate embedding and query Cosmos DB
    var embedding = await embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
    return await babbleRepository.SearchByVectorAsync(userId, embedding, topK, cancellationToken);
}
```

Discrepancy references:
- DD-02 — Routing is server-side only

Success criteria:
- Routes queries with 1-2 words AND < 15 chars to text search
- Routes queries with 3+ words OR >= 15 chars to vector search
- Text search reuses existing `GetByUserAsync` with search parameter
- Vector search generates embedding then calls `SearchByVectorAsync`
- Returns consistent `IReadOnlyList<BabbleSearchResult>` regardless of route

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Scenario 7) — Query routing strategy

Dependencies:
- Step 2.1 (EmbeddingService)
- Step 2.2 (SearchByVectorAsync)
- Step 1.5 (IBabbleService.SearchAsync)

### Step 2.5: Register `IEmbeddingService` and `IEmbeddingGenerator` in DI

Register the embedding service in the Infrastructure DI configuration.

Files:
- prompt-babbler-service/src/Infrastructure/DependencyInjection.cs — Add registration

```csharp
services.AddSingleton<IEmbeddingService, EmbeddingService>();
```

Note: `IEmbeddingGenerator<string, Embedding<float>>` is registered in `Program.cs` (Step 3.3) because it depends on the `AzureOpenAIClient` configured there. The Infrastructure DI only registers the wrapping `EmbeddingService`.

Discrepancy references:
- None

Success criteria:
- `IEmbeddingService` registered as Singleton (matches Cosmos repository lifetime)
- Registration is in `DependencyInjection.cs` alongside other Infrastructure services

Context references:
- prompt-babbler-service/src/Infrastructure/DependencyInjection.cs — Existing DI registrations

Dependencies:
- Step 2.1 (EmbeddingService class exists)

### Step 2.6: Validate phase changes

Run build to confirm all Infrastructure and Domain changes compile.

Validation commands:
- `dotnet build PromptBabbler.slnx` in prompt-babbler-service/ — Full solution build

## Implementation Phase 3: API Endpoint

<!-- parallelizable: false -->

### Step 3.1: Create `BabbleSearchResponse` API response model

Create the API response model for search results with snippets and scores.

Files:
- prompt-babbler-service/src/Api/Models/Responses/BabbleSearchResponse.cs — NEW file

```csharp
namespace PromptBabbler.Api.Models.Responses;

public sealed record BabbleSearchResultItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Snippet { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public bool IsPinned { get; init; }
    public required double Score { get; init; }
}

public sealed record BabbleSearchResponse
{
    public required IReadOnlyList<BabbleSearchResultItem> Results { get; init; }
}
```

Discrepancy references:
- DD-01 — Uses snippet-based response per Scenario 11

Success criteria:
- Response model includes `Id`, `Title`, `Snippet` (not full text), `Tags`, `CreatedAt`, `IsPinned`, `Score`
- Does NOT include `ContentVector` or full `Text`
- Wrapped in `BabbleSearchResponse` with `Results` array

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 840-856) — Snippet response format

Dependencies:
- None

### Step 3.2: Add `Search` action to `BabbleController`

Add a new GET endpoint for semantic search.

Files:
- prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Add action method

```csharp
[HttpGet("search")]
[ProducesResponseType(typeof(BabbleSearchResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Search(
    [FromQuery(Name = "q")] string query,
    [FromQuery] int topK = 10,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(query) || query.Length < 2 || query.Length > 200)
    {
        return BadRequest("Query must be between 2 and 200 characters.");
    }

    if (topK is < 1 or > 50)
    {
        return BadRequest("topK must be between 1 and 50.");
    }

    var userId = User.GetUserIdOrAnonymous();
    var results = await babbleService.SearchAsync(userId, query, topK, cancellationToken);

    var response = new BabbleSearchResponse
    {
        Results = results.Select(r => new BabbleSearchResultItem
        {
            Id = r.Babble.Id,
            Title = r.Babble.Title,
            Snippet = r.Babble.Text, // Already truncated to 200 chars by repository
            Tags = r.Babble.Tags,
            CreatedAt = r.Babble.CreatedAt,
            IsPinned = r.Babble.IsPinned,
            Score = r.SimilarityScore,
        }).ToList(),
    };

    return Ok(response);
}
```

Important: This endpoint must be placed BEFORE the `[HttpGet("{id}")]` route in the controller to avoid route conflicts (ASP.NET Core evaluates routes top-down; "search" would match the `{id}` parameter otherwise).

Discrepancy references:
- None — direct user requirement

Success criteria:
- Endpoint at `GET /api/babbles/search?q=...&topK=...`
- Validates query length (2-200 chars) and topK (1-50)
- Uses `User.GetUserIdOrAnonymous()` for partition scoping
- Returns `BabbleSearchResponse` with snippet-based results
- Route placed before `{id}` route to avoid conflicts

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 239-265) — Search endpoint contract
- prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Existing controller patterns

Dependencies:
- Step 1.5 (IBabbleService.SearchAsync)
- Step 3.1 (BabbleSearchResponse)

### Step 3.3: Register `IEmbeddingGenerator` in `Program.cs`

Register the MEAI embedding generator from the existing `AzureOpenAIClient`.

Files:
- prompt-babbler-service/src/Api/Program.cs — Add registration after existing `IChatClient`

```csharp
// After existing IChatClient registration
var embeddingDeploymentName = builder.Configuration["MicrosoftFoundry:embeddingDeploymentName"] ?? "embedding";
var embeddingClient = openAiClient.GetEmbeddingClient(embeddingDeploymentName)
    .AsIEmbeddingGenerator();
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(embeddingClient);
```

Required using additions:
```csharp
using Microsoft.Extensions.AI;
```

Discrepancy references:
- None

Success criteria:
- `IEmbeddingGenerator<string, Embedding<float>>` registered as Singleton
- Uses existing `AzureOpenAIClient` (`openAiClient`) already in Program.cs
- Deployment name configurable via `MicrosoftFoundry:embeddingDeploymentName` with "embedding" default
- Uses `.AsIEmbeddingGenerator()` extension method

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 307-313) — Program.cs registration
- prompt-babbler-service/src/Api/Program.cs — Existing AzureOpenAIClient and IChatClient registration

Dependencies:
- None (can be done independently, but logically after Step 2.5)

### Step 3.4: Validate phase changes

Run build to confirm API changes compile.

Validation commands:
- `dotnet build PromptBabbler.slnx` in prompt-babbler-service/ — Full solution build

## Implementation Phase 4: Infrastructure — Aspire and Bicep

<!-- parallelizable: true -->

### Step 4.1: Add embedding model deployment to Aspire AppHost

Add the `text-embedding-3-small` model deployment to the Aspire AppHost and wire it to the API service.

Files:
- prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs — Add embedding deployment

```csharp
// After existing chat model deployment
var embeddingDeployment = foundryProject.AddModelDeployment(
    "embedding",
    builder.Configuration["MicrosoftFoundry:embeddingModelName"] ?? "text-embedding-3-small",
    builder.Configuration["MicrosoftFoundry:embeddingModelVersion"] ?? "1",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });

// Add to API service references (alongside existing chat deployment reference)
// apiService.WithReference(embeddingDeployment).WaitFor(embeddingDeployment);
```

Discrepancy references:
- DR-01 — Aspire vector policy propagation unverified; this step focuses on model deployment only

Success criteria:
- `text-embedding-3-small` model deployment added with name "embedding"
- Model name and version configurable via configuration
- GlobalStandard SKU with capacity 50
- API service references the embedding deployment

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 296-306) — Aspire AppHost config
- prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs — Existing AppHost

Dependencies:
- None

### Step 4.2: Add embedding model to `model-deployments.json`

Add the embedding model deployment configuration for Bicep.

Files:
- infra/model-deployments.json — Add embedding model entry

Add to the existing JSON array:
```json
{
  "model": { "format": "OpenAI", "name": "text-embedding-3-small", "version": "1" },
  "name": "embedding",
  "sku": { "name": "GlobalStandard", "capacity": 50 }
}
```

Discrepancy references:
- None

Success criteria:
- `text-embedding-3-small` model entry added to array
- Format, name, version match Azure AI Foundry catalog
- SKU is GlobalStandard with capacity 50
- No `raiPolicyName` (embedding models don't use content policies)

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 289-297) — Model deployments config
- infra/model-deployments.json — Existing deployment config

Dependencies:
- None

### Step 4.3: Add `EnableNoSQLVectorSearch` capability and vector policy to `main.bicep`

Enable vector search on the Cosmos DB account and configure the babbles container with vector embedding policy.

Files:
- infra/main.bicep — Modify Cosmos DB account and container configuration

Changes:
1. Add `EnableNoSQLVectorSearch` to the Cosmos DB account capabilities array (alongside existing `EnableServerless`)
2. Add `vectorEmbeddingPolicy` to the babbles container definition with path `/contentVector`, Float32, Cosine, 1536 dimensions
3. Add `vectorIndexes` to the babbles container indexing policy with `quantizedFlat` type
4. Add `/contentVector/*` to excluded paths in the indexing policy

Note: DR-02 flags that AVM module support for these properties is unverified. If the AVM module does not support `vectorEmbeddingPolicy`, use a raw `Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers` resource definition for the babbles container.

Discrepancy references:
- DR-02 — AVM module vector policy support unverified; may need raw resource fallback

Success criteria:
- `EnableNoSQLVectorSearch` capability added to Cosmos DB account
- babbles container has `vectorEmbeddingPolicy` with `/contentVector` path
- Vector index type is `quantizedFlat`
- `/contentVector/*` excluded from regular indexing
- Bicep validates with `az bicep build`

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 183-206) — Container properties with vector policy
- infra/main.bicep — Existing Cosmos DB configuration

Dependencies:
- None

### Step 4.4: Validate Bicep builds

Validation commands:
- `az bicep build --file infra/main.bicep` — Validate Bicep compilation

## Implementation Phase 5: Frontend — Search Component

<!-- parallelizable: true -->

### Step 5.1: Install shadcn/ui Command component

Install the cmdk-based Command component from shadcn/ui.

Files:
- prompt-babbler-app/src/components/ui/command.tsx — Generated by shadcn CLI

Run command in prompt-babbler-app/:
```bash
npx shadcn@latest add command
```

This installs:
- `cmdk` npm package (the underlying Command Menu primitive)
- `command.tsx` UI component in `src/components/ui/`
- Updates to `package.json` and lockfile

Success criteria:
- `cmdk` package in `package.json` dependencies
- `command.tsx` exists in `src/components/ui/`
- Exports `CommandDialog`, `CommandInput`, `CommandList`, `CommandItem`, `CommandEmpty`

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 219-220) — Install command
- prompt-babbler-app/components.json — shadcn/ui configuration

Dependencies:
- None

### Step 5.2: Add `searchBabbles` function to `api-client.ts`

Add the API function for calling the search endpoint.

Files:
- prompt-babbler-app/src/services/api-client.ts — Add function

```typescript
export async function searchBabbles(
  query: string,
  topK: number = 10,
  accessToken?: string
): Promise<BabbleSearchResponse> {
  return fetchJson(`/api/babbles/search?q=${encodeURIComponent(query)}&topK=${topK}`, {
    headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
  });
}
```

The function uses the existing `fetchJson` helper from the same file.

Discrepancy references:
- None

Success criteria:
- Function exported from `api-client.ts`
- Uses `encodeURIComponent` for query parameter encoding
- Uses existing `fetchJson` helper
- Accepts optional `accessToken` for multi-user mode
- Returns typed `BabbleSearchResponse`

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 602-615) — API client function
- prompt-babbler-app/src/services/api-client.ts — Existing API client

Dependencies:
- Step 5.3 (BabbleSearchResult type)

### Step 5.3: Add `BabbleSearchResult` type to frontend types

Add TypeScript types for the search API response.

Files:
- prompt-babbler-app/src/types/index.ts — Add types

```typescript
export interface BabbleSearchResultItem {
  id: string;
  title: string;
  snippet: string;
  tags?: string[];
  createdAt: string;
  isPinned: boolean;
  score: number;
}

export interface BabbleSearchResponse {
  results: BabbleSearchResultItem[];
}
```

Discrepancy references:
- DD-01 — Matches snippet-based API response

Success criteria:
- Types exported from `types/index.ts`
- `BabbleSearchResultItem` has `snippet` (not `text`), `score`, and all metadata fields
- `BabbleSearchResponse` wraps results array

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 602-614) — TypeScript types

Dependencies:
- None

### Step 5.4: Create `useSemanticSearch` hook

Create a custom hook for debounced search with the API.

Files:
- prompt-babbler-app/src/hooks/useSemanticSearch.ts — NEW file

```typescript
import { useState, useEffect, useCallback, useRef } from "react";
import { searchBabbles } from "@/services/api-client";
import type { BabbleSearchResultItem } from "@/types";

export function useSemanticSearch(query: string, topK: number = 10) {
  const [results, setResults] = useState<BabbleSearchResultItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    // Minimum 2 characters before searching
    if (query.trim().length < 2) {
      setResults([]);
      setLoading(false);
      return;
    }

    setLoading(true);

    const timeoutId = setTimeout(async () => {
      // Cancel previous request
      abortControllerRef.current?.abort();
      abortControllerRef.current = new AbortController();

      try {
        const response = await searchBabbles(query, topK);
        setResults(response.results);
        setError(null);
      } catch (err) {
        if (err instanceof DOMException && err.name === "AbortError") return;
        setError(err instanceof Error ? err.message : "Search failed");
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, 300); // 300ms debounce

    return () => clearTimeout(timeoutId);
  }, [query, topK]);

  return { results, loading, error };
}
```

Discrepancy references:
- None

Success criteria:
- Hook exports `{ results, loading, error }`
- 300ms debounce delay matches existing codebase pattern
- 2-character minimum threshold
- Cancels previous request on new query
- Handles abort errors gracefully

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Scenarios 8, 9) — Debounce and minimum thresholds
- prompt-babbler-app/src/hooks/useBabbles.ts — Existing hook pattern

Dependencies:
- Step 5.2 (searchBabbles function)
- Step 5.3 (BabbleSearchResultItem type)

### Step 5.5: Create `SearchCommand` component

Create the Command Palette dialog component.

Files:
- prompt-babbler-app/src/components/search/SearchCommand.tsx — NEW file

The component:
1. Listens for Ctrl+K keyboard shortcut to toggle open/close
2. Uses `CommandDialog` from shadcn/ui
3. Calls `useSemanticSearch` with current input value
4. Renders results as `CommandItem` entries with title, snippet, tags
5. Navigates to babble detail on selection
6. Uses `shouldFilter={false}` since filtering is server-side

```typescript
import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import {
  CommandDialog,
  CommandInput,
  CommandList,
  CommandItem,
  CommandEmpty,
  CommandGroup,
} from "@/components/ui/command";
import { useSemanticSearch } from "@/hooks/useSemanticSearch";
import { Badge } from "@/components/ui/badge";
import { Loader2 } from "lucide-react";

export function SearchCommand() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const { results, loading } = useSemanticSearch(query);
  const navigate = useNavigate();

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.key === "k" && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        setOpen((prev) => !prev);
      }
    };
    document.addEventListener("keydown", down);
    return () => document.removeEventListener("keydown", down);
  }, []);

  const handleSelect = (babbleId: string) => {
    setOpen(false);
    setQuery("");
    navigate(`/babble/${babbleId}`);
  };

  return (
    <CommandDialog open={open} onOpenChange={setOpen}>
      <CommandInput
        placeholder="Search babbles..."
        value={query}
        onValueChange={setQuery}
      />
      <CommandList>
        {loading && (
          <div className="flex items-center justify-center py-6">
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
          </div>
        )}
        <CommandEmpty>
          {query.length < 2 ? "Type to search..." : "No results found."}
        </CommandEmpty>
        {results.length > 0 && (
          <CommandGroup heading="Babbles">
            {results.map((result) => (
              <CommandItem
                key={result.id}
                value={result.id}
                onSelect={() => handleSelect(result.id)}
              >
                <div className="flex flex-col gap-1">
                  <span className="font-medium">{result.title}</span>
                  <span className="text-muted-foreground text-sm line-clamp-2">
                    {result.snippet}
                  </span>
                  {result.tags && result.tags.length > 0 && (
                    <div className="flex gap-1 mt-1">
                      {result.tags.slice(0, 3).map((tag) => (
                        <Badge key={tag} variant="secondary" className="text-xs">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  )}
                </div>
              </CommandItem>
            ))}
          </CommandGroup>
        )}
      </CommandList>
    </CommandDialog>
  );
}
```

Discrepancy references:
- DD-02 — No client-side pre-filtering phase; all results from server

Success criteria:
- Ctrl+K opens/closes the dialog
- Input triggers `useSemanticSearch` with debounced, server-side search
- Results display title, snippet (truncated), and up to 3 tags
- Selection navigates to `/babble/:id` and closes dialog
- Loading state shows spinner
- Empty state distinguishes between "type to search" and "no results"
- Uses `shouldFilter={false}` on CommandDialog (cmdk default, since results are server-filtered)

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Lines 210-248) — Command Palette pattern
- .copilot-tracking/research/subagents/2026-04-26/frontend-search-research.md — Frontend research

Dependencies:
- Step 5.1 (Command component installed)
- Step 5.4 (useSemanticSearch hook)

### Step 5.6: Add search trigger button to `Header.tsx`

Add a search button between the nav links and the UserMenu.

Files:
- prompt-babbler-app/src/components/layout/Header.tsx — Add button and import SearchCommand

Add a button with a search icon and Ctrl+K keyboard shortcut badge:

```typescript
import { Search } from "lucide-react";
import { SearchCommand } from "@/components/search/SearchCommand";

// In the Header JSX, between nav and ml-auto UserMenu:
<Button
  variant="outline"
  className="relative h-9 w-9 p-0 xl:h-10 xl:w-60 xl:justify-start xl:px-3 xl:py-2"
  onClick={() => setSearchOpen(true)}
>
  <Search className="h-4 w-4 xl:mr-2" />
  <span className="hidden xl:inline-flex">Search babbles...</span>
  <kbd className="pointer-events-none absolute right-1.5 top-2 hidden h-6 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-xs font-medium opacity-100 xl:flex">
    <span className="text-xs">⌘</span>K
  </kbd>
</Button>
<SearchCommand />
```

Note: The `SearchCommand` component manages its own open state via the Ctrl+K keyboard shortcut. The header button provides a visual affordance and click target. The two share state via the keyboard event listener — clicking the button can also trigger the keyboard shortcut programmatically, or `SearchCommand` can accept an `open`/`onOpenChange` prop pair.

Discrepancy references:
- None

Success criteria:
- Search button visible in Header between nav and UserMenu
- Shows search icon on mobile, "Search babbles..." label on desktop
- Ctrl+K badge visible on desktop
- SearchCommand component rendered in Header
- Clicking button opens the Command Palette

Context references:
- .copilot-tracking/research/2026-04-26/babble-search-semantic-research.md (Scenario 5) — Header search trigger
- prompt-babbler-app/src/components/layout/Header.tsx — Existing header layout

Dependencies:
- Step 5.5 (SearchCommand component)

### Step 5.7: Validate frontend builds and lint

Validation commands:
- `pnpm lint` in prompt-babbler-app/ — ESLint validation
- `pnpm build` in prompt-babbler-app/ — Vite production build

## Implementation Phase 6: Unit Tests

<!-- parallelizable: false -->

### Step 6.1: Add `EmbeddingService` unit tests

Files:
- prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/EmbeddingServiceTests.cs — NEW file

Test cases:
- `GenerateEmbeddingAsync_ReturnsVector_WhenTextProvided` — Verify wrapping of IEmbeddingGenerator
- `GenerateEmbeddingAsync_PassesCancellationToken` — Verify CancellationToken forwarded

Pattern: MSTest v4, NSubstitute for `IEmbeddingGenerator<string, Embedding<float>>`, FluentAssertions for assertions.

Success criteria:
- Tests use `[TestCategory("Unit")]` attribute
- NSubstitute mocks `IEmbeddingGenerator`
- Tests verify correct delegation to MEAI abstraction

Context references:
- prompt-babbler-service/tests/unit/ — Existing test structure

Dependencies:
- Step 2.1 (EmbeddingService implementation)

### Step 6.2: Add `BabbleService.SearchAsync` unit tests

Files:
- prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/BabbleServiceTests.cs — Add tests to existing file or create new

Test cases:
- `SearchAsync_ShortQuery_UsesTextSearch` — 1-2 word query routes to `GetByUserAsync`
- `SearchAsync_LongQuery_UsesVectorSearch` — 3+ word query routes to `SearchByVectorAsync`
- `SearchAsync_ShortQueryLongChars_UsesVectorSearch` — 2 words but >= 15 chars routes to vector
- `CreateAsync_GeneratesEmbedding` — Verify embedding generated on create
- `UpdateAsync_RegeneratesEmbedding` — Verify embedding regenerated on update

Pattern: MSTest v4, NSubstitute for `IBabbleRepository`, `IEmbeddingService`, `IGeneratedPromptRepository`.

Success criteria:
- Tests verify routing logic based on word count and character length
- Tests verify embedding generation on create/update
- Tests use `[TestCategory("Unit")]` attribute

Context references:
- prompt-babbler-service/tests/unit/ — Existing test structure

Dependencies:
- Step 2.3 (BabbleService create/update changes)
- Step 2.4 (BabbleService.SearchAsync)

### Step 6.3: Add `BabbleController.Search` unit tests

Files:
- prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs — Add tests to existing file or create new

Test cases:
- `Search_ValidQuery_ReturnsOkWithResults` — Happy path
- `Search_QueryTooShort_ReturnsBadRequest` — Under 2 chars
- `Search_QueryTooLong_ReturnsBadRequest` — Over 200 chars
- `Search_InvalidTopK_ReturnsBadRequest` — topK out of range
- `Search_UsesCurrentUserPartition` — Verifies user ID extraction

Pattern: MSTest v4, NSubstitute for `IBabbleService`.

Success criteria:
- Tests verify input validation (query length, topK range)
- Tests verify response mapping from domain to API model
- Tests verify user ID extraction via mocked ClaimsPrincipal
- Tests use `[TestCategory("Unit")]` attribute

Context references:
- prompt-babbler-service/tests/unit/ — Existing test structure

Dependencies:
- Step 3.2 (BabbleController.Search)

### Step 6.4: Add frontend `useSemanticSearch` hook tests

Files:
- prompt-babbler-app/tests/hooks/useSemanticSearch.test.ts — NEW file

Test cases:
- Returns empty results for query under 2 characters
- Debounces API calls (300ms)
- Returns search results from API
- Handles API errors gracefully
- Cancels previous request on new query

Pattern: Vitest, React Testing Library `renderHook`, mock `api-client.ts`.

Success criteria:
- Tests cover minimum character threshold
- Tests verify debounce behavior
- Tests mock API responses
- Tests verify error handling

Context references:
- prompt-babbler-app/tests/ — Existing test structure

Dependencies:
- Step 5.4 (useSemanticSearch hook)

### Step 6.5: Add frontend `SearchCommand` component tests

Files:
- prompt-babbler-app/tests/components/SearchCommand.test.tsx — NEW file

Test cases:
- Opens on Ctrl+K keyboard shortcut
- Closes on Escape
- Renders search results
- Navigates on result selection
- Shows loading state
- Shows empty state messages

Pattern: Vitest, React Testing Library, mock `useSemanticSearch` hook.

Success criteria:
- Tests verify keyboard shortcut behavior
- Tests verify result rendering
- Tests verify navigation on selection

Context references:
- prompt-babbler-app/tests/ — Existing test structure

Dependencies:
- Step 5.5 (SearchCommand component)

### Step 6.6: Validate all unit tests pass

Validation commands:
- `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit --configuration Release --no-restore` in prompt-babbler-service/
- `pnpm test` in prompt-babbler-app/

## Implementation Phase 7: Final Validation

<!-- parallelizable: false -->

### Step 7.1: Run full project validation

Execute all validation commands for the project:
- `dotnet format PromptBabbler.slnx --verify-no-changes` in prompt-babbler-service/
- `dotnet build PromptBabbler.slnx` in prompt-babbler-service/
- `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit --configuration Release --no-restore` in prompt-babbler-service/
- `az bicep build --file infra/main.bicep`
- `pnpm lint` in prompt-babbler-app/
- `pnpm build` in prompt-babbler-app/
- `pnpm test` in prompt-babbler-app/

### Step 7.2: Fix minor validation issues

Iterate on lint errors, build warnings, and test failures. Apply fixes directly when corrections are straightforward and isolated.

### Step 7.3: Report blocking issues

When validation failures require changes beyond minor fixes:
- Document the issues and affected files.
- Provide the user with next steps.
- Recommend additional research and planning rather than inline fixes.
- Avoid large-scale refactoring within this phase.

## Dependencies

- `Microsoft.Azure.Cosmos` 3.58.0 (already present)
- `Microsoft.Extensions.AI.OpenAI` 10.5.0 (already present)
- `Azure.AI.OpenAI` 2.1.0 (already present)
- `cmdk` npm package (installed via shadcn/ui Command)
- Azure OpenAI `text-embedding-3-small` model deployment

## Success Criteria

- All backend code compiles without warnings
- All unit tests pass
- Frontend builds and lints clean
- Bicep validates successfully
- Search endpoint returns snippets with scores for vector queries
- Search endpoint returns title-matched results for short text queries
- Command Palette opens with Ctrl+K and displays live search results
