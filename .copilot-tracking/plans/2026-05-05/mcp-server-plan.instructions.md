---
applyTo: '.copilot-tracking/changes/2026-05-05/mcp-server-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: MCP Server for Prompt Babbler

## Overview

Create a new `PromptBabbler.McpServer` ASP.NET Core project that exposes Prompt Babbler data and operations to AI assistants via the MCP protocol using Streamable HTTP transport, communicating exclusively through the existing REST API.

## Objectives

### User Requirements

* Create a new `PromptBabbler.McpServer` ASP.NET Core project under `prompt-babbler-service/src/McpServer/` — Source: research document task statement
* Use `ModelContextProtocol.AspNetCore` SDK with Streamable HTTP transport — Source: research document task statement
* Implement MCP Tools for babble search/read, prompt template CRUD, generated prompt read, and prompt generation — Source: research document task statement
* Implement MCP Resources for browsable template data — Source: research document task statement
* Implement MCP Prompts for the `review_template` workflow — Source: research document task statement
* Create a typed HTTP client for API communication with Aspire service discovery — Source: research document task statement
* Support anonymous, access-code, and Entra ID auth modes — Source: research document task statement
* Integrate into the Aspire AppHost alongside the API — Source: research document task statement
* Add the project to the solution file — Source: research document task statement

### Derived Objectives

* Add `ModelContextProtocol.AspNetCore` to `Directory.Packages.props` — Derived from: central package management convention in `AGENTS.md`
* Create lightweight response DTOs in `Client/Models/` rather than referencing the Api project — Derived from: IP-02 rejection; avoids pulling in Cosmos/AI SDK transitive dependencies
* Add `McpAccessCodeMiddleware` with `FixedTimeEquals` for constant-time access code validation — Derived from: copilot-instructions.md security requirement and DD-02
* The MCP server does NOT reference `PromptBabbler.Domain` or `PromptBabbler.Api` projects — Derived from: IP-02 rejection; keep dependency graph minimal

## Context Summary

### Project Files

* prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs — Aspire AppHost; `apiService` ends at line 84; frontend starts at line 86; MCP server inserts between lines 84–86
* prompt-babbler-service/src/Orchestration/AppHost/PromptBabbler.AppHost.csproj — AppHost project; new ProjectReference inserts after line 15
* prompt-babbler-service/Directory.Packages.props — Central package versions; `ModelContextProtocol.AspNetCore` not present; insert in Azure/AI block after Newtonsoft.Json (line 33)
* prompt-babbler-service/PromptBabbler.slnx — Solution file; `/src/` folder lines 2–8; new project inserts after line 5 (alphabetically between Infrastructure and Orchestration)
* prompt-babbler-service/src/Orchestration/ServiceDefaults/PromptBabbler.ServiceDefaults.csproj — Referenced by new project; provides Aspire service discovery and OpenTelemetry
* prompt-babbler-service/src/Api/Program.cs — Auth patterns for mirroring in MCP server; ACCESS_CODE → AccessControl:AccessCode; AzureAd:ClientId presence controls auth mode
* prompt-babbler-service/Directory.Build.props — net10.0, ImplicitUsings, Nullable, TreatWarningsAsErrors, EnforceCodeStyleInBuild; all apply to new project automatically

### References

* .copilot-tracking/research/2026-05-05/mcp-server-research.md — Full research findings, code patterns, auth architecture, and MCP primitive mapping
* https://github.com/modelcontextprotocol/csharp-sdk — MCP C# SDK; v1.2.0; ModelContextProtocol.AspNetCore package
* https://modelcontextprotocol.io/specification/2025-03-26/basic/transports#streamable-http — Streamable HTTP transport spec
* https://modelcontextprotocol.io/specification/2025-06-18/basic/authorization — MCP OAuth 2.1 authorization spec
* https://modelcontextprotocol.io/specification/2025-06-18/basic/security_best_practices — Token passthrough anti-pattern (§3.7)

### Standards References

* .github/copilot-instructions.md — Every C# class must be `sealed`; `FixedTimeEquals` for secret comparison; domain models as immutable `sealed record`; constructor DI; `[JsonPropertyName]` attributes
* AGENTS.md — SOLID, DRY, KISS, YAGNI principles; sealed classes; `_camelCase` private fields; `I` prefix for interfaces; 4-space indent (C#); central package management

## Implementation Checklist

### [ ] Implementation Phase 1: Package and Solution Infrastructure

<!-- parallelizable: false -->

* [ ] Step 1.1: Add `ModelContextProtocol.AspNetCore` package version to `Directory.Packages.props`
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 14-28)
* [ ] Step 1.2: Create solution folder and `.csproj` file for the new McpServer project
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 29-65)
* [ ] Step 1.3: Add McpServer project to `PromptBabbler.slnx`
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 66-80)

### [ ] Implementation Phase 2: HTTP Client Layer

