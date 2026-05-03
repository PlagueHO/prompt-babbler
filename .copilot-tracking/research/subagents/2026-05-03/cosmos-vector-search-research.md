# Cosmos DB Vector Search Implementation Research

## Research Topics

1. Existing query patterns in `CosmosBabbleRepository.cs`
1. Vector index configuration from Bicep infrastructure
1. Already-defined response models for search results
1. Cosmos DB `VectorDistance()` SQL syntax and .NET SDK usage
1. Methods needed on `IBabbleRepository` and `IBabbleService`
1. Complete vector search query example for this container

## Findings

### 1. Vector Index Configuration (from Bicep)

Source: `infra/cosmos-babbles-vector-container.bicep`

| Property | Value |
|---|---|
| Container name | `babbles` |
| Database name | (parameter: `databaseName`) |
| Partition key | `/userId` (Hash v2) |
| Vector path | `/contentVector` |
| Vector index type | `quantizedFlat` |
| Data type | `Float32` |
| Distance function | `Cosine` |
| Dimensions | `1536` |
| Excluded from standard index | Yes (`/contentVector/*` in `excludedPaths`) |

The container uses `quantizedFlat` index type, which is suitable for datasets with fewer than ~50,000 vectors per physical partition. For larger scales, `diskANN` would be preferred.

### 2. Existing Query Patterns in CosmosBabbleRepository

Source: `prompt-babbler-service/src/Infrastructure/Services/CosmosBabbleRepository.cs`

**Constants:**

```csharp
public const string DatabaseName = "prompt-babbler";
public const string ContainerName = "babbles";
```

**Container initialization:**

```csharp
_container = cosmosClient.GetContainer(DatabaseName, ContainerName);
```

**Query pattern used (StringBuilder + QueryDefinition with parameters):**

```csharp
var queryText = new StringBuilder("SELECT * FROM c WHERE c.userId = @userId");

// Conditional clauses appended
if (!string.IsNullOrEmpty(search))
    queryText.Append(" AND CONTAINS(LOWER(c.title), @search)");

if (isPinned.HasValue)
    queryText.Append(" AND c.isPinned = @isPinned");

// ORDER BY appended
queryText.Append($" ORDER BY {orderByField} {orderByDirection}");

// QueryDefinition with parameters
var queryDefinition = new QueryDefinition(queryText.ToString())
    .WithParameter("@userId", userId);

// QueryRequestOptions with partition key
var options = new QueryRequestOptions
{
    PartitionKey = new PartitionKey(userId),
    MaxItemCount = pageSize,
};

// FeedIterator pattern
using var iterator = _container.GetItemQueryIterator<Babble>(queryDefinition, continuationToken, options);
```

**Existing methods:**

- `GetByUserAsync` — paginated listing with text search, sort, pin filter
- `GetByIdAsync` — single item read by ID + partition key
- `CreateAsync` — create with partition key
- `UpdateAsync` — replace item
- `SetPinAsync` — update pin status
- `DeleteAsync` — delete item

### 3. Babble Domain Model

Source: `prompt-babbler-service/src/Domain/Models/Babble.cs`

```csharp
public sealed record Babble
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("userId")] public required string UserId { get; init; }
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("text")] public required string Text { get; init; }
    [JsonPropertyName("createdAt")] public required DateTimeOffset CreatedAt { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("updatedAt")] public required DateTimeOffset UpdatedAt { get; init; }
    [JsonPropertyName("isPinned")] public bool IsPinned { get; init; }
}
```

**Note:** The `Babble` model does NOT currently include a `contentVector` property. This will need to be added for vector storage/search:

```csharp
[JsonPropertyName("contentVector")]
public IReadOnlyList<float>? ContentVector { get; init; }
```

### 4. Already-Defined Response Models

Source: `prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs`

```csharp
public sealed record BabbleSearchResult(Babble Babble, double SimilarityScore);
```

Source: `prompt-babbler-service/src/Api/Models/Responses/BabbleSearchResponse.cs`

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

