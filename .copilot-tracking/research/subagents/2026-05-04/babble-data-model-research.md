# Babble Data Model Research

## Research Topics

1. Backend Babble domain model — all properties, types, JSON serialization attributes
1. Frontend Babble TypeScript interface
1. Babble API controller endpoints
1. Babble service interface and implementation
1. Babble repository interface and implementation
1. Frontend BabbleCard component and related UI components
1. Cosmos DB container configuration for babbles

---

## 1. Backend Babble Domain Model

### File: `prompt-babbler-service/src/Domain/Models/Babble.cs`

```csharp
using System.Text.Json.Serialization;

namespace PromptBabbler.Domain.Models;

public sealed record Babble
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; init; }

    [JsonPropertyName("contentVector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? ContentVector { get; init; }
}
```

#### Property Summary

| Property | Type | Required | JSON Name | Notes |
|---|---|---|---|---|
| Id | `string` | Yes | `id` | Cosmos DB document ID (GUID) |
| UserId | `string` | Yes | `userId` | Partition key in Cosmos DB |
| Title | `string` | Yes | `title` | Max 200 chars (validated in controller) |
| Text | `string` | Yes | `text` | Max 50,000 chars (validated in controller) |
| CreatedAt | `DateTimeOffset` | Yes | `createdAt` | Set on creation |
| Tags | `IReadOnlyList<string>?` | No | `tags` | Max 20 tags, each max 50 chars |
| UpdatedAt | `DateTimeOffset` | Yes | `updatedAt` | Updated on any modification |
| IsPinned | `bool` | No (defaults false) | `isPinned` | Pin/unpin state |
| ContentVector | `float[]?` | No | `contentVector` | 1536-dimension embedding vector; excluded from JSON when null |

### File: `prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs`

```csharp
namespace PromptBabbler.Domain.Models;

public sealed record BabbleSearchResult(Babble Babble, double SimilarityScore);
```

---

## 2. Frontend Babble TypeScript Interface

### File: `prompt-babbler-app/src/types/index.ts`

```typescript
export interface Babble {
  id: string;
  title: string;
  text: string;
  tags?: string[];
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
}
```

#### Key Differences from Backend

- **No `userId`** — not exposed to frontend
- **No `contentVector`** — internal server-side field, never sent to client
- **Dates are `string`** (ISO 8601) — backend serializes `DateTimeOffset` as "o" format strings in the API response

#### Related Frontend Types (same file)

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

---

## 3. Babble API Controller

### File: `prompt-babbler-service/src/Api/Controllers/BabbleController.cs`

Route prefix: `api/babbles`
Auth: `[Authorize]` + `[RequiredScope("access_as_user")]`

#### Endpoints

| Method | Route | Description | Request Body | Response |
|---|---|---|---|---|
| GET | `/api/babbles` | List babbles (paged, filterable, sortable) | Query: `continuationToken`, `pageSize` (1-100), `search` (max 200), `sortBy` (createdAt/title), `sortDirection` (asc/desc), `isPinned` | `PagedResponse<BabbleResponse>` |
| GET | `/api/babbles/search` | Vector/title semantic search | Query: `query` (required, max 200), `topK` (1-50) | `BabbleSearchResponse` |
| GET | `/api/babbles/{id}` | Get single babble | — | `BabbleResponse` or 404 |
| POST | `/api/babbles` | Create babble | `CreateBabbleRequest` | 201 + `BabbleResponse` |
| PUT | `/api/babbles/{id}` | Update babble | `UpdateBabbleRequest` | `BabbleResponse` or 404 |
| PATCH | `/api/babbles/{id}/pin` | Toggle pin | `PinBabbleRequest` | `BabbleResponse` or 404 |
| DELETE | `/api/babbles/{id}` | Delete babble | — | 204 or 404 |
| POST | `/api/babbles/{id}/generate` | Generate prompt (SSE stream) | `GeneratePromptRequest` | text/event-stream |
| POST | `/api/babbles/{id}/generate-title` | AI-generate title from text | — | `BabbleResponse` or 404/502 |
| POST | `/api/babbles/upload` | Upload audio → transcribe → create babble | multipart/form-data: `file`, `title?`, `language?` | 201 + `BabbleResponse` |

#### Request/Response DTOs

**`CreateBabbleRequest`** (`Api/Models/Requests/CreateBabbleRequest.cs`):

```csharp
public sealed record CreateBabbleRequest
{
    public required string Title { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("isPinned")]
    public bool? IsPinned { get; init; }
}
```

