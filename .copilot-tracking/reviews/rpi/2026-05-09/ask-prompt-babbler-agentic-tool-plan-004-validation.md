<!-- markdownlint-disable-file -->
---
title: "RPI Validation - ask-prompt-babbler-agentic-tool - Phase 004"
description: "Revalidation of Phase 4 implementation against current repository state, plan, changes log, and research requirements."
ms.date: 2026-05-09
revalidated: 2026-05-09
---

## Validation Scope

- Plan: [.copilot-tracking/plans/2026-05-09/ask-prompt-babbler-agentic-tool-plan.instructions.md](.copilot-tracking/plans/2026-05-09/ask-prompt-babbler-agentic-tool-plan.instructions.md)
- Changes log: [.copilot-tracking/changes/2026-05-09/ask-prompt-babbler-agentic-tool-changes.md](.copilot-tracking/changes/2026-05-09/ask-prompt-babbler-agentic-tool-changes.md)
- Research: [.copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md](.copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md)
- Phase validated: 4

> **Revalidation note**: A prior run of this validation returned "Failed" because it was executed before the Phase 6–8 rework passes completed. This revalidation is performed against the current repository state, which includes all rework phases. All test assertions and file evidence have been verified against live file contents and a live `dotnet test` run.

## Phase 4 Plan Items

Phase 4 contains three plan steps:

| Step | Description |
|------|-------------|
| 4.1 | Create AgenticTools unit tests for request forwarding and cancellation |
| 4.2 | Create PromptBabblerAgentOrchestrator unit tests for response contract and cancellation |
| 4.3 | Validate phase changes — run McpServer unit test project |

## Phase 4 Requirements Baseline

- Phase 4 checklist and step intent are defined in the plan under `### [x] Implementation Phase 4: Add Unit Tests for New Agentic Surface`.
- Detailed success criteria are in the details file (lines ~211–255).
- Research requires unit-test scaffolding for the new tool/orchestrator surface (outline item 7 and implementation checklist).

## Step-by-Step Validation

### Step 4.1 — AgenticTools Unit Tests

**Plan requirements (Details lines 211–228):**
- File: `prompt-babbler-service/tests/unit/McpServer.UnitTests/Tools/AgenticToolsTests.cs`
- Uses NSubstitute and `[TestCategory("Unit")]`
- Tests verify exact request forwarding and output passthrough
- Test class follows MSTest conventions and category requirements

**Verification:**

File confirmed to exist and contain the following:

| Attribute / Test method | Coverage |
|---|---|
| `[TestClass]`, `[TestCategory("Unit")]`, `sealed` | MSTest conventions ✓ |
| `Substitute.For<IPromptBabblerAgentOrchestrator>()` | NSubstitute ✓ |
| `AskPromptBabbler_ShouldForwardRequestToOrchestrator` | Request string forwarded; return value passed through |
| `AskPromptBabbler_ShouldPassCancellationTokenToOrchestrator` | CancellationToken forwarded verbatim to `orchestrator.RunAsync` |
| `AskPromptBabbler_WhenOrchestratorThrowsOperationCancelledException_ShouldPropagate` | Cancellation exception is not swallowed |
| `AskPromptBabbler_ShouldReturnOrchestratorResultUnmodified` | Orchestrator result returned without modification |
| `AgenticTools_ShouldBeDiscoverableAsMcpToolType` | `[McpServerToolTypeAttribute]` present on class — MCP discoverability (beyond plan scope) |
| `AskPromptBabbler_ShouldDeclareExpectedMcpToolMetadata` | `[McpServerToolAttribute]` with `Name = "ask_prompt_babbler"` and description containing "execution trace" (beyond plan scope) |

All plan success criteria are met. The two MCP metadata tests exceed the original plan requirements.

**Outcome: COMPLETE** ✓

---

### Step 4.2 — PromptBabblerAgentOrchestrator Unit Tests

**Plan requirements (Details lines 230–251):**
- File: `prompt-babbler-service/tests/unit/McpServer.UnitTests/Agents/PromptBabblerAgentOrchestratorTests.cs`
- Tests validate that answer and steps payloads are serialized correctly
- Tests include cancellation-path behavior
- Tests cover endpoint fallback precedence for `Agentic:FoundryProjectEndpoint`, `AZURE_AI_PROJECT_ENDPOINT`, and `ai-foundry` connection string parsing

**Verification:**

File confirmed to exist and contain the following:

| Test method | Coverage |
|---|---|
| `RunAsync_ShouldSerializeAnswerAndTrace` | Full JSON shape: `answer`, 3-step trace covering `reason`/TextContent, `act`/FunctionCallContent, `observe`/FunctionResultContent; whitespace TextContent correctly excluded |
| `RunAsync_ShouldPassCancellationTokenToRunner` | CancellationToken forwarded to runner; model name default `"chat"` verified |
| `RunAsync_ShouldRegisterExpectedAgentTools` | All 6 expected tool names verified: `search_babbles_api`, `get_babble_api`, `list_prompt_templates_api`, `get_prompt_template_api`, `generate_prompt_api`, `list_generated_prompts_api` |

**Deviation — Endpoint Precedence Coverage Relocated:**

The plan Step 4.2 success criterion specifies endpoint fallback precedence be covered here. Following the Phase 6 architectural rework, endpoint resolution was extracted into `AgenticFoundryClientFactory`. Its unit tests live at:

`prompt-babbler-service/tests/unit/McpServer.UnitTests/Configuration/AgenticFoundryClientFactoryTests.cs`

