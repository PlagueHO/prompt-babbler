---
title: Testing Guide
description: Automated testing strategy for Prompt Babbler, covering backend and frontend test types, execution commands, mocking approach, and runtime dependencies.
author: Prompt Babbler Team
ms.date: 2026-05-17
ms.topic: how-to
keywords:
  - testing
  - unit tests
  - integration tests
  - smoke tests
  - vitest
  - mstest
  - aspire
estimated_reading_time: 8
---

## Automated Testing at a Glance

| Layer | Test type | Primary scope | Runner | Key entry points |
|---|---|---|---|---|
| Frontend | Unit and component tests | React components, hooks, services, utility functions | Vitest + Testing Library | [prompt-babbler-app/package.json](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-app/package.json), [prompt-babbler-app/vitest.config.ts](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-app/vitest.config.ts), [.vscode/tasks.json](https://github.com/PlagueHO/prompt-babbler/blob/main/.vscode/tasks.json) |
| Frontend | Accessibility-focused tests | Accessibility assertions for selected UI components | Vitest + jest-axe | [TagInput.test.tsx](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-app/tests/components/ui/TagInput.test.tsx), [ErrorBanner.test.tsx](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-app/tests/components/ui/ErrorBanner.test.tsx), [AccessCodeDialog.test.tsx](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-app/tests/components/layout/AccessCodeDialog.test.tsx) |
| Backend | Unit tests | Controllers, services, health checks, client logic | MSTest + FluentAssertions + NSubstitute | [prompt-babbler-service/tests/unit](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/unit), [.vscode/tasks.json](https://github.com/PlagueHO/prompt-babbler/blob/main/.vscode/tasks.json) |
| Backend | Integration tests | API pipeline behavior, startup/auth mode wiring, Aspire resource validation | MSTest + WebApplicationFactory + Aspire testing | [prompt-babbler-service/tests/integration](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/integration), [.vscode/tasks.json](https://github.com/PlagueHO/prompt-babbler/blob/main/.vscode/tasks.json) |
| End-to-end smoke | Deployed service checks | API, frontend, MCP health and critical endpoint readiness | Pester (PowerShell) | [tests/smoke/Smoke.Tests.ps1](https://github.com/PlagueHO/prompt-babbler/blob/main/tests/smoke/Smoke.Tests.ps1) |

## Frontend Automated Tests

### What is covered

* Component behavior and rendering
* Hook behavior and state transitions
* Service-level logic
* Utility functions
* Accessibility assertions for selected components

Examples:

* Components: [prompt-babbler-app/tests/components](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-app/tests/components)
* Hooks: [prompt-babbler-app/tests/hooks](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-app/tests/hooks)
* Services: [prompt-babbler-app/tests/services](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-app/tests/services)
* Utilities: [prompt-babbler-app/tests/lib](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-app/tests/lib)

### How to run

From [prompt-babbler-app](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-app):

1. Install dependencies.

```bash
pnpm install
```

1. Run all frontend tests once.

```bash
pnpm test
```

1. Run in watch mode.

```bash
pnpm test:watch
```

1. Run coverage.

```bash
pnpm test:coverage
```

1. Run accessibility-focused tests only.

```bash
pnpm test -t accessibility
```

Equivalent VS Code tasks are defined in [.vscode/tasks.json](https://github.com/PlagueHO/prompt-babbler/blob/main/.vscode/tasks.json):

* app: test
* app: test (watch)
* app: test (accessibility)

### Mocks and fakes used

* Vitest module mocking is used heavily through `vi.mock` and `vi.mocked`, especially around API client and hook dependencies.
* JSDOM is used as the browser-like environment.

References:

* [prompt-babbler-app/vitest.config.ts](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-app/vitest.config.ts)
* [useBabbles.test.ts](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-app/tests/hooks/useBabbles.test.ts)
* [SettingsPage.test.tsx](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-app/tests/components/settings/SettingsPage.test.tsx)

### Dependencies needed

* Node.js and pnpm
* Frontend package dependencies from [prompt-babbler-app/package.json](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-app/package.json)
* No Docker is required for frontend unit and component tests

## Backend Unit Tests

### What is covered

* API controllers and health checks
* API client logic
* Infrastructure and domain logic
* MCP server and CLI units

Projects:

* [Api.UnitTests](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/unit/Api.UnitTests)
* [ApiClient.UnitTests](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/unit/ApiClient.UnitTests)
* [Domain.UnitTests](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/unit/Domain.UnitTests)
* [Infrastructure.UnitTests](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/unit/Infrastructure.UnitTests)
* [McpServer.UnitTests](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/unit/McpServer.UnitTests)
* [Tools.Cli.UnitTests](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/unit/Tools.Cli.UnitTests)

### How to run

From [prompt-babbler-service](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service):

1. Restore.

```bash
dotnet restore PromptBabbler.slnx
```

1. Run unit tests.

```bash
dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit --configuration Release --no-restore
```

Recommended task:

* service: test (unit) in [.vscode/tasks.json](https://github.com/PlagueHO/prompt-babbler/blob/main/.vscode/tasks.json)

### Mocks and fakes used

* NSubstitute is the primary mocking library for backend unit tests.
* FluentAssertions provides assertion style.

Examples:

* [UserControllerTests.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/UserControllerTests.cs)
* [CosmosDbHealthCheckTests.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/unit/Api.UnitTests/HealthChecks/CosmosDbHealthCheckTests.cs)

### Dependencies needed

* .NET SDK 10
* NuGet packages centrally managed in [prompt-babbler-service/Directory.Packages.props](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/Directory.Packages.props)
* No Docker is required for backend unit tests

## Backend Integration Tests

### What is covered

* API endpoint behavior through ASP.NET Core pipeline
* Auth mode startup behavior
* Health endpoint behavior
* Aspire resource lifecycle and minimal infrastructure integration checks

Projects:

* [Api.IntegrationTests](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/integration/Api.IntegrationTests)
* [Infrastructure.IntegrationTests](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/integration/Infrastructure.IntegrationTests)
* [Orchestration.IntegrationTests](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/integration/Orchestration.IntegrationTests)
* Shared fixture utilities in [IntegrationTests.Shared](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service/tests/integration/IntegrationTests.Shared)

### How to run

From [prompt-babbler-service](https://github.com/PlagueHO/prompt-babbler/tree/main/prompt-babbler-service):

1. Run all integration tests by category.

```bash
dotnet test --solution PromptBabbler.slnx --filter TestCategory=Integration --configuration Release --no-restore --results-directory ./TestResults
```

1. Run API integration tests only.

```bash
dotnet test --project tests/integration/Api.IntegrationTests/PromptBabbler.Api.IntegrationTests.csproj --filter TestCategory=Integration --configuration Release --no-restore --results-directory ./TestResults
```

1. Run Infrastructure integration tests only.

```bash
dotnet test --project tests/integration/Infrastructure.IntegrationTests/PromptBabbler.Infrastructure.IntegrationTests.csproj --filter TestCategory=Integration --configuration Release --no-restore --results-directory ./TestResults
```

1. Run Orchestration integration tests only.

```bash
dotnet test --project tests/integration/Orchestration.IntegrationTests/PromptBabbler.Orchestration.IntegrationTests.csproj --filter TestCategory=Integration --configuration Release --no-restore --results-directory ./TestResults
```

VS Code tasks:

* service: test (integration)
* service: test (integration - Api)
* service: test (integration - Infrastructure)
* service: test (integration - Orchestration)

Defined in [.vscode/tasks.json](https://github.com/PlagueHO/prompt-babbler/blob/main/.vscode/tasks.json).

### Mocks and fakes used

API integration tests use a hybrid approach:

* Real in-process host through WebApplicationFactory
* Authentication simulation via custom test auth handler
* Domain service substitutions via NSubstitute
* Startup-safe replacements for external dependencies during tests

Key references:

* [CustomWebApplicationFactory.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/integration/Api.IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs)
* [NoAuthWebApplicationFactory.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/integration/Api.IntegrationTests/Infrastructure/NoAuthWebApplicationFactory.cs)
* [AnonymousModeWebApplicationFactory.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/integration/Api.IntegrationTests/Infrastructure/AnonymousModeWebApplicationFactory.cs)
* [TestAuthHandler.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/integration/Api.IntegrationTests/Infrastructure/TestAuthHandler.cs)

Infrastructure integration uses Aspire AppHost fixture lifecycle:

* [AppHostFixture.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/integration/IntegrationTests.Shared/AppHostFixture.cs)

### Dependencies needed

* .NET SDK 10
* Docker Desktop for tests that start or depend on Aspire resources and Cosmos emulator
* Aspire test dependencies from [prompt-babbler-service/Directory.Packages.props](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/Directory.Packages.props)
* Non-parallel execution is enforced in integration assemblies:
  * [Api.IntegrationTests/AssemblyInfo.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/integration/Api.IntegrationTests/AssemblyInfo.cs)
  * [Infrastructure.IntegrationTests/AssemblyInfo.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/integration/Infrastructure.IntegrationTests/AssemblyInfo.cs)
  * [Orchestration.IntegrationTests/AssemblyInfo.cs](https://github.com/PlagueHO/prompt-babbler/blob/main/prompt-babbler-service/tests/integration/Orchestration.IntegrationTests/AssemblyInfo.cs)

## Smoke Tests

### What is covered

Smoke tests validate deployed environment readiness and core API/frontend/MCP routes:

* API health and liveness
* Babbles and templates endpoints
* Transcription WebSocket startup behavior
* Frontend page and static asset serving
* MCP server health and dependency visibility

Reference:

* [tests/smoke/Smoke.Tests.ps1](https://github.com/PlagueHO/prompt-babbler/blob/main/tests/smoke/Smoke.Tests.ps1)

### How to run

Run the PowerShell smoke script with required base URLs and optional access code. The script expects running endpoints and performs health waits and retries before assertions.

### Mocks and fakes used

* No mocks
* These are live endpoint checks against running services

### Dependencies needed

* PowerShell with Pester support
* Reachable API, frontend, and MCP URLs
* Access code when environment is access-code protected

## Coverage and Reporting

Backend coverage workflow:

1. Run unit tests with coverage output to Cobertura via service: test (unit).
1. Generate reports using service: code coverage (report).

Task definitions:

* [.vscode/tasks.json](https://github.com/PlagueHO/prompt-babbler/blob/main/.vscode/tasks.json)

## Practical Notes

* The solution-level integration filter can return a non-success process code when some projects have zero matching tests for the filter, even if integration tests that exist pass.
* For a precise API integration signal, use the API integration project-specific command or task.
