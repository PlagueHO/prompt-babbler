<!-- markdownlint-disable-file -->
# Implementation Quality Validation: Babble Semantic Search

**Status:** Failed (1 Critical, 3 Major, 5 Minor)

## Critical

### IV-001 — Bicep duplicate `babbles` container blocks deployment

The `babbles` container is defined in both the AVM module's `sqlDatabases.containers` array at `infra/main.bicep` (without vector policy) and in `infra/cosmos-babbles-vector-container.bicep` (with vector policy). Cosmos DB vector embedding policies are immutable after creation — AVM deploys the container first without the policy, then the vector module fails.

**Fix:** Remove the `babbles` entry from the AVM module's `sqlDatabases.containers` array.

## Major

### IV-002 — `useSemanticSearch` race condition (abort signal disconnected)

The hook creates an `AbortController` and calls `abort()`, but the signal is never passed to `searchBabbles` or `fetch`. Stale responses can overwrite correct ones.

**Fix:** Thread `AbortSignal` through `searchBabbles` → `fetchJson` → `fetch`.

### IV-003 — Embedding failure blocks babble creation/update

`BabbleService.CreateAsync` and `UpdateAsync` call embedding generation with no try/catch. If Azure OpenAI is unavailable, all babble CRUD is blocked.

**Fix:** Wrap in try/catch with `ContentVector = null` fallback, or document as intentional.

### IV-004 — Text search fallback ignores `topK`

`BabbleService.SearchAsync` calls `GetByUserAsync` with default `pageSize=20` regardless of user's `topK` value.

**Fix:** Pass `topK` as `pageSize` argument.

## Minor

| ID | Summary |
|---|---|
| IV-005 | Text search path loads `contentVector` (~6KB/doc) unnecessarily |
| IV-006 | `SearchCommand` ignores `error` state from hook — silent failure |
| IV-007 | Dialog doesn't reset query on close — stale results persist |
| IV-008 | `Header.tsx` uses synthetic `dispatchEvent` for dialog trigger |
| IV-009 | No boundary test for text/vector routing at exactly 15 chars with 2 words |
