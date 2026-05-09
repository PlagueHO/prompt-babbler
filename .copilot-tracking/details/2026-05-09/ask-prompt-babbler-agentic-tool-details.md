<!-- markdownlint-disable-file -->
# Implementation Details: ask_prompt_babbler Agentic MCP Tool

## Context Reference

Sources: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md, AGENTS.md, .github/copilot-instructions.md

## Implementation Phase 1: Add Agent Framework and Configuration Plumbing

<!-- parallelizable: false -->

### Step 1.1: Add required package versions in central package management

Update package version pins so McpServer can reference MAF and Foundry SDKs from Directory.Packages.props.

Files:
* prompt-babbler-service/Directory.Packages.props - Add Microsoft.Agents.AI, Microsoft.Agents.AI.Foundry, and Azure.AI.Projects versions.

Discrepancy references:
* Addresses DD-01 by explicitly adding only package changes required for selected Foundry-based path.

Success criteria:
* Package version entries exist for Microsoft.Agents.AI 1.5.0, Microsoft.Agents.AI.Foundry 1.5.0, and Azure.AI.Projects 2.0.1.
* Existing Azure.Identity pin is reused without version drift.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 143-170) - Validated NuGet versions.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 89-90) - Missing package state in current project.

Dependencies:
* None.

### Step 1.2: Reference MAF and Foundry packages from McpServer project

Modify the McpServer project file to consume centrally managed package versions.

Files:
* prompt-babbler-service/src/McpServer/PromptBabbler.McpServer.csproj - Add PackageReference entries for Microsoft.Agents.AI, Microsoft.Agents.AI.Foundry, Azure.AI.Projects, and Azure.Identity.

Success criteria:
* Build restores all new packages without local version attributes in the csproj.
* Project references align with central package management conventions.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 95-96) - Current project does not include required references.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 514-522) - Expected csproj additions.

Dependencies:
* Step 1.1 completion.

### Step 1.3: Configure AIProjectClient and orchestrator registration in McpServer Program

Add connection-string endpoint parsing, credential strategy, AIProjectClient registration, and orchestrator DI registration.

Files:
* prompt-babbler-service/src/McpServer/Program.cs - Add Azure SDK usings, parse ConnectionStrings__ai-foundry endpoint, register AIProjectClient singleton, register IPromptBabblerAgentOrchestrator as scoped.

Discrepancy references:
* Addresses DR-01 by explicitly retaining a fallback to Agentic:FoundryProjectEndpoint when connection-string discovery is unavailable.

Success criteria:
* Program resolves endpoint in this order: Agentic:FoundryProjectEndpoint, then AZURE_AI_PROJECT_ENDPOINT, then ConnectionStrings:ai-foundry Endpoint value.
* Credential behavior matches API project convention (DefaultAzureCredential in development, ManagedIdentityCredential in production).
* DI graph has no captive dependency between orchestrator and IPromptBabblerApiClient.
* Non-Aspire local configuration path succeeds when only AZURE_AI_PROJECT_ENDPOINT is set.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 83-84) - MCP discovery through WithToolsFromAssembly.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 101-106) - Missing Foundry endpoint propagation in current host wiring.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 107-112) - Credential strategy in existing API project.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 174-180) - Endpoint parsing and AIProjectClient requirements.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 185-190) - Lifetime guidance and captive dependency analysis.

Dependencies:
* Step 1.2 completion.

### Step 1.4: Validate phase changes

Run lint/build validation relevant to McpServer dependency and startup wiring after package and Program updates.

Validation commands:
* dotnet build PromptBabbler.slnx - Validate package restore and compile success.
* dotnet format PromptBabbler.slnx --verify-no-changes - Enforce repository formatting gate.
* dotnet test --project tests/unit/McpServer.UnitTests/PromptBabbler.McpServer.UnitTests.csproj --filter "FoundryEndpointResolution" --configuration Release --no-restore - Validate endpoint fallback precedence.

## Implementation Phase 2: Implement Agent Orchestration and MCP Tool Surface

<!-- parallelizable: false -->

### Step 2.1: Create orchestrator interface for testable tool composition

