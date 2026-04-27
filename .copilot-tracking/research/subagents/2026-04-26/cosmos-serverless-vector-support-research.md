# Cosmos DB NoSQL Serverless — Vector Search Support Research

## Status: Complete

## Research Questions

1. Does Cosmos DB NoSQL Serverless support vector search?
1. Does Cosmos DB NoSQL Serverless support the `quantizedFlat` vector index type?
1. Does Cosmos DB NoSQL Serverless support `VectorDistance()` function in queries?
1. Are there any limitations of vector search in Serverless vs Provisioned mode?
1. Does Cosmos DB NoSQL Serverless support Full Text Search?
1. Does Cosmos DB NoSQL Serverless support hybrid search (`ORDER BY RANK RRF(...)`)?

## Context

- Project uses: `Microsoft.Azure.Cosmos` 3.58.0, Cosmos DB NoSQL, Serverless capacity
- Bicep uses AVM module `document-db/database-account:0.19.0` with `EnableServerless` capability
- Plan calls for `text-embedding-3-small` (1536 dimensions, Float32, Cosine distance) with `quantizedFlat` index

## Answer Summary

| # | Question | Answer |
|---|---------|--------|
| 1 | Does Serverless support vector search? | **YES** |
| 2 | Does Serverless support `quantizedFlat` index? | **YES** |
| 3 | Does Serverless support `VectorDistance()`? | **YES** |
| 4 | Any Serverless vs Provisioned limitations? | **Minor — see details** |
| 5 | Does Serverless support Full Text Search? | **YES** |
| 6 | Does Serverless support hybrid search (RRF)? | **YES** |

**Bottom line: No capacity mode change is required. The project's Serverless configuration is fully compatible with all planned search features.**

## Detailed Findings

### 1. Vector Search on Serverless — YES

Microsoft explicitly markets Cosmos DB NoSQL as a serverless vector database. The vector database documentation states:

> "Azure Cosmos DB for NoSQL is the world's first serverless NoSQL vector database."

The multitenancy for vector search page states:

> "Azure Cosmos DB stands out as the world's first full-featured serverless operational database with vector search."

The serverless documentation confirms:

> "Any container that's created in a serverless account is a serverless container. Serverless containers have the same capabilities as containers that are created in a provisioned throughput account type."

The only capacity mode restriction in the vector search "Current limitations" section is:

> "At this time, vector indexing and search aren't supported on accounts with **Shared Throughput**."

"Shared Throughput" refers to provisioned throughput databases where multiple containers share an allocated RU/s budget. This is NOT the same as Serverless. Serverless is a separate account-level capacity mode where throughput is consumed per-request with no provisioning.

The feature is enabled by adding the `EnableNoSQLVectorSearch` capability to the account, which can co-exist with `EnableServerless`.

### 2. `quantizedFlat` Index on Serverless — YES

No capacity mode restriction exists for any vector index type. The `quantizedFlat` index type is available on all account types that support vector search (all except Shared Throughput).

Relevant constraints for `quantizedFlat` (apply to ALL capacity modes):

- Maximum 4,096 dimensions (project uses 1,536 — well within limit)
- Requires at least 1,000 vectors inserted for accurate quantization; uses full-scan fallback below that threshold (higher RU cost for small datasets)
- Recommended when search scope is 50,000 vectors or fewer

### 3. `VectorDistance()` Function on Serverless — YES

The `VectorDistance()` system function is available in all Cosmos DB NoSQL query contexts where vector search is enabled. No capacity mode restriction is documented. The function supports cosine, dot product, and euclidean distance metrics.

### 4. Serverless vs Provisioned Limitations

There are no vector-search-specific limitations unique to Serverless. The general Serverless account constraints apply:

| Constraint | Serverless | Provisioned |
|-----------|-----------|-------------|
| Max regions | 1 | Unlimited |
| Max logical partition storage | 20 GB | 20 GB |
| Max databases + containers | 500 | 500 |
| Burst capacity | Not available | Available |
| Throughput buckets | Not available | Available |
| Reserved capacity pricing | Not available | Available |
| Multi-region writes | Not available | Available |
| Shared throughput databases | Not available | Available |

