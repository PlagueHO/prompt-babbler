# RPI Validation: Phase 1 — Upgrade AVM and Inline Vector Config

**Plan**: `.copilot-tracking/plans/2026-04-27/avm-cosmos-vector-inline-plan.instructions.md`
**Changes log**: `.copilot-tracking/changes/2026-04-27/avm-cosmos-vector-inline-changes.md`
**Research**: `.copilot-tracking/research/2026-04-27/avm-cosmos-vector-inline-research.md`
**Phase**: 1 (Upgrade AVM and Inline Vector Config)
**Validation date**: 2026-04-27

---

## Phase 1 Plan Items vs. Actual State

### Step 1.1: Bump AVM module version from 0.19.0 to 0.19.1

| Attribute | Value |
| --- | --- |
| **Plan status** | BLOCKED |
| **Changes log** | Reports AVM 0.19.1 not published to MCR; Option B workaround used instead |
| **Actual file state** | `infra/main.bicep` line 365: `'br/public:avm/res/document-db/database-account:0.19.0'` — version remains 0.19.0 |
| **Verdict** | Consistent — file matches plan BLOCKED status and changes log |
| **Severity** | N/A — no defect; BLOCKED status is justified |

**Evidence**: [infra/main.bicep](infra/main.bicep#L365) confirms version `0.19.0`.

**Justification assessment**: The changes log cites upstream MCR publication failure and references two GitHub links:

- `https://github.com/Azure/bicep-registry-modules/issues/6624`
- `https://github.com/Azure/bicep-registry-modules/pull/6711`

The BLOCKED status is properly documented with root cause and tracking references.

---

### Step 1.2: Add `EnableNoSQLVectorSearch` to `capabilitiesToAdd`

| Attribute | Value |
| --- | --- |
| **Plan status** | Done (checked) |
| **Changes log** | Reports `EnableNoSQLVectorSearch` added to `capabilitiesToAdd` |
| **Actual file state** | `infra/main.bicep` lines 375-378: `capabilitiesToAdd` contains both `'EnableServerless'` and `'EnableNoSQLVectorSearch'` |
| **Research requirement** | Research confirms `EnableNoSQLVectorSearch` is a valid `@allowed` value (Section 2) |
| **Verdict** | PASS — fully implemented as planned |
| **Severity** | N/A — no defect |

**Evidence**: [infra/main.bicep](infra/main.bicep#L375-L378) confirms both capabilities present.

---

### Step 1.3: Add `indexingPolicy` and `vectorEmbeddingPolicy` to the babbles container

| Attribute | Value |
| --- | --- |
| **Plan status** | BLOCKED (AVM 0.19.0 does not support vectorEmbeddingPolicy) |
| **Changes log** | Reports Option B workaround: babbles container removed from AVM containers array, deployed separately via `cosmos-babbles-vector-container.bicep` module |
| **Actual file state** | babbles container is NOT in the AVM `sqlDatabases.containers` array; comment at line 432-433 references issue #107; standalone `babblesVectorContainer` module wired at lines 449-463 |
| **Research requirement** | Research Section 3 specifies the exact inline schema for vectorEmbeddingPolicy and indexingPolicy; these are correctly implemented in the standalone module instead |
| **Verdict** | DEVIATION — Plan item not completed as specified; workaround applied instead |
| **Severity** | See deviation analysis below |

**Evidence**:

- [infra/main.bicep](infra/main.bicep#L432-L433): Comment replacing babbles in AVM array
- [infra/main.bicep](infra/main.bicep#L448-L463): `babblesVectorContainer` module reference
- [infra/cosmos-babbles-vector-container.bicep](infra/cosmos-babbles-vector-container.bicep): Standalone module with full vector config

---

## Deviation Analysis: Option B Workaround

### What deviated

The plan intended to inline `vectorEmbeddingPolicy` and `indexingPolicy` directly on the babbles container within the AVM module call. Instead, the babbles container was removed from the AVM containers array and deployed via a standalone Bicep module (`cosmos-babbles-vector-container.bicep`) that uses the raw ARM resource type.

### Is the deviation justified?

**Yes.** The root cause is an external dependency: AVM 0.19.1 is not published to MCR despite being merged upstream. This is outside the project's control. The workaround achieves the same functional outcome (Cosmos DB container with vector search config) through a different mechanism.

### Is the deviation properly documented?

| Documentation check | Status |
| --- | --- |
| Changes log explains the deviation | Yes — "Additional or Deviating Changes" section |
| Changes log cites upstream issue | Yes — links to issues #6624 and PR #6711 |
| Plan marks steps 1.1 and 1.3 as BLOCKED | Yes |
| Tracking issue created (#107) | Yes — for future migration to AVM inline |
| Code comments in main.bicep reference #107 | Yes — lines 432-433 and lines 449-453 |

### Standalone module verification

The `cosmos-babbles-vector-container.bicep` file contains:

- Correct partition key: `/userId` with Hash v2 — matches the original babbles container spec
- `vectorEmbeddingPolicy` with `/contentVector`, `Float32`, `Cosine`, `1536` dimensions — matches research Section 3
- `indexingPolicy` with `vectorIndexes` (`quantizedFlat` on `/contentVector`) and `/contentVector/*` in `excludedPaths` — matches research Section 3
- `dependsOn` on `cosmosDbAccount` in `main.bicep` — correct deployment ordering

**Severity**: **Minor** — The deviation is a valid workaround for an external blocker, properly documented with a tracking issue for future resolution. No functional gap exists.

---

## Findings Summary

| # | Finding | Severity | Step |
| --- | --- | --- | --- |
| 1 | AVM version remains 0.19.0 (BLOCKED — justified) | Minor | 1.1 |
| 2 | `EnableNoSQLVectorSearch` correctly added | Pass | 1.2 |
| 3 | Inline vector config not applied (BLOCKED — Option B workaround used) | Minor | 1.3 |
| 4 | Orphaned files NOT deleted — `cosmos-babbles-vector-container.bicep` and `.json` still exist | N/A | Phase 2 scope |
| 5 | All deviations documented with tracking issue #107 | Pass | All |

---

## Coverage Assessment

| Step | Status |
| --- | --- |
| 1.1 Bump AVM version | BLOCKED — properly documented |
| 1.2 Add EnableNoSQLVectorSearch | COMPLETE |
| 1.3 Inline vector config | BLOCKED — workaround delivers equivalent functionality |

**Overall phase coverage**: **Partial** — 1 of 3 steps completed as planned; 2 steps blocked by external dependency with a functional workaround in place. The workaround achieves the vector search infrastructure goal through an alternative path.

---

## Validation Status: **Partial**

Phase 1 is partially complete. The critical functional requirement (Cosmos DB vector search infrastructure) is met through the Option B workaround. The original plan objective (eliminate the standalone module by inlining via AVM 0.19.1) remains blocked on upstream MCR publication. Tracking issue #107 captures the remaining work.
