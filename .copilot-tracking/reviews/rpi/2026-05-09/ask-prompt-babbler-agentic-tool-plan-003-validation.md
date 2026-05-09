<!-- markdownlint-disable-file -->
# RPI Validation Report: ask-prompt-babbler-agentic-tool-plan Phase 003

## Validation Scope

* Plan: .copilot-tracking/plans/2026-05-09/ask-prompt-babbler-agentic-tool-plan.instructions.md
* Changes log: .copilot-tracking/changes/2026-05-09/ask-prompt-babbler-agentic-tool-changes.md
* Research: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md
* Phase: 3 (Host wiring and documentation updates)
* Validation date: 2026-05-09 (revalidated against current repository state)

## Overall Status

**Passed**

Both Phase 3 deliverables are verified against the current repository state. The Aspire AppHost wiring is present and correct. The response contract documented in `docs/MCP-SERVER.md` now matches the implementation exactly (aligned during the Phase 7 rework). Two Minor documentation quality gaps are recorded; neither represents a missing required deliverable.

The previous validation verdict of **Partial** was based on a since-resolved response contract mismatch. That mismatch (`answer`/`steps`/`type` vs `answer`/`trace`/`kind`) was corrected in Phase 7, which aligned both the `AgentExecutionResult` / `AgentExecutionStep` records and the documentation example to the `{answer, trace:[{kind, name, content}]}` shape.

## Step-by-Step Validation

| Step | Expected outcome | Status | Evidence |
|---|---|---|---|
| 3.1 | Add `WithReference(foundryProject)` and `WaitFor(foundryProject)` to `mcpServer` in AppHost | **Passed** | Both calls verified present in `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs` (lines 90 and 92). MCP server receives `ConnectionStrings__ai-foundry` and will not start until the Foundry resource is ready. |
| 3.2 | Update `docs/MCP-SERVER.md` with tool entry, prerequisites, configuration keys, and response contract matching implementation | **Passed** | Agentic Tools summary table, `ask_prompt_babbler` detail section, prerequisites, and response contract all present. Contract `{answer, trace:[{kind, name, content}]}` matches `AgentExecutionResult` / `AgentExecutionStep` in `prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs`. Two Minor findings recorded below. |
| 3.3 | Run `pnpm lint:md` and `dotnet build PromptBabbler.slnx` | **Passed** | Changes log final validation summary confirms `pnpm lint:md` — 212 files, 0 errors; `dotnet build` — 14 projects succeeded (pre-existing MSB3277 Aspire.Hosting warning in Api.IntegrationTests is unrelated to Phase 3). |

## File Evidence

### Step 3.1 — AppHost.cs

`prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`, `mcpServer` builder block (verified from file):

```csharp
var mcpServer = builder.AddProject<Projects.PromptBabbler_McpServer>("mcp-server")
    .WithExternalHttpEndpoints()
    .WithReference(foundryProject)     // line 90 — injects ConnectionStrings__ai-foundry
    .WithReference(apiService)
    .WaitFor(foundryProject)           // line 92 — startup ordering
    .WaitFor(apiService)
    ...
```

`foundryProject` is defined as `foundry.AddProject("ai-foundry")` and is already wired to `apiService`. The new `WithReference` / `WaitFor` pair mirrors the `apiService` pattern exactly.

### Step 3.2 — docs/MCP-SERVER.md

**Tool summary table** — `### Agentic Tools` section present:

```markdown
| Tool | Read-only | Description |
| `ask_prompt_babbler` | Yes | Run a Foundry-backed ReAct agent ... |
```

**Prerequisites section** — covers all three requirements from research (lines 496–510):

* Azure AI Foundry project with deployed chat model (`MicrosoftFoundry:chatModelName`) ✓
* `ai-foundry` Aspire connection string OR `Agentic:FoundryProjectEndpoint` config key ✓
* Managed Identity / DefaultAzureCredential access ✓

**Response contract** — documentation example:

```json
{
  "answer": "Here is a Strahd-themed image prompt ...",
  "trace": [
    { "kind": "reason", "name": "text", "content": "Reviewing matching babbles." },
    { "kind": "act", "name": "search_babbles_api", "content": "{\"query\":\"Strahd castle\"}" },
    { "kind": "observe", "name": "call-1", "content": "[...]" }
  ]
}
```

Implementation records (verified from `PromptBabblerAgentOrchestrator.cs`):

```csharp
private sealed record AgentExecutionResult(string Answer, IReadOnlyList<AgentExecutionStep> Trace);
private sealed record AgentExecutionStep(string Kind, string Name, string Content);
```

Serialized with `JsonSerializerDefaults.Web` (camelCase) → `{answer, trace:[{kind, name, content}]}`. **Contract is fully aligned.**

## Severity-Graded Findings

### Minor

**M-1 — `list_generated_prompts` and `get_generated_prompt` detail sections appear under `### Agentic Tools`**

`docs/MCP-SERVER.md` places the `#### list_generated_prompts` and `#### get_generated_prompt` detail entries after the `### Agentic Tools` heading and after `#### ask_prompt_babbler`. In the markdown heading hierarchy these detail entries fall under "Agentic Tools" rather than under their owning `### Generated Prompt Tools` section, which contains only the summary table.

* Severity: Minor — affects navigation and semantic hierarchy; no tool content is missing.
* File: `docs/MCP-SERVER.md`
* Likely cause: Phase 3 inserted the `### Agentic Tools` section between the `### Generated Prompt Tools` summary table and the pre-existing `#### list_generated_prompts` / `#### get_generated_prompt` detail entries.
* Recommended fix: Move the two `####` detail sections to directly follow the `### Generated Prompt Tools` summary table, before the `---` separator and before `### Agentic Tools`.

---

**M-2 — Tool count in overview paragraph is stale**

`docs/MCP-SERVER.md` (Tools section) states "The MCP server exposes ten tools across four categories." Current inventory: 4 Babble + 5 Prompt Template + 2 Generated Prompt + 1 Agentic = **12 tools**.

* Severity: Minor — cosmetic; does not affect discoverability.
* File: `docs/MCP-SERVER.md`
* Recommended fix: Update the sentence to "twelve tools across four categories."

## Coverage Assessment

| Requirement | Source | Status |
|---|---|---|
| MCP server receives `ai-foundry` connection string | Research L101–106; Details L163 | Covered — `WithReference(foundryProject)` present |
| Startup ordering: MCP server waits for Foundry | Details L168 | Covered — `WaitFor(foundryProject)` present |
| Config keys documented (`Agentic:FoundryProjectEndpoint`, Aspire connection string) | Research L496–510; Details L184 | Covered |
| Model deployment prerequisite documented | Details L184 | Covered |
| Response contract matches implementation exactly | Details L184; Success criterion | Covered — `{answer, trace:[{kind, name, content}]}` aligned |
| Tool name and examples match implemented API | Research L24–35 | Covered — `ask_prompt_babbler`, trace tool names match |
| `pnpm lint:md` passes | Details L201 | Covered — 0 errors on 212 files |
| `dotnet build` passes | Details L202 | Covered — 14 projects succeeded |

**Phase 3 requirement coverage: 100%.** Two Minor documentation quality gaps do not represent unmet plan requirements.

## Clarifying Questions

None. All plan items were verifiable from the available artifacts and current repository state.
