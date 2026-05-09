<!-- markdownlint-disable-file -->
# RPI Validation: ask_prompt_babbler Agentic Tool — Phase 2

**Plan**: .copilot-tracking/plans/2026-05-09/ask-prompt-babbler-agentic-tool-plan.instructions.md
**Changes Log**: .copilot-tracking/changes/2026-05-09/ask-prompt-babbler-agentic-tool-changes.md
**Research**: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md
**Phase**: 2 — Implement Agent Orchestrator and MCP Tool
**Validation Date**: 2026-05-09
**Validator**: RPI Validator (re-run, overwrite)

---

## Phase 2 Plan Items

| Step | Description | Status |
|------|-------------|--------|
| 2.1 | Create `IPromptBabblerAgentOrchestrator` interface | PASS |
| 2.2 | Implement `PromptBabblerAgentOrchestrator` using MAF function tools | PASS with deviation |
| 2.3 | Add `AgenticTools` MCP tool class with `ask_prompt_babbler` | PASS |
| 2.4 | Validate phase changes (build + unit tests) | PASS (final) |

Additionally, Phase 4 steps that target Phase 2 output are validated here:

| Step | Description | Status |
|------|-------------|--------|
| 4.1 | `AgenticToolsTests` unit tests | PASS |
| 4.2 | `PromptBabblerAgentOrchestratorTests` unit tests | PASS with minor deviation |
| 4.3 | Unit test focused validation | PASS |

---

## Step-by-Step Findings

### Step 2.1 — Create `IPromptBabblerAgentOrchestrator` Interface

**File**: `prompt-babbler-service/src/McpServer/Agents/IPromptBabblerAgentOrchestrator.cs`
**File exists**: Yes.

**Plan success criteria**:
1. Interface defines `RunAsync(string request, CancellationToken cancellationToken)` → **MET**: Interface contains exactly `Task<string> RunAsync(string request, CancellationToken cancellationToken)`.
2. `PromptBabblerAgentOrchestrator` implements the interface → **MET**: class declaration is `public sealed class PromptBabblerAgentOrchestrator(...) : IPromptBabblerAgentOrchestrator`.
3. Tool class depends on interface, not concrete implementation → **MET**: `AgenticTools` constructor is `AgenticTools(IPromptBabblerAgentOrchestrator orchestrator)`.

**Verdict**: PASS — no deficiencies.

---

### Step 2.2 — Implement `PromptBabblerAgentOrchestrator` Using MAF Function Tools

**File**: `prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs`
**File exists**: Yes.

**Plan success criteria**:

1. `BuildTools` maps one-to-one to the six required `IPromptBabblerApiClient` operations → **MET**:
   - `search_babbles_api` → `SearchBabblesAsync` ✓
   - `get_babble_api` → `GetBabbleAsync` ✓
   - `list_prompt_templates_api` → `GetTemplatesAsync` ✓
   - `get_prompt_template_api` → `GetTemplateAsync` ✓
   - `generate_prompt_api` → `GeneratePromptAsync` ✓
   - `list_generated_prompts_api` → `ListGeneratedPromptsAsync` ✓

   Note: `ListBabblesAsync` exists in `IPromptBabblerApiClient` but is not listed as a required agent tool in the research document or plan. Its omission is correct.

2. Agent run returns JSON with final answer and structured step trace → **MET**: `AgentExecutionResult(Answer, Trace)` sealed record is serialized via `JsonSerializer.Serialize`. Trace steps are projected from `FunctionCallContent`, `FunctionResultContent`, and `TextContent` into `AgentExecutionStep(Kind, Name, Content)`.

3. `CancellationToken` passed throughout → **MET** (with Phase 7 runner abstraction):
   - Orchestrator passes the token to `agentRunner.RunAsync(...)`. The `IPromptBabblerAgentRunner` abstraction owns the Foundry session/run lifecycle and propagates the token internally. This is correct at the abstraction boundary.
   - All six tool delegate methods declare `CancellationToken cancellationToken = default` and pass it to `apiClient` calls. ✓
   - Direct reference to `CreateSessionAsync` in the orchestrator is absent because Phase 7 rework extracted this into `IPromptBabblerAgentRunner`/`PromptBabblerFoundryAgentRunner`. This is a documented deviation.

4. Class is `sealed` → **MET** (`public sealed class PromptBabblerAgentOrchestrator`).

5. Constructor injection: `IPromptBabblerAgentRunner`, `IPromptBabblerApiClient`, `IConfiguration` → **MET**.

6. `[Description]` attributes on each tool delegate method and parameters → **MET**: All six methods carry `[Description]` on the method; each parameter that benefits from a description has one.

7. Response model types are private `sealed record` → **MET**: `AgentExecutionResult` and `AgentExecutionStep` are both `private sealed record` nested inside the orchestrator class.

