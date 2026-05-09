<!-- markdownlint-disable-file -->
# Release Changes: ask_prompt_babbler Agentic MCP Tool

**Related Plan**: .copilot-tracking/plans/2026-05-09/ask-prompt-babbler-agentic-tool-plan.instructions.md
**Implementation Date**: 2026-05-09

## Summary

Completed implementation of the ask_prompt_babbler agentic MCP capability using Microsoft Agent Framework with Foundry, including dependency wiring, runtime endpoint resolution, orchestrator and tool surfaces, Aspire host integration, documentation, tests, and full validation.

## Changes

### Added

* prompt-babbler-service/src/McpServer/Agents/IPromptBabblerAgentOrchestrator.cs - Added orchestrator abstraction for testable tool composition.
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs - Added Foundry-backed MAF orchestration with function tools mapped to IPromptBabblerApiClient and JSON trace shaping.
* prompt-babbler-service/src/McpServer/Tools/AgenticTools.cs - Added MCP tool surface exposing ask_prompt_babbler.
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Tools/AgenticToolsTests.cs - Added unit tests for request forwarding, cancellation passthrough, result passthrough, and exception propagation.
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Agents/PromptBabblerAgentOrchestratorTests.cs - Added unit tests for orchestrator contract and structural constraints.
* prompt-babbler-service/src/McpServer/Configuration/IAgenticFoundryClientFactory.cs - Added a testable abstraction for Foundry endpoint resolution and client creation.
* prompt-babbler-service/src/McpServer/Configuration/AgenticFoundryClientFactory.cs - Added the Foundry client factory that owns endpoint precedence and credential-based AIProjectClient creation.
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Configuration/AgenticFoundryClientFactoryTests.cs - Added focused unit coverage for endpoint precedence and missing-endpoint behavior.
* prompt-babbler-service/src/McpServer/Agents/IPromptBabblerAgentRunner.cs - Added a testable abstraction for the Foundry agent run loop.
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerFoundryAgentRunner.cs - Added the concrete Foundry-backed agent runner that creates sessions and executes agent runs.

### Modified

* prompt-babbler-service/Directory.Packages.props - Added MAF/Foundry package version pins: Microsoft.Agents.AI 1.5.0, Microsoft.Agents.AI.Foundry 1.5.0, Azure.AI.Projects 2.0.1.
* prompt-babbler-service/src/McpServer/PromptBabbler.McpServer.csproj - Added package references for Azure.AI.Projects, Azure.Identity, Microsoft.Agents.AI, and Microsoft.Agents.AI.Foundry.
* prompt-babbler-service/src/McpServer/Program.cs - Replaced inline Foundry endpoint resolution and eager AIProjectClient registration with a dedicated factory boundary.
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs - Added foundryProject reference/wait to mcpServer for Aspire connection-string propagation.
* docs/MCP-SERVER.md - Updated the documented ask_prompt_babbler response contract to match the implementation exactly.
* .copilot-tracking/research/subagents/2026-05-09/codebase-gap-analysis.md - Added missing fenced-code language specifier to satisfy repository markdown lint validation.
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs - Switched to on-demand Foundry client creation through the new factory and then delegated Foundry execution through a testable runner.
* prompt-babbler-service/src/McpServer/Program.cs - Registered the new agent runner dependency used by the orchestrator.
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Agents/PromptBabblerAgentOrchestratorTests.cs - Replaced structural checks with behavioral coverage for JSON shaping and cancellation propagation.
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Tools/AgenticToolsTests.cs - Added MCP metadata assertions for discoverability coverage.

### Removed

* None.

## Additional or Deviating Changes

* Step 1.4 validation remains partial.
  * dotnet test with filter FoundryEndpointResolution returned zero matching tests (exit code 8).

* Phase 2 unit-validation command reported non-success despite passing unit tests.
  * dotnet test --solution with TestCategory=Unit included integration assemblies that produced zero-test exit code 8 behavior.

* Phase 3 repository-wide markdown lint initially failed on an existing unrelated tracking file.
  * .copilot-tracking/research/subagents/2026-05-09/codebase-gap-analysis.md MD040 issue was fixed in Phase 5.