Tests in that file that satisfy the plan's endpoint criterion:

| Test method | Coverage |
|---|---|
| `ResolveProjectEndpoint_ShouldPreferConfiguredEndpoint` | `Agentic:FoundryProjectEndpoint` wins over env var and connection string |
| `ResolveProjectEndpoint_ShouldUseEnvironmentEndpointWhenConfiguredEndpointMissing` | `AZURE_AI_PROJECT_ENDPOINT` used when config key absent |
| `ResolveProjectEndpoint_ShouldUseConnectionStringEndpointWhenHigherPrecedenceSourcesMissing` | `ConnectionStrings:ai-foundry` `Endpoint=` segment parsed as final fallback |
| `ResolveProjectEndpoint_ShouldUseBareConnectionStringUriWhenEndpointSegmentMissing` | Bare URI connection string used when no `Endpoint=` segment present |
| `CreateClient_ShouldThrowClearErrorWhenEndpointMissing` | `InvalidOperationException` with message containing `"Agentic Foundry project endpoint is not configured"` |

The relocation is an architectural improvement — endpoint resolution concerns belong in the factory, not the orchestrator. All three endpoint sources from the plan's success criterion are covered. The deviation is documented in the changes log ("Additional or Deviating Changes" section).

**Deviation severity:** Minor — file location differs from plan, but coverage is complete and architecturally superior.

**Outcome: COMPLETE (with documented deviation)** ✓

---

### Step 4.3 — Validate Phase Changes

**Plan requirement (Details lines ~253–255):**

```text
dotnet test --project tests/unit/McpServer.UnitTests/PromptBabbler.McpServer.UnitTests.csproj --configuration Release --no-restore
```

**Verification (live run, 2026-05-09 revalidation):**

```text
Test run summary: Passed!
  total: 32
  failed: 0
  succeeded: 32
  skipped: 0
  duration: 2s 987ms
```

32 of 32 tests pass against the current repository state. This includes:

- `AgenticToolsTests` (6 tests)
- `PromptBabblerAgentOrchestratorTests` (3 tests)
- `AgenticFoundryClientFactoryTests` (5 tests, endpoint precedence)
- Other McpServer unit tests from prior phases (18 tests)

**Outcome: COMPLETE** ✓

---

## Coverage Assessment

| Plan item | Status | Evidence |
|---|---|---|
| 4.1 — AgenticTools tests (forwarding, passthrough, cancellation) | Complete | `AgenticToolsTests.cs` — 6 tests, all criteria met and exceeded |
| 4.2 — Orchestrator tests (JSON shape, cancellation, tool names) | Complete | `PromptBabblerAgentOrchestratorTests.cs` — 3 behavioral tests |
| 4.2 — Endpoint precedence tests | Complete (relocated) | `AgenticFoundryClientFactoryTests.cs` — 5 tests, all three sources covered |
| 4.3 — Unit test run passes | Complete | 32/32 pass in live revalidation run |

**Overall Phase 4 coverage: 100%** — all plan items verified with file evidence and a passing live test run.

## Severity-Graded Findings

### Critical Findings

None.

### Major Findings

None.

### Minor Findings

**M-001 — Endpoint Precedence Coverage Relocated to `AgenticFoundryClientFactoryTests.cs`**

- Severity: Minor
- Description: Plan Step 4.2 success criteria specify endpoint fallback precedence coverage inside `PromptBabblerAgentOrchestratorTests.cs`. Following the Phase 6 extraction of endpoint resolution into `AgenticFoundryClientFactory`, this coverage was placed in `AgenticFoundryClientFactoryTests.cs`.
- Evidence: `PromptBabblerAgentOrchestratorTests.cs` contains no endpoint tests; `AgenticFoundryClientFactoryTests.cs` contains 5 endpoint precedence tests; changes log documents the deviation under "Additional or Deviating Changes."
- Impact: None on correctness or testability — coverage is complete and better structured.
- Recommendation: No action required; the deviation is documented and architecturally justified.

## Deviations and Missing Work

- Deviation: Endpoint precedence unit tests were moved from `PromptBabblerAgentOrchestratorTests.cs` to `AgenticFoundryClientFactoryTests.cs` as part of the Phase 6 architectural rework. All endpoint sources required by Step 4.2 are covered.
- Missing work: None.

## Severity Summary

| Severity | Count |
|---|---|
| Critical | 0 |
| Major | 0 |
| Minor | 1 |

## Validation Status

**Overall status: PASSED**

## Confidence Rating

- Confidence: High
- Basis: All test files were read and verified against plan criteria. A live `dotnet test` run confirmed 32/32 tests pass. The one deviation is documented in the changes log and verified to satisfy the underlying plan requirement through an alternative file.

## Recommended Next Validations

- [ ] Revalidate Phase 5 (full solution validation: format, build, test, lint) against current repository state to confirm no regressions from rework phases 6–8.
- [ ] Revalidate Phase 6 (Foundry factory extraction and DI safety rework) since its outputs — `AgenticFoundryClientFactory`, `AgenticFoundryClientFactoryTests.cs`, `IPromptBabblerAgentRunner`, `PromptBabblerFoundryAgentRunner` — satisfy Phase 4 endpoint requirements.
- [ ] Monitor `RunAsync_ShouldRegisterExpectedAgentTools` for drift if new `IPromptBabblerApiClient` methods are added in future phases.

## Clarifying Questions

None. All Phase 4 plan items are fully resolved against available artifacts and a live test run.
