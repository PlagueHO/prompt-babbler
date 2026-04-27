<!-- markdownlint-disable-file -->
# Release Changes: Cosmos DB Vector Search Infrastructure (Option B Workaround)

**Related Plan**: avm-cosmos-vector-inline-plan.instructions.md
**Implementation Date**: 2026-04-27

## Summary

Wire Cosmos DB vector search infrastructure into `main.bicep` using Option B workaround: standalone `cosmos-babbles-vector-container.bicep` module with `dependsOn` on the AVM Cosmos DB account. AVM 0.19.1 inline approach blocked by upstream MCR publication (tracked in issue #107). Added `EnableNoSQLVectorSearch` account capability.

## Changes

### Added

* GitHub Issue [#107](https://github.com/PlagueHO/prompt-babbler/issues/107) - Tracking issue for replacing standalone vector container with AVM inline vectorEmbeddingPolicy when 0.19.1 publishes

### Modified

* infra/main.bicep - Added `EnableNoSQLVectorSearch` to `capabilitiesToAdd` array (Step 1.2)
* infra/main.bicep - Wired `cosmos-babbles-vector-container.bicep` as module with `dependsOn` on Cosmos DB account (Option B workaround)
* infra/main.bicep - Removed `babbles` container from AVM `sqlDatabases.containers` array (standalone module deploys it instead)
* infra/main.bicep - Added comments linking to tracking issue #107
* infra/main.bicep - Extracted `cosmosDbDatabaseName` variable to replace hardcoded `'prompt-babbler'` strings (review IV-002)
* infra/main.bicep - Passes `tags` to `babblesVectorContainer` module call (review IV-001)
* infra/cosmos-babbles-vector-container.bicep - Added `tags` parameter with default empty object and applied to container resource (review IV-001)

### Removed

* infra/cosmos-babbles-vector-container.json - Deleted stale ARM artifact; Bicep modules reference `.bicep` source not `.json` (review IV-003)

## Additional or Deviating Changes

* AVM 0.19.1 inline approach blocked — used Option B (wire standalone module) instead
  * AVM 0.19.1 is merged to `main` in bicep-registry-modules but not published to MCR (upstream test failure)
  * Upstream: https://github.com/Azure/bicep-registry-modules/issues/6624, https://github.com/Azure/bicep-registry-modules/pull/6711
* Created GitHub issue #107 to track the migration to AVM inline when 0.19.1 publishes

## Release Summary

Total files affected: 2 modified, 1 removed

**Modified files:**
- infra/main.bicep — added EnableNoSQLVectorSearch capability, wired standalone vector container module, removed babbles from AVM containers array, added tracking comments, extracted `cosmosDbDatabaseName` variable, passes `tags` to vector container module
- infra/cosmos-babbles-vector-container.bicep — added `tags` parameter and applied to container resource

**Removed files:**
- infra/cosmos-babbles-vector-container.json — stale ARM artifact

**Tracking artifacts updated:**
- .copilot-tracking/plans/2026-04-27/avm-cosmos-vector-inline-plan.instructions.md — annotated Phase 2 steps
- .copilot-tracking/changes/2026-04-27/avm-cosmos-vector-inline-changes.md — updated title/summary for Option B, added lint validation results

**Validation:**
- `az bicep build --file infra/main.bicep` — PASS (exit 0)
- `az bicep build --file infra/cosmos-babbles-vector-container.bicep` — PASS (exit 0)
- `az bicep lint --file infra/main.bicep` — PASS (exit 0)
- `az bicep lint --file infra/cosmos-babbles-vector-container.bicep` — PASS (exit 0)
