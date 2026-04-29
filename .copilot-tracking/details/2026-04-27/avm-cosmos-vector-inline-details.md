<!-- markdownlint-disable-file -->
# Implementation Details: Inline Cosmos DB Vector Config via AVM 0.19.1 Upgrade

## Context Reference

Sources: .copilot-tracking/research/2026-04-27/avm-cosmos-vector-inline-research.md, conversation review of infra/main.bicep and infra/cosmos-babbles-vector-container.bicep

## Implementation Phase 1: Upgrade AVM and Inline Vector Config

<!-- parallelizable: false -->

### Step 1.1: Bump AVM module version from 0.19.0 to 0.19.1

Change the module reference tag on the Cosmos DB account module declaration.

Files:
- infra/main.bicep — Line 365: change `0.19.0` to `0.19.1`

Discrepancy references:
- None

Success criteria:
- Module reference reads `br/public:avm/res/document-db/database-account:0.19.1`

Context references:
- .copilot-tracking/research/2026-04-27/avm-cosmos-vector-inline-research.md (Lines 136-142) — No breaking changes confirmed

Dependencies:
- None

### Step 1.2: Add `EnableNoSQLVectorSearch` to `capabilitiesToAdd`

Add the vector search capability alongside the existing `EnableServerless` capability. This is an account-level capability required for vector search on any container.

Files:
- infra/main.bicep — Lines 375-377: expand the `capabilitiesToAdd` array

Replace:
```bicep
    capabilitiesToAdd: [
      'EnableServerless'
    ]
```

With:
```bicep
    capabilitiesToAdd: [
      'EnableServerless'
      'EnableNoSQLVectorSearch'
    ]
```

Discrepancy references:
- DD-01: This was claimed as done in the changes log but was never actually applied

Success criteria:
- `capabilitiesToAdd` array contains both `'EnableServerless'` and `'EnableNoSQLVectorSearch'`

Context references:
- .copilot-tracking/research/2026-04-27/avm-cosmos-vector-inline-research.md (Lines 63-80) — Capability confirmed as allowed value

Dependencies:
- None

### Step 1.3: Add `indexingPolicy` and `vectorEmbeddingPolicy` to the `babbles` container

Expand the minimal `babbles` container entry to include full vector search configuration. The indexing policy excludes the vector field from standard indexing and defines a `quantizedFlat` vector index. The vector embedding policy declares the `/contentVector` path with Float32, Cosine distance, and 1536 dimensions matching `text-embedding-3-small`.

Files:
- infra/main.bicep — Lines 427-432: expand the `babbles` container object

Replace:
```bicep
          {
            name: 'babbles'
            paths: [
              '/userId'
            ]
          }
```

With:
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

Discrepancy references:
- DD-01: Replaces the orphaned workaround approach with AVM-native inline config

Success criteria:
- `babbles` container entry includes `indexingPolicy` with `vectorIndexes` array
- `babbles` container entry includes `vectorEmbeddingPolicy` with `vectorEmbeddings` array
- `/contentVector/*` path is in `excludedPaths`
- Configuration matches the existing `cosmos-babbles-vector-container.bicep` exactly

Context references:
- infra/cosmos-babbles-vector-container.bicep (Lines 20-64) — Source vector config to replicate
- .copilot-tracking/research/2026-04-27/avm-cosmos-vector-inline-research.md (Lines 82-126) — Confirmed parameter schema

Dependencies:
- Step 1.1 (AVM 0.19.1 required for `vectorEmbeddingPolicy` and `indexingPolicy.vectorIndexes` parameters)

## Implementation Phase 2: Remove Orphaned Files

<!-- parallelizable: true -->

### Step 2.1: Delete `infra/cosmos-babbles-vector-container.bicep`

Delete the orphaned Bicep file. It is not referenced from `main.bicep` or any other file (verified by workspace-wide grep).

