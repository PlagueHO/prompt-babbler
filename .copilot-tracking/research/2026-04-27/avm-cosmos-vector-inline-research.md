# AVM Cosmos DB `document-db/database-account` 0.19.1 — Vector Inline Research

## Research Questions

1. Does AVM 0.19.1 `containers` array accept `vectorEmbeddingPolicy`, `indexingPolicy` (including `vectorIndexes`), and `fullTextPolicy`?
1. Is `EnableNoSQLVectorSearch` accepted in `capabilitiesToAdd`?
1. What is the exact parameter shape for inline vector config?
1. Are there breaking changes between 0.19.0 and 0.19.1?
1. What gaps exist in the current `main.bicep`?
1. Is the orphaned `cosmos-babbles-vector-container.bicep` safe to delete?

---

## Findings

### 1. AVM 0.19.1 Container Parameters — Confirmed

Version 0.19.1 CHANGELOG explicitly states:

> Added support for `vectorEmbeddingPolicy` and `fullTextPolicy` on SQL containers, and `fullTextIndexes` within `indexingPolicy`

The `containerType` exported from `sql-database/main.bicep` includes:

| Parameter | Type | Required |
| --- | --- | --- |
| `name` | `string` | Yes |
| `paths` | `string[]` | Yes |
| `indexingPolicy` | `resourceInput<...containers@2025-04-15>.properties.resource.indexingPolicy` | No |
| `vectorEmbeddingPolicy` | `resourceInput<...containers@2025-04-15>.properties.resource.vectorEmbeddingPolicy` | No |
| `fullTextPolicy` | `resourceInput<...containers@2025-04-15>.properties.resource.fullTextPolicy` | No |
| `kind` | `'Hash' \| 'MultiHash'` | No |
| `version` | `1 \| 2` | No |
| `defaultTtl` | `int` | No |
| `throughput` | `int` | No |
| `autoscaleSettingsMaxThroughput` | `int` | No |
| `analyticalStorageTtl` | `int` | No |
| `conflictResolutionPolicy` | object | No |
| `uniqueKeyPolicyKeys` | array | No |
| `tags` | object | No |

The `sql-database/main.bicep` forwards all three new parameters to the container child module:

```bicep
vectorEmbeddingPolicy: container.?vectorEmbeddingPolicy
fullTextPolicy: container.?fullTextPolicy
indexingPolicy: container.?indexingPolicy
```

The `container/main.bicep` spreads them onto the resource:

```bicep
...(vectorEmbeddingPolicy != null ? { vectorEmbeddingPolicy: vectorEmbeddingPolicy } : {})
...(fullTextPolicy != null ? { fullTextPolicy: fullTextPolicy } : {})
indexingPolicy: indexingPolicy
```

**Conclusion**: All three parameters (`vectorEmbeddingPolicy`, `indexingPolicy` with `vectorIndexes`, `fullTextPolicy`) are fully supported in AVM 0.19.1 at the container level.

### 2. `EnableNoSQLVectorSearch` Capability — Confirmed

The `main.bicep` `capabilitiesToAdd` parameter is typed as `string[]` with `@allowed`:

```bicep
@allowed([
  'EnableCassandra'
  'EnableTable'
  'EnableGremlin'
  'EnableMongo'
  'DisableRateLimitingResponses'
  'EnableServerless'
  'EnableNoSQLVectorSearch'
  'EnableNoSQLFullTextSearch'
  'EnableMaterializedViews'
  'DeleteAllItemsByPartitionKey'
])
param capabilitiesToAdd string[]?
```

`EnableNoSQLVectorSearch` is an allowed value. The current `main.bicep` only passes `['EnableServerless']` — adding `'EnableNoSQLVectorSearch'` is valid.

### 3. Parameter Schema for Inline Vector Config

The types are derived from `resourceInput<'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2025-04-15'>`, meaning they match the ARM API schema exactly. Based on the existing `cosmos-babbles-vector-container.bicep` and the AVM parameter passthrough:

#### `vectorEmbeddingPolicy`

```bicep
vectorEmbeddingPolicy: {
  vectorEmbeddings: [
    {
      path: '/contentVector'
      dataType: 'Float32'
      distanceFunction: 'Cosine'
      dimensions: 1536
    }
  ]
}
```

#### `indexingPolicy` (with `vectorIndexes` and `excludedPaths`)

```bicep
indexingPolicy: {
  indexingMode: 'consistent'
  automatic: true
  includedPaths: [
    {
      path: '/*'
    }
  ]
  excludedPaths: [
    {
      path: '/"_etag"/?'
    }
    {
      path: '/contentVector/*'
    }
  ]
  vectorIndexes: [
    {
      path: '/contentVector'
      type: 'quantizedFlat'
    }
  ]
}
```

**Key points**:

- Vector field paths (`/contentVector/*`) must be in `excludedPaths` so the standard index does not index the embedding array
- `vectorIndexes` sits inside `indexingPolicy`, not at the container level
- The `type` can be `quantizedFlat`, `flat`, or `diskANN` depending on workload (the project currently uses `quantizedFlat`)

### 4. Breaking Changes Between 0.19.0 and 0.19.1

From the CHANGELOG:

> **0.19.1 — Breaking Changes: None**

The 0.19.1 release is additive only. It adds optional parameters (`vectorEmbeddingPolicy`, `fullTextPolicy`, `fullTextIndexes` in `indexingPolicy`) with no renames, removals, or defaults changes.

All existing parameters used in the project's `main.bicep` (capabilities, sqlDatabases, containers with `name`/`paths`, private endpoints, diagnostic settings, network restrictions, `enableBurstCapacity`, `disableLocalAuthentication`, `disableKeyBasedMetadataWriteAccess`, `zoneRedundant`) are unchanged.

**Note**: The API version used internally by the module changed from `2024-11-15` to `2025-04-15`. This is transparent to consumers — the module handles it internally.

### 5. Existing `main.bicep` Gaps

Current state at `infra/main.bicep` lines 365–457:

| Gap | Current Value | Required Value |
| --- | --- | --- |
| Module version | `0.19.0` | `0.19.1` |
| `capabilitiesToAdd` | `['EnableServerless']` | `['EnableServerless', 'EnableNoSQLVectorSearch']` |
| `babbles` container | `{ name: 'babbles', paths: ['/userId'] }` | Needs `indexingPolicy` and `vectorEmbeddingPolicy` added inline |

The exact inline `babbles` container should be:

```bicep
{
  name: 'babbles'
  paths: [
    '/userId'
  ]
  indexingPolicy: {
    indexingMode: 'consistent'
    automatic: true
    includedPaths: [
      {
        path: '/*'
      }
    ]
    excludedPaths: [
      {
        path: '/"_etag"/?'
      }
      {
        path: '/contentVector/*'
      }
    ]
    vectorIndexes: [
      {
        path: '/contentVector'
        type: 'quantizedFlat'
      }
    ]
  }
  vectorEmbeddingPolicy: {
    vectorEmbeddings: [
      {
        path: '/contentVector'
        dataType: 'Float32'
        distanceFunction: 'Cosine'
        dimensions: 1536
      }
    ]
  }
}
```

### 6. Orphaned File Assessment

#### `cosmos-babbles-vector-container.bicep`

- Not referenced from `main.bicep` or any other Bicep file in `infra/`
- Not referenced in `azure.yaml` or any deployment hook
- A `grep_search` across the entire workspace for `cosmos-babbles-vector-container` found zero references in infra files — only references in `.copilot-tracking/` review/change documents
- The file defines a raw `Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15` resource with vector policies — this was a workaround for AVM 0.19.0 lacking vector support

#### `cosmos-babbles-vector-container.json`

- The compiled ARM template for the above Bicep file
- Not referenced anywhere in the codebase

**Both files are safe to delete** once the AVM module is upgraded to 0.19.1 with inline vector configuration.

---

## Risks and Caveats

1. **Vector embedding policy immutability**: Cosmos DB vector embedding policies are immutable after container creation. If the `babbles` container was previously created without a vector policy (by the AVM module at 0.19.0), it cannot be retroactively modified. The container must be **deleted and recreated** (or a new environment deployed) for the vector policy to take effect. This is a deployment concern, not a Bicep concern.

1. **Duplicate container risk**: The current `main.bicep` already defines a `babbles` container in the `sqlDatabases.containers` array (without vector config). The orphaned `cosmos-babbles-vector-container.bicep` was intended to redeploy it with vector settings, but since it is never called, the container exists without vector support. The fix is to inline the vector config on the existing `babbles` entry and delete the orphaned file.

1. **API version change**: AVM 0.19.1 internally uses API version `2025-04-15` (vs `2024-11-15` in 0.19.0). This is transparent but worth noting for debugging ARM template errors.

1. **`EnableNoSQLVectorSearch` is an account-level capability**: Once enabled on a serverless account, it cannot be removed. This is expected and necessary for vector search.

---

## Summary

Upgrading from AVM `document-db/database-account` 0.19.0 to 0.19.1 enables inlining vector configuration directly on the `babbles` container. The required changes are:

1. Update module version tag from `0.19.0` to `0.19.1`
1. Add `'EnableNoSQLVectorSearch'` to `capabilitiesToAdd`
1. Add `indexingPolicy` and `vectorEmbeddingPolicy` to the `babbles` container entry
1. Delete `infra/cosmos-babbles-vector-container.bicep` and `infra/cosmos-babbles-vector-container.json`

There are no breaking changes. All parameter shapes are confirmed from the AVM source.
