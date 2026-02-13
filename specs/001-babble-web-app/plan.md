# Implementation Plan: Prompt Babbler — Speech-to-Prompt Web Application

**Branch**: `001-babble-web-app` | **Date**: 2026-02-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-babble-web-app/spec.md`

## Summary

Build a local-first web application that captures speech via the browser's MediaRecorder API, transcribes it using Azure OpenAI Whisper through a .NET 10 backend proxy, and generates structured prompts for target systems (e.g., GitHub Copilot) via Azure OpenAI LLM. The architecture follows Libris-Maleficarum patterns: Clean Architecture backend (Api → Domain → Infrastructure), React 19 + TypeScript + Vite frontend, Aspire AppHost orchestration, and pnpm monorepo with GitHub Actions CI/CD. Babbles, templates, and generated prompts are stored in browser localStorage; LLM settings are stored server-side in `~/.prompt-babbler/settings.json`. The backend binds to localhost only and serves three API groups: prompt generation (SSE streaming), audio transcription (Whisper proxy), and settings management.

## Technical Context

**Language/Version**: TypeScript 5.x (frontend), C# / .NET 10.0.100 (backend)
**Primary Dependencies**: React 19, Vite, Shadcn/UI, TailwindCSS v4, React Hook Form + Zod (frontend); ASP.NET Core, Azure.AI.OpenAI v2.1+, .NET Aspire 13.1 (backend)
**Storage**: Browser localStorage (babbles, templates, prompts); local config file `~/.prompt-babbler/settings.json` (LLM settings)
**Testing**: Vitest + Testing Library (frontend); MSTest + FluentAssertions (backend)
**Target Platform**: Local machine — Chrome 49+, Edge 79+, Safari 14.1+, Firefox 25+ (MediaRecorder API required)
**Project Type**: Web application (frontend + backend monorepo)
**Performance Goals**: App interactive < 3s; transcription chunk latency ~5s; prompt generation < 30s (excl. LLM response time)
**Constraints**: Backend binds to `localhost` / `127.0.0.1` only; audio chunks `audio/webm;codecs=opus` max 25 MB; localStorage warn at 80% quota; UUID v4 for all entity IDs; interim transcription persisted after every chunk (~5s)
**Scale/Scope**: Single user, local-first, 5 user stories (P1–P5), ~100-200 babbles in localStorage

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Simplicity & YAGNI | **PASS** | V1 backend is minimal (3 controller groups). No Redux, no Docker, no cloud infra. Deferred: prompt history (V2), export/import (V2), Azure hosting (V2). |
| II. Clean Code & Readability | **PASS** | SOLID via Clean Architecture. Automated formatting: ESLint + Prettier (frontend), `dotnet format` (backend). Self-documenting TypeScript interfaces and C# records. |
| III. Modularity & Library-First | **PASS** | Clean Architecture layers with explicit boundaries. Frontend organized by feature hooks (`useAudioRecording`, `useTranscription`, `usePromptGeneration`). No circular dependencies. |
| IV. Test-First Development | **PASS** | TDD mandated. Vitest + Testing Library for frontend, MSTest + FluentAssertions for backend. Coverage tracked via Cobertura. E2E testing deferred to V2. |
| V. Integration Testing Over Mocks | **PASS** | Backend integration tests use real HTTP pipeline (WebApplicationFactory). Frontend tests use Testing Library (real DOM). Mocks only for Azure OpenAI SDK (external service). |
| VI. Industry-Standard Dependencies | **PASS** | All dependencies are mainstream: React 19, Vite, Shadcn/UI, TailwindCSS v4, Azure.AI.OpenAI, .NET Aspire. No niche libraries. |
| VII. Azure-First & Cost Optimization | **PASS** | Azure OpenAI is the sole cloud dependency. `azure.yaml` + `infra/` placeholder scaffolded for azd. Bicep-only IaC (deferred). No Terraform. |

**Pre-research gate**: PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-babble-web-app/
├── plan.md              # This file
├── research.md          # Phase 0 output — technology decisions
├── data-model.md        # Phase 1 output — entity schemas
├── quickstart.md        # Phase 1 output — developer setup guide
├── contracts/
│   └── api.yaml         # Phase 1 output — OpenAPI 3.1 contract
├── checklists/
│   └── requirements.md  # Requirements traceability checklist
└── tasks.md             # Phase 2 output — implementation tasks
```

### Source Code (repository root)