* Phase 4 unit tests intentionally constrained deep orchestration assertions.
  * ResolveAiProjectEndpoint in top-level Program and sealed AIProjectClient limit unit-scope verification of endpoint precedence and full RunAsync behavior.

* Phase 6 focused test command was executed from an already-correct working directory.
  * The redundant Set-Location produced a benign path error before the successful test run.

* Solution build still reports a pre-existing Aspire package conflict warning outside the changed slice.
  * PromptBabbler.Api.IntegrationTests references an Aspire.Hosting version mix; Phases 6 and 7 intentionally did not widen scope into integration dependency cleanup.

* Phase 7 focused validation surfaced two local compile/test mismatches before passing.
  * The Foundry runner needed the Azure.AI.Projects extension namespace and positional `AsAIAgent` arguments, and the new tests needed nullable-compatible argument dictionaries plus a fully qualified `DescriptionAttribute` reference.

## Release Summary

Phases completed: 8 of 8. The implementation finished with the original delivery phases plus a targeted rework pass for DI safety, endpoint testability, contract alignment, and behavioral unit coverage.

Files created:
* prompt-babbler-service/src/McpServer/Agents/IPromptBabblerAgentOrchestrator.cs - Orchestrator abstraction for MCP tool composition.
* prompt-babbler-service/src/McpServer/Agents/IPromptBabblerAgentRunner.cs - Testable abstraction for the Foundry agent run loop.
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs - Foundry-backed MAF orchestration and JSON response shaping.
* prompt-babbler-service/src/McpServer/Agents/PromptBabblerFoundryAgentRunner.cs - Concrete Foundry runner that creates sessions and executes agent runs.
* prompt-babbler-service/src/McpServer/Configuration/IAgenticFoundryClientFactory.cs - Testable Foundry endpoint and client creation abstraction.
* prompt-babbler-service/src/McpServer/Configuration/AgenticFoundryClientFactory.cs - Foundry endpoint resolver and AIProjectClient factory.
* prompt-babbler-service/src/McpServer/Tools/AgenticTools.cs - MCP tool endpoint for ask_prompt_babbler.
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Agents/PromptBabblerAgentOrchestratorTests.cs - Behavioral orchestrator unit coverage.
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Configuration/AgenticFoundryClientFactoryTests.cs - Endpoint precedence and missing-configuration coverage.
* prompt-babbler-service/tests/unit/McpServer.UnitTests/Tools/AgenticToolsTests.cs - Tool forwarding and MCP metadata coverage.

Files modified:
* prompt-babbler-service/Directory.Packages.props - Added Microsoft Agent Framework and Azure AI Projects version pins.
* prompt-babbler-service/src/McpServer/PromptBabbler.McpServer.csproj - Added package references for the agentic/Foundry stack.
* prompt-babbler-service/src/McpServer/Program.cs - Replaced inline Foundry setup with dedicated factory and runner registrations.
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs - Added foundryProject reference and dependency ordering for the MCP server.
* docs/MCP-SERVER.md - Aligned ask_prompt_babbler documentation with the implemented response contract and configuration behavior.
* .copilot-tracking/research/subagents/2026-05-09/codebase-gap-analysis.md - Fixed a markdown fence language lint issue encountered during repository linting.
* .copilot-tracking tracking files - Updated the plan, details, changes log, and planning log for the rework phases and final validation evidence.

Validation outcomes:
* dotnet format PromptBabbler.slnx --verify-no-changes: PASS
* dotnet build PromptBabbler.slnx: PASS
  * 14 projects succeeded
  * Pre-existing MSB3277 Aspire.Hosting version warning persists in Api.IntegrationTests
* dotnet test --project tests/unit/McpServer.UnitTests/PromptBabbler.McpServer.UnitTests.csproj --configuration Release --no-restore: PASS
  * 32 of 32 tests succeeded
* pnpm lint:md: PASS
  * 212 files scanned with 0 markdown errors

Operational and dependency notes:
* Foundry endpoint resolution now supports Agentic:FoundryProjectEndpoint, AZURE_AI_PROJECT_ENDPOINT, and ConnectionStrings:ai-foundry parsing through a testable factory boundary.
* The MCP server no longer fails at startup when Foundry configuration is absent; the missing-configuration failure is deferred to ask_prompt_babbler invocation.
* Aspire AppHost propagates foundryProject connection metadata to the MCP server process.