### 5. Existing Embedding Infrastructure

Source: `prompt-babbler-service/src/Domain/Interfaces/IEmbeddingService.cs`

```csharp
public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
```

Source: `prompt-babbler-service/src/Infrastructure/Services/EmbeddingService.cs`

```csharp
public sealed class EmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : IEmbeddingService
{
    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text, CancellationToken cancellationToken = default)
    {
        var embeddings = await embeddingGenerator.GenerateAsync(
            [text], cancellationToken: cancellationToken);
        return embeddings[0].Vector;
    }
}
```

The embedding service uses `Microsoft.Extensions.AI.IEmbeddingGenerator<string, Embedding<float>>` — a generic abstraction that supports Azure OpenAI and other providers.

### 6. Cosmos DB VectorDistance() SQL Syntax

**Function signature:**

```sql
VectorDistance(<vector_expr_1>, <vector_expr_2>, <bool_expr>, <obj_expr>)
```

**Parameters:**

| Parameter | Description |
|---|---|
| `vector_expr_1` | Document vector path (e.g., `c.contentVector`) |
| `vector_expr_2` | Query vector (can be parameterized as `@embedding`) |
| `bool_expr` | Optional. `true` = brute force, `false` = use index (default) |
| `obj_expr` | Optional JSON for overrides: `distanceFunction`, `dataType`, `searchListSizeMultiplier`, `quantizedVectorListMultiplier`, `filterPriority` |

**Return type:** Numeric similarity score (higher = more similar for Cosine).

**Critical rules:**

- Always use `TOP N` in the SELECT to avoid excessive RU consumption
- The vector path must match the `vectorEmbeddingPolicy` path (`/contentVector`)
- Parameters can be passed via `QueryDefinition.WithParameter("@embedding", vectorArray)`

**Basic query pattern:**

```sql
SELECT TOP @topN c.title, VectorDistance(c.contentVector, @embedding) AS SimilarityScore
FROM c
ORDER BY VectorDistance(c.contentVector, @embedding)
```

**With partition key filter (cross-partition not recommended):**

```sql
SELECT TOP @topN c.title, VectorDistance(c.contentVector, @embedding) AS SimilarityScore
FROM c
WHERE c.userId = @userId
ORDER BY VectorDistance(c.contentVector, @embedding)
```

### 7. Methods Needed on IBabbleRepository and IBabbleService

**`IBabbleRepository` — needs a new method:**

```csharp
Task<IReadOnlyList<BabbleSearchResult>> SearchByVectorAsync(
    string userId,
    ReadOnlyMemory<float> queryVector,
    int topN = 10,
    CancellationToken cancellationToken = default);
```

**`IBabbleService` — needs a corresponding method:**

```csharp
Task<IReadOnlyList<BabbleSearchResult>> SearchAsync(
    string userId,
    string queryText,
    int topN = 10,
    CancellationToken cancellationToken = default);
```

The service layer method accepts text (not a vector), uses `IEmbeddingService` to generate the vector, then delegates to the repository.

### 8. Complete Vector Search Query for This Container

**SQL query (parameterized):**

```sql
SELECT TOP @topN
    c.id,
    c.userId,
    c.title,
    c.text,
    c.createdAt,
    c.tags,
    c.updatedAt,
    c.isPinned,
    VectorDistance(c.contentVector, @embedding) AS SimilarityScore
FROM c
WHERE c.userId = @userId
ORDER BY VectorDistance(c.contentVector, @embedding)
```

**C# implementation pattern (following existing repository conventions):**