```text
prompt-babbler/
├── prompt-babbler-service/                       # .NET backend monorepo
│   ├── PromptBabbler.slnx                        # Solution file
│   ├── global.json                               # .NET SDK + MSTest SDK versions
│   ├── Directory.Build.props                     # Shared build properties
│   ├── Directory.Packages.props                  # Central package management
│   ├── src/
│   │   ├── Api/                                  # ASP.NET Core API project
│   │   │   ├── Controllers/
│   │   │   │   ├── PromptController.cs           # POST /api/prompts/generate (SSE streaming)
│   │   │   │   ├── TranscriptionController.cs    # POST /api/transcribe (Whisper proxy)
│   │   │   │   └── SettingsController.cs         # GET/PUT /api/settings, POST /api/settings/test
│   │   │   ├── Models/
│   │   │   │   ├── Requests/                     # GeneratePromptRequest, LlmSettingsSaveRequest
│   │   │   │   └── Responses/                    # GeneratePromptResponse, LlmSettingsResponse, etc.
│   │   │   └── Program.cs                        # DI registration, middleware, CORS
│   │   ├── Domain/                               # Business models & interfaces
│   │   │   ├── Models/
│   │   │   │   └── LlmSettings.cs
│   │   │   └── Interfaces/
│   │   │       ├── ISettingsService.cs
│   │   │       ├── IPromptGenerationService.cs
│   │   │       └── ITranscriptionService.cs
│   │   ├── Infrastructure/                       # External service implementations
│   │   │   ├── Services/
│   │   │   │   ├── FileSettingsService.cs        # Read/write ~/.prompt-babbler/settings.json
│   │   │   │   ├── AzureOpenAiPromptGenerationService.cs
│   │   │   │   └── AzureOpenAiTranscriptionService.cs
│   │   │   └── DependencyInjection.cs
│   │   └── Orchestration/
│   │       ├── AppHost/                          # Aspire AppHost
│   │       │   └── AppHost.cs                    # Orchestrates API + frontend
│   │       └── ServiceDefaults/                  # Shared OpenTelemetry, health checks
│   │           └── Extensions.cs
│   └── tests/
│       ├── unit/
│       │   ├── Api.UnitTests/
│       │   ├── Domain.UnitTests/
│       │   └── Infrastructure.UnitTests/
│       └── integration/
│           ├── Api.IntegrationTests/
│           ├── Infrastructure.IntegrationTests/
│           └── Orchestration.IntegrationTests/
│
├── prompt-babbler-app/                           # React frontend
│   ├── package.json
│   ├── tsconfig.json
│   ├── vite.config.ts
│   ├── vitest.config.ts
│   ├── index.html
│   ├── src/
│   │   ├── main.tsx                              # App entry point
│   │   ├── App.tsx                               # Root component + routing
│   │   ├── components/
│   │   │   ├── ui/                               # Shadcn/UI primitives
│   │   │   ├── recording/                        # RecordButton, RecordingIndicator, TranscriptPreview
│   │   │   ├── babbles/                          # BabbleList, BabbleCard, BabbleEditor, DeleteBabbleDialog
│   │   │   ├── prompts/                          # PromptGenerator, PromptDisplay, TemplatePicker, CopyButton
│   │   │   ├── templates/                        # TemplateList, TemplateEditor, TemplateCard
│   │   │   ├── settings/                         # SettingsForm, ConnectionTest, LanguageSelector
│   │   │   └── layout/                           # Header, PageLayout, ErrorBoundary, StorageWarning, SettingsRequiredBanner, BrowserCheck
│   │   ├── pages/                                # HomePage, RecordPage, BabblePage, TemplatesPage, SettingsPage
│   │   ├── hooks/
│   │   │   ├── useAudioRecording.ts              # MediaRecorder wrapper
│   │   │   ├── useTranscription.ts               # POST /api/transcribe per chunk
│   │   │   ├── usePromptGeneration.ts            # POST /api/prompts/generate (SSE)
│   │   │   ├── useLocalStorage.ts                # Generic localStorage hook
│   │   │   ├── useBabbles.ts                     # CRUD operations on babbles
│   │   │   ├── useTemplates.ts                   # CRUD operations on templates
│   │   │   └── useSettings.ts                    # GET/PUT /api/settings
│   │   ├── services/
│   │   │   ├── api-client.ts                     # Fetch wrapper for backend API
│   │   │   ├── local-storage.ts                  # localStorage utilities + quota check
│   │   │   └── default-templates.ts              # Built-in template definitions
│   │   ├── types/
│   │   │   └── index.ts                          # Babble, PromptTemplate, GeneratedPrompt, etc.
│   │   └── lib/
│   │       └── utils.ts                          # cn() helper, date formatting
│   └── tests/
│       ├── components/
│       ├── hooks/
│       └── services/
│
├── .github/
│   ├── workflows/
│   │   ├── continuous-integration.yml            # Main orchestrator
│   │   ├── build-and-publish-backend-service.yml # .NET CI
│   │   ├── build-and-publish-frontend-app.yml    # React CI
│   │   ├── lint-markdown.yml                     # markdownlint-cli2
│   │   └── set-build-variables.yml               # GitVersion
│   └── prompts/                                  # SpecKit prompt files
│
├── azure.yaml                                    # azd manifest (placeholder for vNext)
├── infra/
│   └── README.md                                 # Planned Bicep IaC (deferred)
│
└── specs/                                        # Feature specifications
    └── 001-babble-web-app/
```

**Structure Decision**: Web application (Option 2) — adapted to Libris-Maleficarum naming conventions. Backend in `prompt-babbler-service/`, frontend in `prompt-babbler-app/`, Clean Architecture with Api/Domain/Infrastructure/Orchestration layers, Aspire AppHost orchestration.

## Complexity Tracking

> No constitution violations requiring justification. All design decisions align with the 7 principles.