Add a dedicated orchestrator interface consumed by the MCP tool class.

Files:
* prompt-babbler-service/src/McpServer/Agents/IPromptBabblerAgentOrchestrator.cs - Define RunAsync(string request, CancellationToken cancellationToken).

Success criteria:
* Interface exists and is implemented by PromptBabblerAgentOrchestrator.
* Tool class depends on interface, not concrete implementation.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 191-195) - Testability recommendation.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 293-337) - Interface and tool usage example.

Dependencies:
* Phase 1 completion.

### Step 2.2: Implement PromptBabblerAgentOrchestrator with MAF function tools

Implement orchestrator to build AITool delegates around IPromptBabblerApiClient calls, execute agent run, and serialize execution trace.

Files:
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs - Implement Foundry-backed agent run path, tool delegates, trace projection, and JSON response model.

Discrepancy references:
* Addresses DD-02 by selecting Foundry-hosted MAF path and documenting rejection of non-Foundry alternatives.

Success criteria:
* BuildTools maps one-to-one to SearchBabblesAsync, GetBabbleAsync, GetTemplatesAsync/GetTemplateAsync, GeneratePromptAsync, and ListGeneratedPromptsAsync.
* Agent run returns JSON with final answer and structured step trace.
* CancellationToken is passed to CreateSessionAsync, RunAsync, and all tool delegate methods.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 85-94) - API surface available for orchestration.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 128-142) - MAF function tool and run-loop mechanics.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 251-292) - Reference orchestrator pattern.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 457-489) - Selected scenario and lifetime model.

Dependencies:
* Step 2.1 completion.

### Step 2.3: Add AgenticTools MCP tool class and ask_prompt_babbler method

Create MCP tool class that forwards request handling to the orchestrator and is discoverable via WithToolsFromAssembly.

Files:
* prompt-babbler-service/src/McpServer/Tools/AgenticTools.cs - Add [McpServerToolType] sealed class with AskPromptBabbler tool method.

Success criteria:
* Tool method name is ask_prompt_babbler and appears in MCP discovery.
* Tool method is thin orchestration wrapper with no direct HTTP/API logic.
* Class is sealed and uses constructor injection.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 83-88) - Existing MCP tool pattern and discovery behavior.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 339-352) - Target tool implementation pattern.

Dependencies:
* Step 2.2 completion.

### Step 2.4: Validate phase changes

Run unit-scope validation for McpServer tool/orchestrator compilation and behavior.

Validation commands:
* dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit --configuration Release --no-restore
* dotnet build PromptBabbler.slnx

## Implementation Phase 3: Host Wiring and Documentation Updates

<!-- parallelizable: true -->

### Step 3.1: Inject Foundry resource reference into MCP server orchestration

Update Aspire AppHost so MCP server receives ConnectionStrings__ai-foundry from foundryProject.

Files:
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs - Add WithReference(foundryProject) and WaitFor(foundryProject) to mcpServer project builder.

Discrepancy references:
* Addresses DD-01 by intentionally expanding beyond McpServer-only scope to satisfy runtime dependency injection at host layer.

Success criteria:
* MCP server receives ai-foundry connection string during Aspire run.
* Startup ordering ensures MCP server waits for Foundry reference readiness.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 101-106) - Current missing reference.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 362-370) - Required AppHost pattern.

Dependencies:
* Phase 1 completion.

### Step 3.2: Update MCP server documentation for agentic tool usage

Document ask_prompt_babbler behavior, prerequisites, and expected response structure.

Files:
* docs/MCP-SERVER.md - Add tool table entry and agentic tools section.

Success criteria:
* Documentation describes configuration keys, model deployment dependency, and response trace contract.
* Tool name and examples match implemented API exactly.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 496-510) - Documentation checklist and required notes.

Dependencies:
* Step 2.3 completion.

### Step 3.3: Validate phase changes

Run docs and solution validation relevant to host and documentation updates.

Validation commands:
* pnpm lint:md - Markdown quality for updated docs.
* dotnet build PromptBabbler.slnx - Confirm AppHost wiring compiles.

## Implementation Phase 4: Add Unit Tests for Tool Contract and Orchestrator Behavior