```csharp
public async Task<IReadOnlyList<BabbleSearchResult>> SearchByVectorAsync(
    string userId,
    ReadOnlyMemory<float> queryVector,
    int topN = 10,
    CancellationToken cancellationToken = default)
{
    var queryText = @"
        SELECT TOP @topN
            c.id,
            c.userId,
            c.title,
            c.text,
            c.createdAt,
            c.tags,
            c.updatedAt,
            c.isPinned,
            VectorDistance(c.contentVector, @embedding) AS SimilarityScore
        FROM c
        WHERE c.userId = @userId
        ORDER BY VectorDistance(c.contentVector, @embedding)";

    var queryDefinition = new QueryDefinition(queryText)
        .WithParameter("@topN", topN)
        .WithParameter("@userId", userId)
        .WithParameter("@embedding", queryVector.ToArray());

    var options = new QueryRequestOptions
    {
        PartitionKey = new PartitionKey(userId),
    };

    var results = new List<BabbleSearchResult>();
    using var iterator = _container.GetItemQueryIterator<VectorSearchResultItem>(
        queryDefinition, requestOptions: options);

    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync(cancellationToken);
        foreach (var item in response)
        {
            var babble = new Babble
            {
                Id = item.Id,
                UserId = item.UserId,
                Title = item.Title,
                Text = item.Text,
                CreatedAt = item.CreatedAt,
                Tags = item.Tags,
                UpdatedAt = item.UpdatedAt,
                IsPinned = item.IsPinned,
            };
            results.Add(new BabbleSearchResult(babble, item.SimilarityScore));
        }
    }

    return results.AsReadOnly();
}
```

**Note:** A private record type is needed for deserialization:

```csharp
private sealed record VectorSearchResultItem
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("userId")] public required string UserId { get; init; }
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("text")] public required string Text { get; init; }
    [JsonPropertyName("createdAt")] public required DateTimeOffset CreatedAt { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("updatedAt")] public required DateTimeOffset UpdatedAt { get; init; }
    [JsonPropertyName("isPinned")] public bool IsPinned { get; init; }
    [JsonPropertyName("SimilarityScore")] public double SimilarityScore { get; init; }
}
```

Alternatively, the query can use `SELECT TOP @topN *` and deserialize the Babble directly, then retrieve the score separately — but projecting all fields and the score together is cleaner.

## Key Implementation Notes

1. **`contentVector` on Babble model** — the `Babble` record needs a `ContentVector` property added. It should be nullable (`IReadOnlyList<float>?`) because existing documents won't have it, and the vector should be excluded from API responses.

1. **Embedding generation on write** — when a babble is created or text is updated, the embedding must be generated and stored in `contentVector`. This should happen in the service layer before calling the repository.

1. **Partition key scoping** — the query includes `WHERE c.userId = @userId` and uses `PartitionKey` in request options. This confines vector search to a single user's babbles (single logical partition), which is efficient for `quantizedFlat` index type.

1. **TOP N is mandatory** — Cosmos DB requires `TOP N` for vector search queries to avoid full scans and excessive RU cost.

1. **Parameter passing** — the vector is passed as `@embedding` parameter (a `float[]`). The .NET SDK handles serialization of float arrays into the JSON array format Cosmos DB expects.

1. **No continuation token** — vector search results are typically small (TOP 10-20) so pagination is not necessary for the search endpoint.

## References

- [VectorDistance function reference](https://learn.microsoft.com/azure/cosmos-db/nosql/query/vectordistance)
- [Vector search in Azure Cosmos DB for NoSQL](https://learn.microsoft.com/azure/cosmos-db/vector-search)
- [Index and query vectors in .NET](https://learn.microsoft.com/azure/cosmos-db/how-to-dotnet-vector-index-query)
- [Index vector data in Cosmos DB](https://learn.microsoft.com/cosmos-db/index-vector-data)

## Clarifying Questions

1. Should the vector search be cross-partition (all users) or always scoped to a single user's babbles? — **Answered:** The existing model partitions by `userId`, so search should be scoped per-user.
1. Should a minimum similarity score threshold be applied (e.g., `WHERE VectorDistance(...) > 0.5`)? — This is a design decision not determinable from the codebase alone.
1. Should `contentVector` be excluded from API responses when returning babble details? — Likely yes, to avoid sending 1536 floats to the frontend.