**Deviation recorded**: The original Step 2.2 spec described the orchestrator calling `CreateSessionAsync` and `RunAsync` directly on a Foundry agent. Phase 7 rework introduced `IPromptBabblerAgentRunner` to make this path testable. The deviation is documented in the changes log and improves testability without breaking the spec's intent.

**Verdict**: PASS with deviation — deviation is intentional, documented, and an improvement.

---

### Step 2.3 — Add `AgenticTools` MCP Tool Class

**File**: `prompt-babbler-service/src/McpServer/Tools/AgenticTools.cs`
**File exists**: Yes.

**Plan success criteria**:

1. Class has `[McpServerToolType]` attribute → **MET**.
2. Class is `sealed` → **MET** (`public sealed class AgenticTools`).
3. Constructor injection of `IPromptBabblerAgentOrchestrator` → **MET**.
4. Method has `[McpServerTool(Name = "ask_prompt_babbler")]` → **MET**.
5. Method has `[Description(...)]` containing "execution trace" → **MET**: "Ask Prompt Babbler to reason through multi-step work using available tools and return an answer with an execution trace."
6. `request` parameter has `[Description(...)]` → **MET**.
7. Thin wrapper — no direct HTTP/API logic → **MET**: body is a single `return orchestrator.RunAsync(request, cancellationToken)`.
8. `CancellationToken cancellationToken = default` parameter → **MET**.

**Verdict**: PASS — all criteria met.

---

### Step 2.4 — Validate Phase Changes

**Plan commands**: `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit`, `dotnet build PromptBabbler.slnx`.

**DI registration verification** (`prompt-babbler-service/src/McpServer/Program.cs`, lines 73–74):
- `builder.Services.AddScoped<IPromptBabblerAgentRunner, PromptBabblerFoundryAgentRunner>()` ✓
- `builder.Services.AddScoped<IPromptBabblerAgentOrchestrator, PromptBabblerAgentOrchestrator>()` ✓

Both are `AddScoped`, consistent with the research requirement for fresh-per-request DI lifetimes and compatibility with the `IPromptBabblerApiClient` scoped lifetime.

**Evidence from changes log (final Phase 8 validation)**:
- `dotnet format PromptBabbler.slnx --verify-no-changes` → **PASS**
- `dotnet build PromptBabbler.slnx` → **PASS** (14 projects succeeded; pre-existing MSB3277 warning in unrelated integration project is out-of-scope)
- `dotnet test --project tests/unit/McpServer.UnitTests/PromptBabbler.McpServer.UnitTests.csproj --configuration Release --no-restore` → **32/32 PASS**

**In-session Phase 2 anomaly**: The changes log notes that the Phase 2 unit-validation command produced exit code 8 (zero matching tests) when run at solution scope — a known `.NET 10` MTP/VSTest incompatibility artifact when integration assemblies are included without any matching tests. Resolved by scoping to the McpServer unit project directly in Phase 8.

**Verdict**: PASS (final) — in-session anomaly was a tooling artifact; definitive final pass confirmed in Phase 8.

---

### Step 4.1 — `AgenticToolsTests` Unit Tests

**File**: `prompt-babbler-service/tests/unit/McpServer.UnitTests/Tools/AgenticToolsTests.cs`
**File exists**: Yes.

**Plan success criteria**:
1. `[TestClass]`, `[TestCategory("Unit")]`, `sealed` → **MET**.
2. Uses NSubstitute for `IPromptBabblerAgentOrchestrator` → **MET**.
3. Verifies exact request forwarding and output passthrough → **MET**: `AskPromptBabbler_ShouldForwardRequestToOrchestrator`, `AskPromptBabbler_ShouldReturnOrchestratorResultUnmodified`.
4. Verifies cancellation token forwarding → **MET**: `AskPromptBabbler_ShouldPassCancellationTokenToOrchestrator`.
5. Verifies cancellation propagation → **MET**: `AskPromptBabbler_WhenOrchestratorThrowsOperationCancelledException_ShouldPropagate`.
6. MCP metadata assertions (Phase 7 additions) → **MET**: `AgenticTools_ShouldBeDiscoverableAsMcpToolType` verifies `McpServerToolTypeAttribute`; `AskPromptBabbler_ShouldDeclareExpectedMcpToolMetadata` verifies `McpServerToolAttribute.Name == "ask_prompt_babbler"` and `DescriptionAttribute.Description.Contains("execution trace")`.

**Verdict**: PASS — all criteria met plus additional metadata coverage.

---

### Step 4.2 — `PromptBabblerAgentOrchestratorTests` Unit Tests

**File**: `prompt-babbler-service/tests/unit/McpServer.UnitTests/Agents/PromptBabblerAgentOrchestratorTests.cs`
**File exists**: Yes.

