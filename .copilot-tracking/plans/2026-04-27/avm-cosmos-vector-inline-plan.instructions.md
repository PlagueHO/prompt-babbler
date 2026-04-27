---
applyTo: '.copilot-tracking/changes/2026-04-27/avm-cosmos-vector-inline-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: Inline Cosmos DB Vector Config via AVM 0.19.1 Upgrade

## Overview

Eliminate the orphaned `cosmos-babbles-vector-container.bicep` workaround by upgrading the AVM `document-db/database-account` module from 0.19.0 to 0.19.1, inlining vector embedding policy and vector indexes on the `babbles` container, and adding the `EnableNoSQLVectorSearch` account capability.

## Objectives

### User Requirements

- Remove the separate `cosmos-babbles-vector-container.bicep` file and use AVM inline vector support instead — Source: user review feedback (conversation context)
- Ensure Cosmos DB vector search infrastructure is correctly wired in the deployed template — Source: user review finding that current `main.bicep` is missing both the capability and the vector container reference

### Derived Objectives

- Upgrade AVM module to 0.19.1 which added `vectorEmbeddingPolicy` and `indexingPolicy.vectorIndexes` support — Derived from: AVM 0.19.0 lacks these parameters, making inline impossible
- Add `EnableNoSQLVectorSearch` to `capabilitiesToAdd` — Derived from: required account-level capability for vector search, currently missing
- Delete orphaned `cosmos-babbles-vector-container.json` ARM template alongside the `.bicep` file — Derived from: compiled ARM template is also unreferenced
- Update the semantic search changes log to reflect the corrected infrastructure approach — Derived from: changes log inaccurately claims these changes were already made

## Context Summary

### Project Files

- infra/main.bicep (Lines 363-449) — Cosmos DB account module using AVM 0.19.0; `babbles` container at lines 427-432 has only `name` and `paths`
- infra/cosmos-babbles-vector-container.bicep — Orphaned workaround file with raw ARM resource for vector-enabled `babbles` container; never referenced
- infra/cosmos-babbles-vector-container.json — Compiled ARM template of the above; never referenced
- .copilot-tracking/changes/2026-04-26/babble-search-semantic-changes.md — Changes log that inaccurately claims vector infra was wired

### References

- .copilot-tracking/research/2026-04-27/avm-cosmos-vector-inline-research.md — Primary research confirming AVM 0.19.1 parameter schema and no breaking changes
- https://github.com/Azure/bicep-registry-modules/blob/main/avm/res/document-db/database-account/CHANGELOG.md — AVM CHANGELOG confirming 0.19.1 additions

### Standards References

- AGENTS.md — Bicep lint validation via `az bicep build`; CI pipeline runs `bicep lint ./infra/main.bicep`

## Implementation Checklist

### [ ] Implementation Phase 1: Upgrade AVM and Inline Vector Config

<!-- parallelizable: false -->

- [ ] Step 1.1: Bump AVM module version from 0.19.0 to 0.19.1 — **BLOCKED: AVM 0.19.1 does not exist in MCR**
  - Details: .copilot-tracking/details/2026-04-27/avm-cosmos-vector-inline-details.md (Lines 10-24)
- [x] Step 1.2: Add `EnableNoSQLVectorSearch` to `capabilitiesToAdd`
  - Details: .copilot-tracking/details/2026-04-27/avm-cosmos-vector-inline-details.md (Lines 26-42)
- [ ] Step 1.3: Add `indexingPolicy` and `vectorEmbeddingPolicy` to the `babbles` container — **BLOCKED: AVM 0.19.0 does not support vectorEmbeddingPolicy**
  - Details: .copilot-tracking/details/2026-04-27/avm-cosmos-vector-inline-details.md (Lines 44-96)

### [ ] Implementation Phase 2: Remove Orphaned Files

<!-- parallelizable: true -->

- [x] Step 2.1: ~~Delete `infra/cosmos-babbles-vector-container.bicep`~~ — **Skipped by design**: file is actively used by Option B workaround
  - Details: .copilot-tracking/details/2026-04-27/avm-cosmos-vector-inline-details.md (Lines 100-109)
- [x] Step 2.2: Delete `infra/cosmos-babbles-vector-container.json`
  - Details: .copilot-tracking/details/2026-04-27/avm-cosmos-vector-inline-details.md (Lines 111-120)

### [ ] Implementation Phase 3: Update Changes Log

<!-- parallelizable: true -->

- [ ] Step 3.1: Update `babble-search-semantic-changes.md` to reflect corrected infrastructure
  - Details: .copilot-tracking/details/2026-04-27/avm-cosmos-vector-inline-details.md (Lines 124-148)

### [ ] Implementation Phase 4: Validation

<!-- parallelizable: false -->

- [ ] Step 4.1: Run Bicep build validation
  - Execute `az bicep build --file infra/main.bicep`
- [ ] Step 4.2: Run Bicep lint validation
  - Execute `az bicep lint --file infra/main.bicep`
- [ ] Step 4.3: Fix minor validation issues
  - Iterate on Bicep errors and warnings
  - Apply fixes directly when corrections are straightforward
- [ ] Step 4.4: Report blocking issues
  - Document issues requiring additional research
  - Provide next steps and recommended planning

## Planning Log

See .copilot-tracking/plans/logs/2026-04-27/avm-cosmos-vector-inline-log.md for discrepancy tracking, implementation paths considered, and suggested follow-on work.

## Dependencies

- Azure CLI with Bicep extension (for `az bicep build` / `az bicep lint`)
- Network access to pull AVM module `br/public:avm/res/document-db/database-account:0.19.1` from Microsoft Container Registry

## Success Criteria

- `infra/main.bicep` references `avm/res/document-db/database-account:0.19.1` — Traces to: Derived Objective (AVM upgrade)
- `capabilitiesToAdd` includes both `EnableServerless` and `EnableNoSQLVectorSearch` — Traces to: Derived Objective (capability)
- `babbles` container entry includes `indexingPolicy` with `vectorIndexes` and `vectorEmbeddingPolicy` — Traces to: User Requirement (inline vector config)
- `infra/cosmos-babbles-vector-container.bicep` and `.json` are deleted — Traces to: User Requirement (remove separate file)
- `az bicep build --file infra/main.bicep` succeeds without errors — Traces to: AGENTS.md CI pipeline requirement
- Changes log accurately reflects the actual infrastructure modifications — Traces to: Derived Objective (log accuracy)
