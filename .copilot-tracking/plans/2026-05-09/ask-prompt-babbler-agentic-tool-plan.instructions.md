<!-- markdownlint-disable-file -->
---
applyTo: '.copilot-tracking/changes/2026-05-09/ask-prompt-babbler-agentic-tool-changes.md'
---
# Implementation Plan: ask_prompt_babbler Agentic MCP Tool

## Overview

Add a Foundry-backed Microsoft Agent Framework MCP tool named ask_prompt_babbler to Prompt Babbler, with DI/config wiring, tool orchestration over existing API client methods, and validation coverage.

## Objectives

### User Requirements

* Add a new ask_prompt_babbler MCP tool in prompt-babbler-service/src/McpServer that runs a ReAct-style loop with Microsoft Agent Framework and Foundry. — Source: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 4-16)
* Expose existing backend operations as agent function tools via IPromptBabblerApiClient and ASP.NET Core DI. — Source: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 10-14)
* Add required NuGet packages through central package management and wire configuration compatible with Aspire naming. — Source: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 14-16)
* Document the chosen approach and implementation details. — Source: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 16-17)

### Derived Objectives

* Ensure endpoint resolution supports Agentic:FoundryProjectEndpoint, AZURE_AI_PROJECT_ENDPOINT, and Aspire-provided ConnectionStrings:ai-foundry parsing for robust runtime behavior. — Derived from: research endpoint compatibility criteria and current MCP startup gap.
* Prevent lifetime mismatches by keeping orchestrator registration scoped relative to IPromptBabblerApiClient lifetime. — Derived from: captive dependency findings in research.
* Preserve testability by introducing a dedicated orchestrator interface and unit tests at tool/orchestrator boundaries. — Derived from: MAF testability recommendations and repository unit-test conventions.

## Context Summary

### Project Files

* prompt-babbler-service/src/McpServer/Program.cs - MCP host startup where new AIProjectClient and orchestrator DI wiring must be added.
* prompt-babbler-service/src/McpServer/Tools/BabbleTools.cs - Existing sealed tool pattern and attribute usage to mirror.
* prompt-babbler-service/src/McpServer/Client/IPromptBabblerApiClient.cs - Existing operation surface for MAF function tools.
* prompt-babbler-service/src/McpServer/PromptBabbler.McpServer.csproj - Package reference integration point.
* prompt-babbler-service/Directory.Packages.props - Central package versions.
* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs - Aspire resource reference injection to MCP server.
* docs/MCP-SERVER.md - MCP tool documentation target.

### References

* .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md - Primary technical research and implementation direction.
* https://github.com/microsoft/agent-framework - Microsoft Agent Framework package and pattern references.
* https://www.nuget.org/packages/Microsoft.Agents.AI - Version confirmation for Microsoft.Agents.AI.
* https://www.nuget.org/packages/Microsoft.Agents.AI.Foundry - Version confirmation for Microsoft.Agents.AI.Foundry.
* https://www.nuget.org/packages/Azure.AI.Projects - Version confirmation for Azure.AI.Projects.

### Standards References

* AGENTS.md — repository commands, quality gates, and testing workflow.
* .github/copilot-instructions.md — sealed class rule, dependency injection patterns, and security constraints.

## Implementation Checklist

### [x] Implementation Phase 1: Add MAF Dependencies and Startup Wiring

<!-- parallelizable: false -->

* [x] Step 1.1: Add package versions to central package management
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 12-32)
* [x] Step 1.2: Add package references in McpServer project
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 33-50)
* [x] Step 1.3: Add endpoint parsing with explicit fallback order, credentials, AIProjectClient, and orchestrator DI registration in Program
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 51-75)
* [x] Step 1.4: Validate phase changes
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 76-83)

### [x] Implementation Phase 2: Implement Agent Orchestrator and MCP Tool

<!-- parallelizable: false -->

* [x] Step 2.1: Create IPromptBabblerAgentOrchestrator interface
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 88-105)
* [x] Step 2.2: Implement PromptBabblerAgentOrchestrator using MAF function tools
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 106-129)
* [x] Step 2.3: Add AgenticTools MCP tool class with ask_prompt_babbler tool
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 130-148)
* [x] Step 2.4: Validate phase changes
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 149-155)