<!-- parallelizable: false -->

* [ ] Step 2.1: Create `Client/Models/` DTOs mirroring API JSON contracts
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 83-165)
* [ ] Step 2.2: Create `Client/IPromptBabblerApiClient.cs` interface
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 166-200)
* [ ] Step 2.3: Create `Client/ApiAuthDelegatingHandler.cs` for auth header injection
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 201-245)
* [ ] Step 2.4: Create `Client/PromptBabblerApiClient.cs` typed HTTP client implementation
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 246-370)

### [ ] Implementation Phase 3: MCP Tools

<!-- parallelizable: true -->

* [ ] Step 3.1: Create `Tools/BabbleTools.cs` — search, list, get, generate_prompt
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 373-445)
* [ ] Step 3.2: Create `Tools/PromptTemplateTools.cs` — list, get, create, update, delete
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 446-520)
* [ ] Step 3.3: Create `Tools/GeneratedPromptTools.cs` — list, get
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 521-560)

### [ ] Implementation Phase 4: MCP Resources and Prompts

<!-- parallelizable: true -->

* [ ] Step 4.1: Create `Resources/TemplateResources.cs` — template list and URI template per ID
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 563-600)
* [ ] Step 4.2: Create `Prompts/TemplateReviewPrompt.cs` — user-triggered slash command returning static ChatMessage array
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 601-630)

### [ ] Implementation Phase 5: Application Bootstrap

<!-- parallelizable: false -->

* [ ] Step 5.1: Create `McpAccessCodeMiddleware.cs` for access-code validation
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 633-680)
* [ ] Step 5.2: Create `Program.cs` with MCP server setup, conditional auth, and HttpClient registration
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 681-760)
* [ ] Step 5.3: Create `appsettings.json` with placeholder configuration
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 761-790)
* [ ] Step 5.4: Create `Properties/launchSettings.json` for local development
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 791-825)

### [ ] Implementation Phase 6: Aspire Integration

<!-- parallelizable: false -->

* [ ] Step 6.1: Add `ProjectReference` for McpServer to `PromptBabbler.AppHost.csproj`
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 828-845)
* [ ] Step 6.2: Add `mcp-server` resource to `AppHost.cs` (after apiService, before frontend)
  * Details: .copilot-tracking/details/2026-05-05/mcp-server-details.md (Lines 846-885)

### [ ] Implementation Phase 7: Final Validation

<!-- parallelizable: false -->

* [ ] Step 7.1: Run full project validation
  * `cd prompt-babbler-service && dotnet build PromptBabbler.slnx`
  * `dotnet format PromptBabbler.slnx --verify-no-changes --severity error`
  * `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit --configuration Release`
* [ ] Step 7.2: Fix minor validation issues (lint errors, build warnings)
* [ ] Step 7.3: Report blocking issues requiring additional research

## Planning Log

See .copilot-tracking/plans/logs/2026-05-05/mcp-server-log.md for discrepancy tracking, implementation paths considered, and suggested follow-on work.

## Dependencies

* .NET 10 SDK
* `ModelContextProtocol.AspNetCore` v1.2.0 (to be added to Directory.Packages.props)
* `Microsoft.AspNetCore.Authentication.JwtBearer` (already in Directory.Packages.props — verify)
* `Microsoft.Identity.Web` (already in Directory.Packages.props — verify)
* `PromptBabbler.ServiceDefaults` project (already exists)
* Aspire AppHost project already wired with apiService

## Success Criteria

* `PromptBabbler.McpServer` builds cleanly with no warnings — Traces to: Directory.Build.props TreatWarningsAsErrors requirement
* `/mcp` endpoint served via Streamable HTTP transport (stateless) — Traces to: user requirement + MCP spec
* 11 MCP Tools accessible: `search_babbles`, `list_babbles`, `get_babble`, `generate_prompt`, `list_templates`, `get_template`, `create_template`, `update_template`, `delete_template`, `list_generated_prompts`, `get_generated_prompt` — Traces to: user requirement (babble search/read + template CRUD + generated prompt read + prompt generation)
* `babbler://templates` and `babbler://templates/{id}` MCP Resources exposed — Traces to: user requirement (MCP Resources for browsable data)
* `review_template` MCP Prompt exposed as user-triggered slash command — Traces to: user requirement (MCP Prompts for common workflows)
* Anonymous auth mode works with no configuration — Traces to: user requirement (anonymous for local development)
* Aspire AppHost starts the MCP server alongside the API — Traces to: user requirement (Aspire integration)
* All classes are `sealed`; all private fields use `_camelCase`; interfaces use `I` prefix — Traces to: .github/copilot-instructions.md and AGENTS.md conventions
* Project appears in `PromptBabbler.slnx` under `/src/` folder — Traces to: user requirement (add to solution file)
* `dotnet format --verify-no-changes` passes — Traces to: CI pipeline requirement in AGENTS.md