Vector search limitations that apply to ALL capacity modes equally:

- `quantizedFlat` and `diskANN` require 1,000+ vectors for accurate quantization
- `flat` index: max 505 dimensions; `quantizedFlat`/`diskANN`: max 4,096 dimensions
- Large ingestion (5M+ vectors) may need more index build time
- Not supported on Shared Throughput databases
- Once enabled on a container, cannot be disabled

### 5. Full Text Search on Serverless — YES

The full text search documentation does not mention any capacity mode restrictions. The feature requires:

1. Enabling the feature on the account
1. Defining a full-text policy on the container
1. Adding full-text indexes to the indexing policy

Supported functions: `FullTextContains`, `FullTextContainsAll`, `FullTextContainsAny`, `FullTextScore` (BM25 scoring in `ORDER BY RANK` clause).

No exclusion for Serverless capacity mode is documented.

### 6. Hybrid Search on Serverless — YES

Hybrid search combines vector search and full text search using `ORDER BY RANK RRF(VectorDistance(...), FullTextScore(...))`. The documentation does not mention any capacity mode restrictions. Since both vector search and full text search are supported on Serverless, hybrid search is also supported.

Weighted RRF is also available: `ORDER BY RANK RRF(VectorDistance(...), FullTextScore(...), [2, 1])`.

## Project Compatibility Assessment

| Project Requirement | Supported on Serverless? | Notes |
|--------------------|-------------------------|-------|
| `text-embedding-3-small` (1536 dims) | YES | Max is 4,096 for `quantizedFlat` |
| Float32 data type | YES | Also supports float16, int8, uint8 |
| Cosine distance function | YES | Default distance function |
| `quantizedFlat` index | YES | Best for < 50K vectors per search scope |
| `VectorDistance()` queries | YES | Use `TOP N` clause always |
| Full text search | YES | Requires full-text policy + index |
| Hybrid search (RRF) | YES | Combines vector + full-text |
| `EnableServerless` + `EnableNoSQLVectorSearch` | YES | Capabilities are independent |

## Conclusion

**No changes to the capacity mode or architecture are required.** The Cosmos DB NoSQL Serverless capacity mode fully supports:

- Vector search with all index types (`flat`, `quantizedFlat`, `diskANN`)
- `VectorDistance()` system function
- Full text search with BM25 scoring
- Hybrid search with RRF ranking

The only unsupported configuration for vector search is **Shared Throughput databases** (provisioned throughput mode where multiple containers share RU/s), which is not applicable to Serverless accounts.

## References

- [Vector search in Azure Cosmos DB for NoSQL](https://learn.microsoft.com/azure/cosmos-db/vector-search) — Main vector search documentation with limitations section
- [Azure Cosmos DB serverless account type](https://learn.microsoft.com/azure/cosmos-db/serverless) — Serverless capabilities: "same capabilities as provisioned throughput"
- [Vector database — Azure Cosmos DB](https://learn.microsoft.com/azure/cosmos-db/vector-database) — "world's first serverless NoSQL vector database"
- [Multitenancy for vector search](https://learn.microsoft.com/azure/cosmos-db/multi-tenancy-vector-search) — "world's first full-featured serverless operational database with vector search"
- [Full-text search in Azure Cosmos DB for NoSQL](https://learn.microsoft.com/azure/cosmos-db/gen-ai/full-text-search) — Full text search docs (no capacity mode restrictions)
- [Hybrid search in Azure Cosmos DB for NoSQL](https://learn.microsoft.com/azure/cosmos-db/gen-ai/hybrid-search) — Hybrid search with RRF (no capacity mode restrictions)
- [Azure Cosmos DB service quotas and default limits](https://learn.microsoft.com/azure/cosmos-db/concepts-limits) — Serverless-specific limits section
- [Choose an Azure service for vector search](https://learn.microsoft.com/azure/architecture/guide/technology-choices/vector-search) — Capability matrix for Cosmos DB NoSQL vector search