**Plan success criteria**:
1. `[TestClass]`, `[TestCategory("Unit")]`, `sealed` → **MET**.
2. Tests validate answer and trace JSON shape → **MET**: `RunAsync_ShouldSerializeAnswerAndTrace` verifies exact JSON structure, blank text filtering, and all three content types (`reason`, `act`, `observe`).
3. Tests include cancellation-path behavior → **MET**: `RunAsync_ShouldPassCancellationTokenToRunner` verifies the token is forwarded to `agentRunner.RunAsync` with exact match.
4. Tests cover expected agent tool registration → **MET**: `RunAsync_ShouldRegisterExpectedAgentTools` captures and asserts the exact ordered list of six tool names.
5. Tests cover endpoint fallback precedence → **MINOR DEVIATION**: Endpoint fallback tests were moved to `AgenticFoundryClientFactoryTests.cs` (Phase 6), which tests `IAgenticFoundryClientFactory` — a more appropriate boundary since the orchestrator no longer owns endpoint resolution after the Phase 7 rework. The plan's intent for this coverage is satisfied; only the file location differs.

**Verdict**: PASS with minor deviation — endpoint precedence coverage exists in `AgenticFoundryClientFactoryTests.cs`, not in the orchestrator test class. The separation is appropriate.

---

### Step 4.3 — Unit Test Focused Validation

**Evidence from changes log**: `dotnet test --project tests/unit/McpServer.UnitTests/PromptBabbler.McpServer.UnitTests.csproj --configuration Release --no-restore` → **32/32 PASS**.

**Verdict**: PASS.

---

## Findings Summary

### Critical Findings

None.

### Major Findings

None.

### Minor Findings

| ID | Severity | Step | Finding | Evidence |
|----|----------|------|---------|---------|
| M-01 | Minor | 2.2 | Original spec referenced `CreateSessionAsync`/`RunAsync` on the Foundry agent directly inside the orchestrator. Phase 7 rework delegated this to `IPromptBabblerAgentRunner`. | `PromptBabblerAgentOrchestrator.cs` — no direct session creation; delegates to `IPromptBabblerAgentRunner`. Changes log records this as intentional rework. |
| M-02 | Minor | 4.2 | Step 4.2 spec required endpoint fallback precedence tests in orchestrator tests. Tests were correctly placed in `AgenticFoundryClientFactoryTests.cs` (Phase 6) instead. | `PromptBabblerAgentOrchestratorTests.cs` — no endpoint resolution tests. `AgenticFoundryClientFactoryTests.cs` — endpoint precedence tests present. |

Both minor findings represent intentional, documented scope adjustments that improve the design, not regressions or omissions.

---

## Coverage Assessment

| Phase 2 Requirement Area | Coverage |
|--------------------------|----------|
| Orchestrator interface (`IPromptBabblerAgentOrchestrator`) | 100% |
| Orchestrator implementation — tool mapping (6/6 required tools) | 100% |
| Orchestrator implementation — JSON response contract | 100% |
| Orchestrator implementation — cancellation propagation | 100% (via runner abstraction) |
| Orchestrator DI lifetime safety (both registrations `Scoped`) | 100% |
| Tool class (`AgenticTools`) — MCP registration and discovery | 100% |
| Tool class — thin wrapper pattern | 100% |
| Tool class — metadata correctness | 100% |
| Unit tests — tool forwarding (4 scenarios) | 100% |
| Unit tests — orchestrator JSON shaping (3 content types) | 100% |
| Unit tests — cancellation paths | 100% |
| Unit tests — tool name registration (ordered assertion) | 100% |
| Build validation | 100% |
| Format validation | 100% |

**Overall Phase 2 Coverage: 100% of required items verified present and correct.**

---

## Validation Status

**Status: PASSED**

All four Phase 2 steps are implemented and verified against the plan. The two minor deviations (runner abstraction and endpoint test separation) are intentional improvements documented in the changes log. No critical or major findings were identified. Final build and unit test gates passed with 32/32 tests.

---

## Recommended Next Validations

- [ ] Validate Phase 1 (dependency wiring, Program.cs endpoint resolution, factory registration) against current repository state.
- [ ] Validate Phase 3 (AppHost wiring, MCP-SERVER.md documentation) against current repository state.
- [ ] Validate Phase 6 (`IAgenticFoundryClientFactory`, `AgenticFoundryClientFactory`, `AgenticFoundryClientFactoryTests`) — these modify Phase 2 output files and warrant a separate review.
- [ ] Validate Phase 7 (`IPromptBabblerAgentRunner`, `PromptBabblerFoundryAgentRunner`, updated orchestrator and tests) to confirm runner contract and behavioral coverage.
- [ ] Run integration tests when an Aspire/Foundry environment is available to confirm end-to-end `ask_prompt_babbler` discovery and execution.

---

## Clarifying Questions

None — all Phase 2 items were resolvable from the available artifacts.