### [x] Implementation Phase 3: Wire Host and Update Documentation

<!-- parallelizable: true -->

* [x] Step 3.1: Inject foundryProject reference into MCP server in AppHost
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 161-181)
* [x] Step 3.2: Update MCP server docs for ask_prompt_babbler
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 182-198)
* [x] Step 3.3: Validate phase changes
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 199-205)

### [x] Implementation Phase 4: Add Unit Tests for New Agentic Surface

<!-- parallelizable: true -->

* [x] Step 4.1: Create AgenticTools unit tests for request forwarding and cancellation
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 211-228)
* [x] Step 4.2: Create PromptBabblerAgentOrchestrator unit tests for response contract and cancellation
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 230-251)
* [x] Step 4.3: Validate phase changes
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 250-255)

### [x] Implementation Phase 5: Validation

<!-- parallelizable: false -->

* [x] Step 5.1: Run full project validation
  * Execute all lint commands (`pnpm lint:md` and formatting checks)
  * Execute build scripts for modified components (`dotnet build PromptBabbler.slnx`)
  * Run tests covering modified code (`dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit`)
* [x] Step 5.2: Fix minor validation issues
  * Iterate on lint errors and build/test warnings that are straightforward
* [x] Step 5.3: Report blocking issues
  * Document blockers requiring additional research and planning
  * Avoid large-scale refactors in this phase

### [x] Implementation Phase 6: Rework Foundry Endpoint Resolution and DI Safety

<!-- parallelizable: false -->

* [x] Step 6.1: Extract endpoint resolution and AIProjectClient creation into a testable service boundary
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 258-281)
* [x] Step 6.2: Update McpServer startup to use the extracted boundary and fail safely when agentic configuration is absent
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 282-299)
* [x] Step 6.3: Add unit coverage for endpoint precedence and missing-endpoint behavior
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 300-308)
* [x] Step 6.4: Validate phase changes
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 309-314)

### [x] Implementation Phase 7: Rework Agentic Contract and Behavioral Test Coverage

<!-- parallelizable: false -->

* [x] Step 7.1: Extract the Foundry agent run dependency behind a testable abstraction
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 319-338)
* [x] Step 7.2: Update the orchestrator response contract to align code and docs
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 339-351)
* [x] Step 7.3: Add behavioral unit tests for response shaping, cancellation propagation, and tool metadata coverage
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 352-364)
* [x] Step 7.4: Validate phase changes
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 365-370)

### [x] Implementation Phase 8: Final Validation and Tracking Reconciliation

<!-- parallelizable: false -->

* [x] Step 8.1: Run focused and project-level validation for the rework pass
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 375-384)
* [x] Step 8.2: Reconcile tracking artifacts with final validation evidence
  * Details: .copilot-tracking/details/2026-05-09/ask-prompt-babbler-agentic-tool-details.md (Lines 385-392)

## Planning Log

See .copilot-tracking/plans/logs/2026-05-09/ask-prompt-babbler-agentic-tool-log.md for discrepancy tracking, implementation paths considered, and suggested follow-on work.

## Dependencies

* .NET 10 SDK and NuGet restore access
* Aspire AppHost foundryProject resource availability
* Foundry deployment name for chat model (or configured Agentic:ModelDeploymentName)

## Success Criteria

* ask_prompt_babbler is discoverable via MCP tool registration and returns answer plus execution trace JSON. — Traces to: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 24-35)
* Agent orchestration uses Microsoft.Agents.AI + Microsoft.Agents.AI.Foundry and maps tools one-to-one with IPromptBabblerApiClient capabilities. — Traces to: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 10-14)
* Runtime configuration resolves Foundry endpoint from Agentic:FoundryProjectEndpoint, AZURE_AI_PROJECT_ENDPOINT, or Aspire connection string, and passes validation gates (format/build/unit/doc lint). — Traces to: .copilot-tracking/research/2026-05-09/ask-prompt-babbler-agentic-tool-research.md (Lines 20, 27-31, 171-180, 362-370)