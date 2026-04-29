<!-- markdownlint-disable-file -->
# Implementation Quality Validation: Inline Cosmos DB Vector Config

**Date**: 2026-04-27
**Scope**: full-quality
**Status**: Passed (0 Critical, 0 Major, 4 Minor)

## Findings by Category

### Correctness

* **[Minor] IV-001**: The `babblesVectorContainer` module does not receive or apply `tags` from the parent template, while all other resources propagate the `tags` variable. The babbles container will lack `azd-env-name` and `project` tags. Evidence: `cosmos-babbles-vector-container.bicep` has no `tags` parameter; `main.bicep` lines 458-463 does not pass `tags`.

* **[Minor] IV-002**: The `databaseName` parameter is hardcoded as `'prompt-babbler'` at `main.bicep` line 462 rather than derived from a shared variable. If the database name changes in the AVM containers array, the standalone module would diverge.

### Maintainability

* **[Minor] IV-003**: `infra/cosmos-babbles-vector-container.json` is a stale compiled ARM template not referenced by any deployment pipeline. Will become stale if the `.bicep` source is modified. Should be deleted.

### Risk

* **[Minor] IV-004**: Frontend `SearchCommand.tsx` does not pass `shouldFilter={false}` to cmdk, which may filter out valid semantic search results client-side. Not directly infrastructure scope but noted for completeness.

### Deployment Chain

Verified correct: `rg` → `cosmosDbAccount` → `babblesVectorContainer`. No duplicate container risk — babbles removed from AVM array and only defined in standalone module.

### Documentation

Excellent — inline comments explain the workaround, link to tracking issue #107, and reference upstream blockers.

## Summary

| Severity | Count |
|---|---|
| Critical | 0 |
| Major | 0 |
| Minor | 4 |