Files:
- infra/cosmos-babbles-vector-container.bicep — DELETE entire file

Discrepancy references:
- DR-01: File was created as AVM workaround but never wired into deployment

Success criteria:
- File no longer exists at `infra/cosmos-babbles-vector-container.bicep`

Context references:
- .copilot-tracking/research/2026-04-27/avm-cosmos-vector-inline-research.md (Lines 194-210) — Orphan assessment

Dependencies:
- Phase 1 completion (inline config must be in place before removing the workaround)

### Step 2.2: Delete `infra/cosmos-babbles-vector-container.json`

Delete the compiled ARM template corresponding to the orphaned Bicep file.

Files:
- infra/cosmos-babbles-vector-container.json — DELETE entire file

Discrepancy references:
- DR-01: Same as Step 2.1

Success criteria:
- File no longer exists at `infra/cosmos-babbles-vector-container.json`

Context references:
- .copilot-tracking/research/2026-04-27/avm-cosmos-vector-inline-research.md (Lines 194-210) — Orphan assessment

Dependencies:
- Step 2.1 (delete together)

## Implementation Phase 3: Update Changes Log

<!-- parallelizable: true -->

### Step 3.1: Update `babble-search-semantic-changes.md` to reflect corrected infrastructure

The changes log currently states "Added EnableNoSQLVectorSearch capability and cosmos-babbles-vector-container module reference" under the `infra/main.bicep` Modified entry. Update this to accurately describe the AVM 0.19.1 upgrade with inline vector config.

Files:
- .copilot-tracking/changes/2026-04-26/babble-search-semantic-changes.md — Update the `infra/main.bicep` entry in Modified section and move `cosmos-babbles-vector-container.bicep` to Removed section

Changes to make:
1. In the **Added** section: Remove any reference to `cosmos-babbles-vector-container.bicep` if present
2. In the **Modified** section: Update the `infra/main.bicep` line to read: "Upgraded AVM document-db/database-account from 0.19.0 to 0.19.1, added EnableNoSQLVectorSearch capability, inlined vectorEmbeddingPolicy and vectorIndexes on babbles container"
3. In the **Removed** section: Add entries for `infra/cosmos-babbles-vector-container.bicep` and `infra/cosmos-babbles-vector-container.json`
4. Update the **Release Summary** file counts to reflect removals

Discrepancy references:
- DD-02: Changes log previously described infrastructure that was not actually deployed

Success criteria:
- Changes log accurately reflects AVM upgrade and inline vector configuration
- Orphaned files listed under Removed section
- File counts updated

Context references:
- .copilot-tracking/changes/2026-04-26/babble-search-semantic-changes.md (full file) — Current inaccurate entries

Dependencies:
- Phase 1 and Phase 2 completion

## Implementation Phase 4: Validation

<!-- parallelizable: false -->

### Step 4.1: Run Bicep build validation

Execute the Bicep build to verify the modified `main.bicep` compiles correctly with AVM 0.19.1:
- `az bicep build --file infra/main.bicep`

### Step 4.2: Run Bicep lint validation

Execute Bicep linting to catch style and best-practice violations:
- `az bicep lint --file infra/main.bicep`

### Step 4.3: Fix minor validation issues

Iterate on Bicep errors and warnings. Apply fixes directly when corrections are straightforward and isolated.

### Step 4.4: Report blocking issues

When validation failures require changes beyond minor fixes:
- Document the issues and affected files
- Provide the user with next steps
- Recommend additional research and planning rather than inline fixes
- Avoid large-scale refactoring within this phase

## Dependencies

- Azure CLI with Bicep extension
- Network access to Microsoft Container Registry for AVM module pull

## Success Criteria

- `az bicep build --file infra/main.bicep` succeeds
- `az bicep lint --file infra/main.bicep` passes without errors
- No orphaned vector container files remain in `infra/`
- Changes log accurately reflects the implemented infrastructure
