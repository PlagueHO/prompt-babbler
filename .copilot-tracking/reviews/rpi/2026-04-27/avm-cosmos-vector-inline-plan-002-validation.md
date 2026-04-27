# RPI Validation: Phase 2 — Remove Orphaned Files

**Plan**: avm-cosmos-vector-inline-plan.instructions.md
**Changes Log**: avm-cosmos-vector-inline-changes.md
**Research**: avm-cosmos-vector-inline-research.md
**Phase**: 2 (Remove Orphaned Files)
**Validation Date**: 2026-04-27
**Status**: Partial

---

## Phase 2 Plan Items

| Step | Planned Action | Executed | Status |
|------|---------------|----------|--------|
| 2.1 | Delete `infra/cosmos-babbles-vector-container.bicep` | No | Correctly skipped — deviation justified |
| 2.2 | Delete `infra/cosmos-babbles-vector-container.json` | No | Not addressed — cleanup opportunity missed |

---

## Findings

### F-01: Step 2.1 correctly NOT executed — `.bicep` file is no longer orphaned (No Issue)

- **Severity**: N/A — correct behavior
- **Evidence**: `infra/main.bicep` lines 453–464 reference `cosmos-babbles-vector-container.bicep` as a module:
  ```
  module babblesVectorContainer './cosmos-babbles-vector-container.bicep' = {
    name: 'cosmos-babbles-vector-container-deployment-${resourceToken}'
    ...
  }
  ```
- **Analysis**: The original plan assumed Phase 1 would inline vector config via AVM 0.19.1, making the standalone file orphaned. Since AVM 0.19.1 was not published to MCR (DD-04 in planning log), Option B was selected (DD-06, user decision ID-01). Option B wires the standalone `.bicep` as a module dependency in `main.bicep`. Deleting it would break the deployment.
- **Verdict**: Phase 2 Step 2.1 was correctly NOT executed. The deviation is well-documented in the changes log under "Additional or Deviating Changes" and in the planning log (DD-06, ID-01). GitHub issue [#107](https://github.com/PlagueHO/prompt-babbler/issues/107) tracks future migration to AVM inline.

### F-02: Changes log accurately reflects no files removed (No Issue)

- **Severity**: N/A — correct behavior
- **Evidence**: Changes log `### Removed` section is empty. The "Additional or Deviating Changes" section explicitly documents the Option B pivot and explains why the original plan was not followed.
- **Analysis**: The changes log correctly shows no files in the Removed section. The deviation rationale is clearly stated with upstream issue links. This is accurate and consistent with the codebase state.

### F-03: `cosmos-babbles-vector-container.json` remains orphaned (Minor)

- **Severity**: Minor
- **Evidence**:
  - File exists at `infra/cosmos-babbles-vector-container.json` (compiled ARM template, 84 lines)
  - Bicep module references use the `.bicep` source, not the `.json` compiled output: `module babblesVectorContainer './cosmos-babbles-vector-container.bicep'` ([main.bicep](infra/main.bicep#L453))
  - Workspace-wide search for `cosmos-babbles-vector-container.json` returns zero matches in infrastructure, deployment, or configuration files — only `.copilot-tracking/` documents reference it
  - Not referenced in `azure.yaml` or any deployment hook
- **Analysis**: Bicep module declarations always resolve the `.bicep` source file, not compiled `.json` ARM templates. The `.json` file is a stale compilation artifact from a previous `az bicep build` run. It serves no deployment purpose, is not consumed by any pipeline step, and will become stale if the `.bicep` source is modified without recompiling.
- **Recommendation**: Delete `infra/cosmos-babbles-vector-container.json` as a standalone cleanup. This was valid for deletion under both the original plan AND the Option B deviation. The changes log should be updated if this cleanup is performed.

### F-04: Plan checklist items remain unchecked — accurate but could note deviation (Minor)

- **Severity**: Minor
- **Evidence**: Plan file Phase 2 checklist shows both steps as `[ ]` (unchecked):
  ```
  - [ ] Step 2.1: Delete `infra/cosmos-babbles-vector-container.bicep`
  - [ ] Step 2.2: Delete `infra/cosmos-babbles-vector-container.json`
  ```
- **Analysis**: Step 2.1 should ideally be marked with a deviation annotation (e.g., `SKIPPED — file no longer orphaned per Option B`) rather than left unchecked, which could imply it is still pending. Step 2.2 is genuinely still pending. The planning log (DD-06) documents the rationale, but the checklist itself does not reflect the status change.
- **Recommendation**: Annotate Step 2.1 in the plan checklist to distinguish "skipped by design" from "not yet started."

---

## Coverage Assessment

| Aspect | Coverage | Notes |
|--------|----------|-------|
| Step 2.1 (delete `.bicep`) | Addressed | Correctly skipped; deviation documented in changes log and planning log |
| Step 2.2 (delete `.json`) | Gap | Neither executed nor explicitly deferred; file remains orphaned |
| Changes log accuracy | Complete | Removed section is empty; deviation section explains the pivot |
| Codebase consistency | Mostly consistent | One stale artifact (`.json`) remains |

**Overall Phase 2 Coverage**: ~75% — The primary risk (deleting an actively-referenced file) was correctly avoided. The secondary cleanup (stale `.json`) was missed.

---

## Summary

Phase 2 was correctly NOT executed for Step 2.1 due to the Option B deviation that converted the "orphaned" `.bicep` file into an active module dependency. The changes log accurately reflects this. However, `cosmos-babbles-vector-container.json` (Step 2.2) remains a genuinely orphaned artifact that could be cleaned up independently of the AVM 0.19.1 timeline tracked by issue #107.