<!-- parallelizable: true -->

### Step 4.1: Add unit tests for AgenticTools request forwarding

Create tool-layer tests ensuring request and cancellation token are forwarded to orchestrator interface.

Files:
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Tools/AgenticToolsTests.cs - Unit tests with NSubstitute and [TestCategory("Unit")].

Success criteria:
* Tests verify exact request forwarding and output passthrough.
* Test class follows MSTest conventions and category requirements.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 191-195) - Interface-based testability.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 524-527) - Planned unit test files.

Dependencies:
* Step 2.3 completion.

### Step 4.2: Add unit tests for PromptBabblerAgentOrchestrator response shaping

Create orchestrator tests with controlled fake chat/tool behavior to validate trace shaping and error surfaces.

Files:
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Agents/PromptBabblerAgentOrchestratorTests.cs - Unit tests for JSON shape, tool registration intent, and cancellation propagation.

Discrepancy references:
* Addresses DR-02 by limiting deep MAF internals verification to integration scope and covering deterministic serializer/output behavior in unit scope.

Success criteria:
* Tests validate that answer and steps payloads are serialized correctly.
* Tests include cancellation-path behavior.
* Tests cover endpoint fallback precedence for Agentic config key, AZURE_AI_PROJECT_ENDPOINT, and ai-foundry connection string parsing.

Context references:
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 191-195) - Recommended testing pattern.
* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 524-527) - Planned unit test files.

Dependencies:
* Step 2.2 completion.

### Step 4.3: Validate phase changes

Run unit tests for McpServer test project and collect failures for immediate fixes.

Validation commands:
* dotnet test --project tests/unit/McpServer.UnitTests/PromptBabbler.McpServer.UnitTests.csproj --configuration Release --no-restore

## Implementation Phase 5: Validation

<!-- parallelizable: false -->

### Step 5.1: Run full project validation

Execute all validation commands for modified components.
* dotnet format PromptBabbler.slnx --verify-no-changes
* dotnet build PromptBabbler.slnx
* dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit --configuration Release --no-restore
* pnpm lint:md

### Step 5.2: Fix minor validation issues

Iterate on lint errors, build warnings, and unit test failures that are straightforward and isolated to this change.

### Step 5.3: Report blocking issues

When validation failures require larger refactors, document the blockers, impacted files, and recommended follow-up planning.

## Dependencies

* .NET 10 SDK and Restore access for prompt-babbler-service solution.
* Aspire AppHost foundryProject resource definition remains available.
* Foundry project model deployment named chat (or updated via Agentic:ModelDeploymentName).

## Success Criteria

* ask_prompt_babbler is available via MCP tool discovery and delegates through the orchestrator.
* Foundry-backed MAF execution path is wired through DI and configuration without lifetime mismatches.
* Unit tests and format/build/lint gates pass for modified scopes.

## Implementation Phase 6: Rework Foundry Endpoint Resolution and DI Safety

<!-- parallelizable: false -->

### Step 6.1: Extract endpoint resolution and AIProjectClient creation into a testable boundary

Move endpoint precedence parsing and AIProjectClient creation out of `Program.cs` into a dedicated service or helper that unit tests can exercise directly.

Files:
* prompt-babbler-service/src/McpServer/Configuration/AgenticFoundryOptions.cs - Add immutable options or helper model for resolved endpoint state if needed.
* prompt-babbler-service/src/McpServer/Configuration/IAgenticFoundryClientFactory.cs - Add a testable abstraction for endpoint resolution and optional AIProjectClient creation.
* prompt-babbler-service/src/McpServer/Configuration/AgenticFoundryClientFactory.cs - Implement precedence parsing and credential/client creation.

Success criteria:
* Endpoint precedence remains `Agentic:FoundryProjectEndpoint`, then `AZURE_AI_PROJECT_ENDPOINT`, then `ConnectionStrings:ai-foundry`.
* The new boundary can be unit-tested without bootstrapping the full web host.
* Missing endpoint configuration does not force a startup failure path through unconditional DI registration.

### Step 6.2: Update McpServer startup to use the extracted boundary and fail safely when agentic configuration is absent