**`UpdateBabbleRequest`** (`Api/Models/Requests/UpdateBabbleRequest.cs`):

```csharp
public sealed record UpdateBabbleRequest
{
    public required string Title { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("isPinned")]
    public bool? IsPinned { get; init; }
}
```

**`PinBabbleRequest`** (`Api/Models/Requests/PinBabbleRequest.cs`):

```csharp
public sealed record PinBabbleRequest
{
    [JsonPropertyName("isPinned")]
    public required bool IsPinned { get; init; }
}
```

**`BabbleResponse`** (`Api/Models/Responses/BabbleResponse.cs`):

```csharp
public sealed record BabbleResponse
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
    [JsonPropertyName("isPinned")]
    public required bool IsPinned { get; init; }
}
```

**`BabbleSearchResponse`** (`Api/Models/Responses/BabbleSearchResponse.cs`):

```csharp
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

#### Validation Rules (enforced in controller)

- Title: required, 1-200 chars
- Text: required, 1-50,000 chars
- Tags: max 20 items, each max 50 chars, non-whitespace
- Search query: max 200 chars
- pageSize: clamped 1-100
- topK: clamped 1-50
- Audio upload: max 500 MB, supported formats: MP3, WAV, WebM, OGG, M4A

---

## 4. Babble Service Interface and Implementation

### Interface: `prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs`

```csharp
public interface IBabbleService
{
    Task<(IReadOnlyList<Babble> Items, string? ContinuationToken)> GetByUserAsync(
        string userId, string? continuationToken, int pageSize, string? search,
        string? sortBy, string? sortDirection, bool? isPinned, CancellationToken cancellationToken);
    Task<Babble?> GetByIdAsync(string userId, string babbleId, CancellationToken cancellationToken);
    Task<Babble> CreateAsync(Babble babble, CancellationToken cancellationToken);
    Task<Babble> UpdateAsync(string userId, Babble babble, CancellationToken cancellationToken);
    Task DeleteAsync(string userId, string babbleId, CancellationToken cancellationToken);
    Task<Babble> SetPinAsync(string userId, string babbleId, bool isPinned, CancellationToken cancellationToken);
    Task<IReadOnlyList<BabbleSearchResult>> SearchAsync(string userId, string query, int topN, CancellationToken cancellationToken);
}
```

### Implementation: `prompt-babbler-service/src/Infrastructure/Services/BabbleService.cs`

Key behaviors:

- **Create/Update**: Generates an embedding via `IEmbeddingService`, attaches as `ContentVector`. Falls back to saving without vector on failure.
- **Delete**: Cascade-deletes all `GeneratedPrompt` records for the babble first.
- **Search**: Uses a heuristic to decide search mode:
  - If query has < 3 words AND < 15 chars → title-only text search (avoids embedding API call)
  - Otherwise → parallel title search + vector search, merges/deduplicates by babble ID keeping highest score
- Dependencies: `IBabbleRepository`, `IGeneratedPromptRepository`, `IEmbeddingService`, `ILogger<BabbleService>`

---

## 5. Babble Repository Interface and Implementation

### Interface: `prompt-babbler-service/src/Domain/Interfaces/IBabbleRepository.cs`

```csharp
public interface IBabbleRepository
{
    Task<(IReadOnlyList<Babble> Items, string? ContinuationToken)> GetByUserAsync(...);
    Task<Babble?> GetByIdAsync(string userId, string babbleId, CancellationToken cancellationToken);
    Task<Babble> CreateAsync(Babble babble, CancellationToken cancellationToken);
    Task<Babble> UpdateAsync(Babble babble, CancellationToken cancellationToken);
    Task DeleteAsync(string userId, string babbleId, CancellationToken cancellationToken);
    Task<Babble> SetPinAsync(string userId, string babbleId, bool isPinned, CancellationToken cancellationToken);
    Task<IReadOnlyList<BabbleSearchResult>> SearchByVectorAsync(string userId, ReadOnlyMemory<float> vector, int topN, CancellationToken cancellationToken);
    Task<IReadOnlyList<BabbleSearchResult>> SearchByTitleAsync(string userId, string query, int topN, CancellationToken cancellationToken);
}
```

### Implementation: `prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs`

- Database: `prompt-babbler`
- Container: `babbles`
- Partition key: `userId`
- Minimum similarity score threshold: `0.75`
- **GetByUserAsync**: Builds dynamic SQL with optional `CONTAINS(LOWER(c.title), @search)`, `isPinned` filter, and `ORDER BY` (createdAt/title, ASC/DESC). Uses Cosmos SDK iterator with pagination.
- **GetByIdAsync**: Point-read by (id, partitionKey=userId); returns null on 404.
- **CreateAsync**: `CreateItemAsync` with partition key from `babble.UserId`.
- **UpdateAsync**: `ReplaceItemAsync` by (id, partitionKey=userId).
- **SetPinAsync**: Read → modify `IsPinned` + `UpdatedAt` → replace.
- **DeleteAsync**: Read → delete (throws if not found).
- **SearchByVectorAsync**: Uses Cosmos DB `VectorDistance(c.contentVector, @embedding)` query, filters results below 0.75 similarity post-query.
- **SearchByTitleAsync**: `CONTAINS(LOWER(c.title), @search)` with score 1.0 assigned.
- **VectorSearchResultItem**: Internal sealed record mirroring Babble properties + `SimilarityScore` for deserialization of VectorDistance projection.

---

## 6. Frontend Babble Components

### `prompt-babbler-app/src/components/babbles/BabbleCard.tsx`

Card-style display with title, date (updatedAt), truncated text (150 chars), and tags. Links to `/babble/{id}`.

### `prompt-babbler-app/src/components/babbles/BabbleBubbles.tsx`

Grid layout of BabbleBubbleCard components with pin toggle overlay. Shows title, date, truncated text, tags, and a pin/unpin button on hover.

### `prompt-babbler-app/src/components/babbles/BabbleListItem.tsx`

Compact list-row display with title (link), createdAt date, tags, and pin toggle button. Highlights pinned babbles with border/background accent.

### `prompt-babbler-app/src/components/babbles/BabbleList.tsx`

Simple grid of BabbleCard components with sorting by updatedAt descending, loading spinner, and "Load More" button for pagination.

### `prompt-babbler-app/src/components/babbles/BabbleListSection.tsx`

Full-featured list section with search input (debounced 300ms), sort controls (by createdAt/title, asc/desc), infinite scroll via IntersectionObserver, and renders BabbleListItem components.

### `prompt-babbler-app/src/components/babbles/BabbleEditor.tsx`

Edit form for babble text and tags. Uses Textarea + TagInput (max 20 tags, max 50 chars each). Save/Cancel buttons.

### `prompt-babbler-app/src/components/babbles/DeleteBabbleDialog.tsx`

Confirmation dialog for deleting a babble. Shows babble title in the prompt.

---

## 7. Cosmos DB Container Configuration

### File: `infra/cosmos-babbles-vector-container.bicep`

```bicep
resource babblesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: cosmosDbAccount::database
  name: 'babbles'
  properties: {
    resource: {
      id: 'babbles'
      partitionKey: {
        paths: ['/userId']
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [{ path: '/*' }]
        excludedPaths: [
          { path: '/"_etag"/?' }
          { path: '/contentVector/*' }
        ]
        vectorIndexes: [
          { path: '/contentVector', type: 'quantizedFlat' }
        ]
      }
      vectorEmbeddingPolicy: {
        vectorEmbeddings: [
          {
            path: '/contentVector'
            dataType: 'float32'
            distanceFunction: 'cosine'
            dimensions: 1536
          }
        ]
      }
    }
  }
}
```

#### Configuration Summary

| Setting | Value |
|---|---|
| Container name | `babbles` |
| Partition key | `/userId` (Hash v2) |
| Indexing mode | Consistent, automatic |
| Excluded from index | `_etag`, `/contentVector/*` |
| Vector index | `/contentVector`, type `quantizedFlat` |
| Vector embedding | path `/contentVector`, float32, cosine distance, 1536 dimensions |

---

## Key Discoveries

1. **Babble model is an immutable sealed record** with 9 properties. The `ContentVector` is a 1536-dim float array (Azure OpenAI text-embedding-ada-002 or equivalent) stored directly in Cosmos DB and excluded from regular indexing.
1. **The API never exposes `userId` or `contentVector`** to clients — the response DTO (`BabbleResponse`) strips these fields.
1. **Search is hybrid**: short queries (< 3 words or < 15 chars) use title-only text matching; longer queries run both title search and vector search in parallel, then merge results by highest similarity score.
1. **Cascade delete**: Deleting a babble cascades to all associated `GeneratedPrompt` records.
1. **Frontend has 7 babble components**: BabbleCard, BabbleBubbles, BabbleListItem, BabbleList, BabbleListSection, BabbleEditor, DeleteBabbleDialog.
1. **Cosmos DB uses quantizedFlat vector index** — suitable for moderate-scale datasets; cosine distance function.

## Clarifying Questions

None — all research questions answered through code inspection.
