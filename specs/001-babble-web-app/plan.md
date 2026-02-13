# Implementation Plan: Prompt Babbler — Speech-to-Prompt Web Application

**Branch**: `001-babble-web-app` | **Date**: 2026-02-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-babble-web-app/spec.md`

## Summary

Build a local-first web application that records stream-of-consciousness speech ("babbles"), transcribes it to text via the browser's Web Speech API, and transforms it into structured prompts for target systems (e.g., GitHub Copilot) using Azure OpenAI. The frontend is React 19 + TypeScript + Vite + Shadcn/UI + TailwindCSS v4; a .NET 10 ASP.NET Core backend proxies LLM calls to Azure OpenAI (CORS workaround) and persists LLM settings to disk. Speech recognition runs entirely in the browser — no audio is sent to the backend. Local development is orchestrated by .NET Aspire AppHost. Project structure, CI/CD, and conventions follow the [Libris-Maleficarum](https://github.com/PlagueHO/Libris-Maleficarum) repository. The solution is structured to be deployable via Azure Developer CLI (`azd`) in a future iteration (vNext), with a placeholder `infra/` folder and `azure.yaml`. In vNext, STT will migrate to a server-side model deployed in Azure AI Foundry (model TBD — could be OpenAI gpt-4o-transcribe, Whisper, or another provider).

## Technical Context

**Language/Version**: .NET 10 / C# 13 (backend), TypeScript 5.9+ / React 19 (frontend)
**Primary Dependencies**: ASP.NET Core, .NET Aspire 13.1, Azure.AI.OpenAI 2.1+ (LLM only), React 19, Shadcn/UI, TailwindCSS v4, Vite 7.x, Sonner (toasts), React Hook Form + Zod (forms), Lucide React (icons)
**Storage**: Browser localStorage (babbles, templates, last prompt — V1); local config file `~/.prompt-babbler/settings.json` (LLM settings — backend)
**Testing**: MSTest SDK 4.1 + FluentAssertions + NSubstitute (backend), Vitest 4.x + Testing Library + jest-axe (frontend)
**Target Platform**: Local desktop (Windows/macOS/Linux), modern browser (Chrome 33+, Edge 79+, Safari 14.1+)
**Project Type**: Web application (frontend + backend)
**Performance Goals**: <3s initial load, <2s page transitions, <30s prompt generation (excl. LLM latency)
**Constraints**: Single user, local-first, no authentication, offline recording allowed (Web Speech API requires internet for Chrome), online required for prompt generation
**Scale/Scope**: 1 user, ~50 babbles typical, ~20 templates, 5 pages/views

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Pre-Design | Post-Design | Notes |
|---|-----------|------------|-------------|-------|
| I | Simplicity & YAGNI | **PASS** | **PASS** | V1 defers backend storage, auth, export/import, prompt history. Backend proxies LLM calls only — CORS requires it (justified by concrete blocker). STT uses browser Web Speech API (zero backend overhead). vNext will migrate STT to Azure AI Foundry (model TBD). AZD `infra/` is a placeholder only; no premature IaC. |
| II | Clean Code & Readability | **PASS** | **PASS** | Automated formatting (dotnet format, ESLint/Prettier), SOLID, self-documenting code enforced. |
| III | Modularity & Library-First | **PASS** | **PASS** | Clean Architecture layers (Api/Domain/Infrastructure) each serve a distinct purpose. Frontend uses component-per-feature. No organizational-only modules. |
| IV | Test-First Development | **PASS** | **PASS** | TDD mandated. MSTest + FluentAssertions backend, Vitest + Testing Library frontend. Unit tests required before merge. |
| V | Integration Testing Over Mocks | **PASS** | **PASS** | Aspire integration tests for API. MSW for frontend dev mocking only. Real HTTP in integration tests. Mock only Azure OpenAI (external third-party). |
| VI | Industry-Standard Dependencies | **PASS** | **PASS** | All dependencies are widely adopted: React 19, Vite, Shadcn/UI, TailwindCSS, Azure.AI.OpenAI (official MS SDK), Aspire (official MS orchestration). |
| VII | Azure-First & Cost Optimization | **PASS** | **PASS** | V1 is local-only (no Azure spend). Architecture designed for future Azure Container Apps deployment via AZD. Azure OpenAI is the LLM provider. `azure.yaml` + `infra/` scaffolded as placeholders for vNext. |

**Gate result: PASS — no violations.**

## Project Structure

### Documentation (this feature)

```text
specs/001-babble-web-app/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── api.yaml         # OpenAPI spec for backend API
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
prompt-babbler/
├── .github/
│   ├── agents/
│   │   └── copilot-instructions.md         # Auto-generated Copilot context
│   └── workflows/
│       ├── continuous-integration.yml          # Main CI orchestrator
│       ├── build-and-publish-backend-service.yml  # Reusable: .NET build/test/publish
│       ├── build-and-publish-frontend-app.yml     # Reusable: pnpm build/test/publish
│       ├── lint-markdown.yml                      # Reusable: markdownlint
│       └── set-build-variables.yml                # Reusable: GitVersion
├── .vscode/
│   ├── tasks.json
│   └── launch.json
├── prompt-babbler-service/                     # .NET backend
│   ├── PromptBabbler.slnx                      # Solution file (.slnx format)
│   ├── global.json                             # .NET SDK version + MSTest SDK
│   ├── src/
│   │   ├── Api/                                # ASP.NET Core Web API
│   │   │   ├── PromptBabbler.Api.csproj
│   │   │   ├── Program.cs
│   │   │   ├── Controllers/
│   │   │   │   ├── PromptController.cs         # POST /api/prompts/generate (streaming)
│   │   │   │   └── SettingsController.cs       # GET/PUT /api/settings, POST /api/settings/test
│   │   │   ├── Models/
│   │   │   │   ├── Requests/
│   │   │   │   │   └── GeneratePromptRequest.cs
│   │   │   │   └── Responses/
│   │   │   │       ├── GeneratePromptResponse.cs
│   │   │   │       ├── LlmSettingsResponse.cs
│   │   │   │       └── TestConnectionResponse.cs
│   │   │   ├── Services/
│   │   │   │   └── SettingsFileService.cs      # Read/write ~/.prompt-babbler/settings.json
│   │   │   ├── appsettings.json
│   │   │   └── appsettings.Development.json
│   │   ├── Domain/                             # Core business logic (minimal in V1)
│   │   │   ├── PromptBabbler.Domain.csproj
│   │   │   ├── Models/
│   │   │   │   └── LlmSettings.cs
│   │   │   └── Interfaces/
│   │   │       ├── ISettingsService.cs
│   │   │       └── IPromptGenerationService.cs
│   │   ├── Infrastructure/                     # External integrations
│   │   │   ├── PromptBabbler.Infrastructure.csproj
│   │   │   └── Services/
│   │   │       ├── AzureOpenAiPromptGenerationService.cs
│   │   │       └── FileSettingsService.cs
│   │   └── Orchestration/
│   │       ├── AppHost/
│   │       │   ├── PromptBabbler.AppHost.csproj  # Aspire.AppHost.Sdk/13.1.0
│   │       │   ├── AppHost.cs                    # Aspire DistributedApplication builder
│   │       │   ├── appsettings.json
│   │       │   └── appsettings.Development.json
│   │       └── ServiceDefaults/
│   │           ├── PromptBabbler.ServiceDefaults.csproj
│   │           └── Extensions.cs               # OpenTelemetry, health checks, resilience
│   └── tests/
│       ├── Api.Tests/
│       │   └── PromptBabbler.Api.Tests.csproj
│       ├── Domain.Tests/
│       │   └── PromptBabbler.Domain.Tests.csproj
│       ├── Infrastructure.Tests/
│       │   └── PromptBabbler.Infrastructure.Tests.csproj
│       └── Api.IntegrationTests/
│           └── PromptBabbler.Api.IntegrationTests.csproj
├── prompt-babbler-app/                         # React frontend
│   ├── package.json
│   ├── pnpm-lock.yaml
│   ├── vite.config.ts
│   ├── vitest.config.ts
│   ├── vitest.setup.ts
│   ├── tsconfig.json
│   ├── tsconfig.app.json
│   ├── tsconfig.node.json
│   ├── eslint.config.js
│   ├── components.json                         # Shadcn/UI config
│   ├── index.html
│   ├── src/
│   │   ├── main.tsx
│   │   ├── App.tsx
│   │   ├── index.css                           # TailwindCSS v4 entry
│   │   ├── components/
│   │   │   ├── ui/                             # Shadcn/UI primitives (button, card, input, etc.)
│   │   │   ├── recording/                      # RecordButton, LiveTranscript, RecordingControls
│   │   │   ├── babbles/                        # BabbleList, BabbleDetail, BabbleEditor
│   │   │   ├── prompts/                        # PromptGenerator, PromptDisplay, CopyButton
│   │   │   ├── templates/                      # TemplateList, TemplateEditor, TemplateCard
│   │   │   ├── settings/                       # LlmSettingsForm, LanguageSelector, TestConnection
│   │   │   └── shared/                         # Layout, Navigation, ErrorBoundary
│   │   ├── hooks/
│   │   │   ├── useSpeechRecognition.ts         # Web Speech API hook (real-time transcription)
│   │   │   ├── useLocalStorage.ts              # Typed localStorage hook
│   │   │   └── usePromptGeneration.ts          # API call + streaming hook
│   │   ├── services/
│   │   │   ├── api.ts                          # Backend API client (fetch)
│   │   │   ├── localStorage.ts                 # Babble/template storage
│   │   │   └── types/
│   │   │       └── index.ts                    # Shared TypeScript types
│   │   ├── lib/
│   │   │   └── utils.ts                        # Shadcn/UI cn() utility
│   │   └── pages/
│   │       ├── HomePage.tsx                    # Dashboard / babble list
│   │       ├── RecordPage.tsx                  # Recording view
│   │       ├── BabblePage.tsx                  # Babble detail + prompt generation
│   │       ├── TemplatesPage.tsx               # Template management
│   │       └── SettingsPage.tsx                # LLM + speech settings
│   └── tests/
│       └── (colocated with src/ via *.test.tsx)
├── infra/                                      # Azure infrastructure (vNext placeholder)
│   └── README.md                               # Documents future Bicep IaC plans
├── azure.yaml                                  # Azure Developer CLI manifest (vNext placeholder)
├── package.json                                # Root: markdownlint scripts
├── pnpm-lock.yaml                              # Root: markdownlint deps
├── GitVersion.yml                              # Semantic versioning config
├── .gitignore
├── .gitattributes
├── .markdownlint.json
├── .markdownlint-cli2.jsonc
├── LICENSE
├── CHANGELOG.md
├── AGENTS.md
└── README.md
```

**Structure Decision**: Follows the Libris-Maleficarum monorepo pattern with `{name}-service/` for the .NET backend and `{name}-app/` for the React frontend. Clean Architecture with Api → Domain ← Infrastructure, Aspire orchestration (AppHost + ServiceDefaults), .slnx solution format, and flat test project structure. V1 backend has two controllers: PromptController (LLM proxy) and SettingsController (settings CRUD). Speech-to-text runs entirely in the browser via the Web Speech API — no audio is sent to the backend. In vNext, STT will migrate to a server-side model deployed in Azure AI Foundry (model TBD). Babble/template storage remains in browser localStorage. An `infra/` directory and `azure.yaml` are scaffolded as placeholders for vNext Azure Developer CLI deployment (Bicep IaC for Azure Container Apps).

## Complexity Tracking

> No constitution violations to justify.
