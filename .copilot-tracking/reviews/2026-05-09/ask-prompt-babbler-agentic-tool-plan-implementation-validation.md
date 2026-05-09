<!-- markdownlint-disable-file -->
# Implementation Validation: ask_prompt_babbler Agentic MCP Tool

## Metadata

* Date: 2026-05-09 (post-rework pass)
* Scope: full-quality
* Source: Implementation Validator subagent output (session artifact)
* Status: Needs Rework

## Severity Summary

* Critical: 0
* Major: 2
* Minor: 5

## Findings by Category

### Major

1. `PromptBabblerFoundryAgentRunner` has no direct unit-test coverage for session creation, run execution, and content extraction behavior.
   * Evidence: prompt-babbler-service/src/McpServer/Agents/PromptBabblerFoundryAgentRunner.cs
2. `AgenticFoundryClientFactory.ResolveProjectEndpoint()` reads `AZURE_AI_PROJECT_ENDPOINT` from process environment directly instead of through `IConfiguration`.
   * Evidence: prompt-babbler-service/src/McpServer/Configuration/AgenticFoundryClientFactory.cs

### Minor

1. `PromptBabblerAgentOrchestrator` has no explicit test proving `Agentic:ModelDeploymentName` override behavior.
   * Evidence: prompt-babbler-service/src/McpServer/Agents/PromptBabblerAgentOrchestrator.cs and prompt-babbler-service/tests/unit/McpServer.UnitTests/Agents/PromptBabblerAgentOrchestratorTests.cs
2. `AgenticFoundryClientFactory` invalid absolute-URI branch is not directly unit tested.
   * Evidence: prompt-babbler-service/src/McpServer/Configuration/AgenticFoundryClientFactory.cs
3. `IAgenticFoundryClientFactory` exposes concrete `AIProjectClient`, which limits test seam quality because the SDK type is sealed.
   * Evidence: prompt-babbler-service/src/McpServer/Configuration/IAgenticFoundryClientFactory.cs
4. Factory currently creates `AIProjectClient` per invocation; no explicit reuse/caching policy is documented.
   * Evidence: prompt-babbler-service/src/McpServer/Agents/PromptBabblerFoundryAgentRunner.cs and prompt-babbler-service/src/McpServer/Configuration/AgenticFoundryClientFactory.cs
5. `docs/MCP-SERVER.md` marks `ask_prompt_babbler` as read-only even though the agent can call mutating API-backed tools.
   * Evidence: docs/MCP-SERVER.md and prompt-babbler-service/src/McpServer/Tools/AgenticTools.cs

## Resolved Prior Findings

The prior critical findings are now resolved by rework phases 6 and 7:

1. No conditional DI registration path for `AIProjectClient` remains in MCP startup; client creation moved behind `IAgenticFoundryClientFactory`.
2. Endpoint resolution was extracted from `Program.cs` into `AgenticFoundryClientFactory` and now has dedicated unit coverage.
3. Orchestrator tests are now behavioral (response shaping, cancellation propagation, expected tool registration).

## Quality Verdict

Implementation is functionally complete and prior critical issues were resolved, but quality remains **needs rework** due two major gaps around runner-level testability/coverage and configuration-source consistency.