Register the new factory in DI and update the orchestrator registration path so the agentic tool returns a clear runtime error when Foundry configuration is missing instead of failing application startup.

Files:
* prompt-babbler-service/src/McpServer/Program.cs - Replace local endpoint function and singleton registration with the new service boundary.
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs - Consume the new abstraction instead of a directly injected `AIProjectClient`.

Success criteria:
* MCP server starts successfully when the Foundry endpoint is not configured.
* `ask_prompt_babbler` fails locally with a clear exception message only when invoked without Foundry configuration.
* The DI graph no longer depends on conditional registration of `AIProjectClient`.

### Step 6.3: Add unit coverage for endpoint precedence and missing-endpoint behavior

Add unit tests around the extracted boundary for precedence and missing configuration behavior.

Files:
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Configuration/AgenticFoundryClientFactoryTests.cs - Cover endpoint precedence and missing-endpoint cases.

Success criteria:
* Tests verify each precedence branch.
* Tests verify the factory reports missing configuration deterministically.

### Step 6.4: Validate phase changes

Validation commands:
* dotnet test --project tests/unit/McpServer.UnitTests/PromptBabbler.McpServer.UnitTests.csproj --filter "AgenticFoundryClientFactory" --configuration Release --no-restore
* dotnet build PromptBabbler.slnx

## Implementation Phase 7: Rework Agentic Contract and Behavioral Test Coverage

<!-- parallelizable: false -->

### Step 7.1: Extract the Foundry agent run dependency behind a testable abstraction

Introduce a dedicated abstraction that encapsulates the Foundry `CreateSessionAsync` and `RunAsync` calls so orchestrator tests can drive deterministic responses without mocking sealed Azure SDK types.

Files:
* prompt-babbler-service/src/McpServer/Agents/IPromptBabblerAgentRunner.cs - Define the agent runner abstraction.
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerFoundryAgentRunner.cs - Implement the abstraction with `AIProjectClient.AsAIAgent(...)`.
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs - Delegate the Foundry interaction to the runner.

Success criteria:
* Orchestrator logic is behavior-testable with a substitute runner.
* Cancellation tokens flow through orchestrator to the runner.

### Step 7.2: Update the orchestrator response contract to align code and docs

Standardize the agentic response schema so documentation and implementation match exactly.

Files:
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs - Align JSON output property names with the documented contract.
* docs/MCP-SERVER.md - Update the documented JSON response example and terminology to match implementation exactly.

Success criteria:
* The implementation returns the same top-level property names documented in `docs/MCP-SERVER.md`.
* Response terminology is consistent across code, tests, and docs.

### Step 7.3: Add behavioral unit tests for response shaping, cancellation propagation, and tool metadata coverage

Replace the purely structural orchestrator tests with behavior-focused tests and extend `AgenticTools` coverage to assert MCP metadata.

Files:
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Agents/PromptBabblerAgentOrchestratorTests.cs - Add behavior tests for JSON shaping and cancellation propagation.
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Tools/AgenticToolsTests.cs - Add attribute/discoverability assertions.

Success criteria:
* Tests validate the final JSON payload structure.
* Tests validate cancellation token forwarding to the runner.
* Tool tests assert the expected MCP tool type/name metadata.

### Step 7.4: Validate phase changes

Validation commands:
* dotnet test --project tests/unit/McpServer.UnitTests/PromptBabbler.McpServer.UnitTests.csproj --filter "PromptBabblerAgentOrchestrator|AgenticTools" --configuration Release --no-restore
* dotnet build PromptBabbler.slnx

## Implementation Phase 8: Final Validation and Tracking Reconciliation

<!-- parallelizable: false -->

### Step 8.1: Run focused and project-level validation for the rework pass

Validation commands:
* dotnet format PromptBabbler.slnx --verify-no-changes
* dotnet build PromptBabbler.slnx
* dotnet test --project tests/unit/McpServer.UnitTests/PromptBabbler.McpServer.UnitTests.csproj --configuration Release --no-restore
* pnpm lint:md

### Step 8.2: Reconcile tracking artifacts with final validation evidence

Update the plan, changes log, and planning log to reflect the rework completion status and remaining follow-on work, if any.