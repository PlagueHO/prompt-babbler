# Tasks: Prompt Babbler — Speech-to-Prompt Web Application

**Input**: Design documents from `/specs/001-babble-web-app/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/api.yaml, quickstart.md

**Tests**: Included. Plan mandates TDD (Constitution Principle IV). Backend: MSTest SDK 4.1 + FluentAssertions + NSubstitute. Frontend: Vitest 4 + Testing Library + jest-axe. Tests written first within each user story phase.

**Organization**: Tasks grouped by user story (P1–P5). US6 (Prompt History) is deferred to V2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `prompt-babbler-service/src/` and `prompt-babbler-service/tests/`
- **Frontend source**: `prompt-babbler-app/src/`
- **Frontend tests**: `prompt-babbler-app/tests/` (components/, hooks/, services/)
- **CI/CD**: `.github/workflows/`
- **Orchestration**: `prompt-babbler-service/src/Orchestration/`

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create the complete project structure, solution files, and project scaffolding for both .NET backend and React frontend

- [ ] T001 Create full directory structure for prompt-babbler-service/ (src/Api, src/Domain, src/Infrastructure, src/Orchestration/AppHost, src/Orchestration/ServiceDefaults, tests/unit, tests/integration) and prompt-babbler-app/ (src/components, src/pages, src/hooks, src/services, src/types, src/lib, tests/components, tests/hooks, tests/services) per plan.md
- [ ] T002 Initialize .NET solution with global.json (.NET 10.0.100, MSTest.Sdk 4.1.0), PromptBabbler.slnx, Directory.Build.props (shared build properties), Directory.Packages.props (central package management), and all source project .csproj files (Domain, Infrastructure, Api, AppHost, ServiceDefaults) with correct project references and NuGet package references (Azure.AI.OpenAI 2.1+, Aspire packages) in prompt-babbler-service/
- [ ] T003 [P] Create .NET test projects (Api.UnitTests, Domain.UnitTests, Infrastructure.UnitTests in tests/unit/ and Api.IntegrationTests, Infrastructure.IntegrationTests, Orchestration.IntegrationTests in tests/integration/) with MSTest SDK + FluentAssertions + NSubstitute references in prompt-babbler-service/tests/
- [ ] T004 Initialize React 19 + TypeScript 5.x + Vite frontend with pnpm in prompt-babbler-app/ (package.json, tsconfig.json, tsconfig.app.json, tsconfig.node.json, vite.config.ts, index.html, src/main.tsx)
- [ ] T005 [P] Initialize Shadcn/UI with TailwindCSS v4 and install required components (button, input, textarea, card, dialog, select, badge, separator, skeleton, scroll-area, dropdown-menu) and configure components.json in prompt-babbler-app/
- [ ] T006 [P] Add frontend dependencies: Sonner (toasts), React Hook Form + @hookform/resolvers + Zod (forms), Lucide React (icons), React Router (routing) in prompt-babbler-app/package.json
- [ ] T007 [P] Configure ESLint flat config + Prettier for TypeScript + React in prompt-babbler-app/eslint.config.js
- [ ] T008 [P] Configure Vitest 4 + @testing-library/react + @testing-library/jest-dom + jest-axe in prompt-babbler-app/vitest.config.ts and prompt-babbler-app/vitest.setup.ts
- [ ] T009 [P] Create root config files: .gitignore (Node + .NET + IDE patterns), .gitattributes, GitVersion.yml (ContinuousDelivery mode), .markdownlint.json, .markdownlint-cli2.jsonc
- [ ] T010 [P] Create azure.yaml (service manifest for backend and frontend) and infra/README.md documenting planned Bicep IaC for vNext Azure deployment
- [ ] T011 [P] Create AGENTS.md, .github/agents/copilot-instructions.md (Copilot agent context), and CHANGELOG.md at repository root

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented. Includes backend API scaffolding, domain layer, shared frontend infrastructure, and CI/CD.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T012 Implement ServiceDefaults with OpenTelemetry tracing/metrics, health check endpoints (/health, /alive), and HTTP resilience policies in prompt-babbler-service/src/Orchestration/ServiceDefaults/Extensions.cs and PromptBabbler.ServiceDefaults.csproj
- [ ] T013 Configure Aspire AppHost with API project reference (localhost-only binding per R12) and Vite frontend (AddViteApp("frontend", "../../../../prompt-babbler-app", "dev").WithPnpm()) with service discovery in prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs
- [ ] T014 [P] Create LlmSettings domain model (Endpoint, ApiKey, DeploymentName, WhisperDeploymentName) in prompt-babbler-service/src/Domain/Models/LlmSettings.cs
- [ ] T015 [P] Create domain service interfaces (ISettingsService, IPromptGenerationService, ITranscriptionService) in prompt-babbler-service/src/Domain/Interfaces/
- [ ] T016 Create API request/response model classes per contracts/api.yaml: GeneratePromptRequest, GeneratePromptResponse, LlmSettingsResponse, LlmSettingsSaveRequest, TestConnectionResponse, TranscriptionResponse in prompt-babbler-service/src/Api/Models/
- [ ] T017 Implement FileSettingsService (read/write ~/.prompt-babbler/settings.json with JSON serialization, directory auto-creation, thread-safe file access) in prompt-babbler-service/src/Infrastructure/Services/FileSettingsService.cs
- [ ] T018 [P] Write unit tests for FileSettingsService (read, write, create directory, handle missing file, handle corrupt JSON) in prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/FileSettingsServiceTests.cs
- [ ] T019 Create Infrastructure/DependencyInjection.cs with AddInfrastructure() IServiceCollection extension method (registers FileSettingsService, prompt generation, transcription services); configure API Program.cs calling AddInfrastructure(), CORS policy for frontend origin, Kestrel localhost-only binding to 127.0.0.1 (R12), health check mapping, ProblemDetails middleware, System.Text.Json configuration, and appsettings.json/appsettings.Development.json in prompt-babbler-service/src/Api/ and prompt-babbler-service/src/Infrastructure/
- [ ] T020 Create shared TypeScript types matching data-model.md (Babble, PromptTemplate, GeneratedPrompt, LlmSettingsView, LlmSettingsSaveRequest) with UUID v4 IDs via crypto.randomUUID() (R15) in prompt-babbler-app/src/types/index.ts
- [ ] T021 [P] Implement useLocalStorage typed hook with JSON serialization, error handling, and storage event listening in prompt-babbler-app/src/hooks/useLocalStorage.ts
- [ ] T022 [P] Create API client service with base fetch wrapper, error handling, SSE streaming support, and typed endpoint methods (generatePrompt, transcribeAudio, getSettings, updateSettings, testConnection) in prompt-babbler-app/src/services/api-client.ts
- [ ] T023 Implement localStorage service with CRUD operations for babbles and templates (create, read, update, delete, list sorted by date), quota monitoring with 80% warning threshold (R14), and 100% refusal guard in prompt-babbler-app/src/services/local-storage.ts
- [ ] T024 [P] Create built-in default template definitions ("GitHub Copilot Prompt", "General Assistant Prompt") with isBuiltIn=true as a static data module in prompt-babbler-app/src/services/default-templates.ts
- [ ] T025 Create PageLayout component (header with navigation, main content area), Header component (links to Home, Record, Templates, Settings with active state), and ErrorBoundary component in prompt-babbler-app/src/components/layout/
- [ ] T026 Configure React Router with routes (/ → HomePage, /record → RecordPage, /babbles/:id → BabblePage, /templates → TemplatesPage, /settings → SettingsPage) and App.tsx layout shell in prompt-babbler-app/src/App.tsx
- [ ] T027 [P] Configure index.css with TailwindCSS v4 @import, theme variables, and base styles in prompt-babbler-app/src/index.css
- [ ] T028 [P] Create Shadcn/UI cn() utility function in prompt-babbler-app/src/lib/utils.ts
- [ ] T029 [P] Create CI/CD workflows: continuous-integration.yml (main orchestrator), build-and-publish-backend-service.yml (.NET restore/build/format/test), build-and-publish-frontend-app.yml (pnpm install/lint/test/build), lint-markdown.yml, set-build-variables.yml (GitVersion) in .github/workflows/
- [ ] T030 [P] Configure VS Code launch.json with debug configurations for .NET API and Aspire AppHost in .vscode/launch.json

**Checkpoint**: Foundation ready — all projects compile, frontend dev server starts, Aspire orchestrates both services, CI/CD pipelines are configured. User story implementation can now begin.

---

## Phase 3: User Story 1 — Record a Babble (Priority: P1) 🎯 MVP

**Goal**: Users can record stream-of-consciousness speech, have it transcribed in near-real-time via Azure OpenAI Whisper (~5-second chunks in `audio/webm;codecs=opus` format, R13), and save the result as a new babble in localStorage with UUID v4 IDs (R15).

**Independent Test**: Open the app, grant microphone access, click Record, speak, see transcribed text appear in ~5-second intervals, click Stop, verify babble is saved and persists across page reloads.

### Tests for User Story 1

> **Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T031 [P] [US1] Write unit tests for AzureOpenAiTranscriptionService (transcribe audio chunk, handle missing settings, handle API errors, validate audio format webm/opus) in prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/AzureOpenAiTranscriptionServiceTests.cs
- [ ] T032 [P] [US1] Write unit tests for TranscriptionController (POST /api/transcribe — success, missing file, file exceeds 25 MB, settings not configured, Whisper API error) in prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/TranscriptionControllerTests.cs
- [ ] T033 [P] [US1] Write component tests for RecordButton (render, microphone permission handling, click states) and RecordingIndicator (start/stop toggle, duration timer) in prompt-babbler-app/tests/components/recording/RecordButton.test.tsx and prompt-babbler-app/tests/components/recording/RecordingIndicator.test.tsx

### Implementation for User Story 1

- [ ] T034 [US1] Implement AzureOpenAiTranscriptionService (proxy audio to Azure OpenAI Whisper /audio/transcriptions endpoint, support language parameter, return transcribed text with duration) in prompt-babbler-service/src/Infrastructure/Services/AzureOpenAiTranscriptionService.cs
- [ ] T035 [US1] Implement TranscriptionController with POST /api/transcribe endpoint (accept multipart/form-data audio file up to 25 MB, optional language code, return TranscriptionResponse, handle 400/422/502 per contracts/api.yaml) in prompt-babbler-service/src/Api/Controllers/TranscriptionController.cs
- [ ] T036 [US1] Implement useAudioRecording hook: MediaRecorder wrapper capturing audio/webm;codecs=opus format (R13), ~5-second chunk stop/restart cycle, handle microphone permissions, expose start/stop/isRecording/duration state in prompt-babbler-app/src/hooks/useAudioRecording.ts
- [ ] T037 [US1] Implement useTranscription hook: POST each audio chunk to /api/transcribe, accumulate transcribed text, manage loading/error state per chunk, expose transcribedText/isTranscribing in prompt-babbler-app/src/hooks/useTranscription.ts
- [ ] T038 [US1] Implement useBabbles hook: CRUD operations for babbles in localStorage via local-storage service, generate UUID v4 IDs via crypto.randomUUID() (R15), sort by updatedAt, expose babbles/createBabble/updateBabble/deleteBabble in prompt-babbler-app/src/hooks/useBabbles.ts
- [ ] T039 [P] [US1] Create RecordButton component with microphone permission request, visual recording indicator, and browser MediaRecorder API support detection with fallback message in prompt-babbler-app/src/components/recording/RecordButton.tsx
- [ ] T040 [P] [US1] Create TranscriptPreview component for displaying accumulated transcription text with auto-scroll and interim chunk indicator in prompt-babbler-app/src/components/recording/TranscriptPreview.tsx
- [ ] T041 [P] [US1] Create RecordingIndicator component (Start/Stop buttons, recording duration timer, chunk progress indicator) in prompt-babbler-app/src/components/recording/RecordingIndicator.tsx
- [ ] T042 [US1] Implement RecordPage with full recording flow: start recording → display transcript preview → stop recording → auto-generate title from first ~50 chars → save babble to localStorage with UUID v4 ID and timestamp → navigate to babble detail, including beforeunload warning for active recordings and per-chunk interim persistence of accumulated transcription text after every transcribed chunk (~5s) to prevent data loss (FR-005, R16) in prompt-babbler-app/src/pages/RecordPage.tsx
- [ ] T043 [US1] Add browser MediaRecorder API support detection on app load with user-friendly unsupported-browser message listing supported browsers (Chrome 49+, Edge 79+, Safari 14.1+, Firefox 25+) in prompt-babbler-app/src/components/layout/BrowserCheck.tsx

**Checkpoint**: Users can record speech, see it transcribed in near-real-time, and save babbles. Babbles persist in localStorage across page reloads. MVP core value is delivered.

---

## Phase 4: User Story 2 — Generate a Prompt from a Babble (Priority: P2)

**Goal**: Users can select a babble and a prompt template, generate a structured prompt via Azure OpenAI LLM with streaming output, and copy the result to the clipboard.

**Independent Test**: Select an existing babble, choose a template, click Generate, see the prompt stream in progressively, copy it to clipboard. Verify error messages appear when LLM settings are missing or the API call fails.

### Tests for User Story 2

> **Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T044 [P] [US2] Write unit tests for AzureOpenAiPromptGenerationService (generate with streaming, handle missing settings, handle API errors, validate input) in prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/AzureOpenAiPromptGenerationServiceTests.cs
- [ ] T045 [P] [US2] Write unit tests for PromptController (POST /api/prompts/generate — streaming success, missing fields, settings not configured, LLM error) in prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/PromptControllerTests.cs
- [ ] T046 [P] [US2] Write component tests for PromptGenerator (template selection, generate button states), PromptDisplay (streaming text render), TemplatePicker (template list rendering), and CopyButton (clipboard interaction) in prompt-babbler-app/tests/components/prompts/

### Implementation for User Story 2

- [ ] T047 [US2] Implement AzureOpenAiPromptGenerationService with CompleteChatStreamingAsync (combine systemPrompt + babbleText, yield streamed chunks via IAsyncEnumerable) in prompt-babbler-service/src/Infrastructure/Services/AzureOpenAiPromptGenerationService.cs
- [ ] T048 [US2] Implement PromptController with POST /api/prompts/generate endpoint (accept GeneratePromptRequest, return SSE stream with data: {"text":"chunk"} events and data: [DONE] terminator, handle 400/422/502 per contracts/api.yaml) in prompt-babbler-service/src/Api/Controllers/PromptController.cs
- [ ] T049 [US2] Implement usePromptGeneration hook with fetch + ReadableStream for SSE consumption, progressive text accumulation, loading/error state management in prompt-babbler-app/src/hooks/usePromptGeneration.ts
- [ ] T050 [US2] Implement useTemplates hook: CRUD operations for templates in localStorage via local-storage service, generate UUID v4 IDs for custom templates (R15), expose templates/createTemplate/updateTemplate/deleteTemplate (built-in template seeding is owned by local-storage.ts in T073) in prompt-babbler-app/src/hooks/useTemplates.ts
- [ ] T051 [P] [US2] Create TemplatePicker component with template selector dropdown (populated from localStorage templates via useTemplates), displaying template name and description in prompt-babbler-app/src/components/prompts/TemplatePicker.tsx
- [ ] T052 [P] [US2] Create PromptGenerator component with TemplatePicker integration, Generate button with loading state, and settings-not-configured warning in prompt-babbler-app/src/components/prompts/PromptGenerator.tsx
- [ ] T053 [P] [US2] Create PromptDisplay component with streaming text display, auto-scroll, and skeleton loading state in prompt-babbler-app/src/components/prompts/PromptDisplay.tsx
- [ ] T054 [P] [US2] Create CopyButton component using navigator.clipboard.writeText with success toast (Sonner) and fallback for older browsers in prompt-babbler-app/src/components/prompts/CopyButton.tsx
- [ ] T055 [US2] Implement BabblePage with babble detail view (title, text, timestamps), prompt generation section (PromptGenerator + PromptDisplay + CopyButton), and last generated prompt persistence to babble.lastGeneratedPrompt (UUID v4 ID, R15) in localStorage in prompt-babbler-app/src/pages/BabblePage.tsx

**Checkpoint**: Users can generate structured prompts from babbles using Azure OpenAI with streaming output and copy to clipboard. Core workflow (record → generate → copy) is complete.

---

## Phase 5: User Story 3 — Manage Babbles (Priority: P3)

**Goal**: Users can view, edit, rename, delete babbles, and append additional speech recordings to existing babbles.

**Independent Test**: Create several babbles, view the sorted list on HomePage, open a babble and edit its text, rename another babble, delete a third with confirmation, verify all changes persist. Use "Record More" to append to an existing babble.

### Tests for User Story 3

> **Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T056 [P] [US3] Write component tests for BabbleList (renders sorted list, empty state), BabbleCard (renders title, date, preview), BabbleEditor (edit text, rename, save), and delete confirmation dialog in prompt-babbler-app/tests/components/babbles/

### Implementation for User Story 3

- [ ] T057 [P] [US3] Create BabbleCard component displaying babble title, date, truncated text preview, and action dropdown menu (edit, delete) in prompt-babbler-app/src/components/babbles/BabbleCard.tsx
- [ ] T058 [P] [US3] Create BabbleList component rendering BabbleCard items sorted by most recently modified with action dropdown menus in prompt-babbler-app/src/components/babbles/BabbleList.tsx
- [ ] T059 [P] [US3] Create BabbleEditor component with inline title editing, full text textarea editing, save/cancel actions, and updatedAt timestamp update in prompt-babbler-app/src/components/babbles/BabbleEditor.tsx
- [ ] T060 [US3] Implement HomePage as dashboard with BabbleList, empty state with call-to-action to record first babble, and "New Babble" button linking to RecordPage in prompt-babbler-app/src/pages/HomePage.tsx
- [ ] T061 [US3] Add edit mode toggle and "Record More" (append speech) functionality to BabblePage, allowing users to switch between view/edit modes and append new recordings to existing babble text in prompt-babbler-app/src/pages/BabblePage.tsx
- [ ] T062 [US3] Implement babble delete with confirmation dialog (Shadcn/UI Dialog), permanent removal from localStorage, and redirect to HomePage in prompt-babbler-app/src/components/babbles/DeleteBabbleDialog.tsx

**Checkpoint**: Users can fully manage their babbles — view list, edit text, rename, delete, and append more speech. All CRUD operations persist in localStorage.

---

## Phase 6: User Story 4 — Configure LLM Settings (Priority: P4)

**Goal**: Users can enter, save, and test their Azure OpenAI configuration (endpoint, API key, LLM deployment, Whisper deployment). Settings persist via the backend to ~/.prompt-babbler/settings.json.

**Independent Test**: Navigate to Settings, enter Azure OpenAI credentials, save, verify settings persist across page reloads (API key masked), click "Test Connection" to verify endpoint is reachable.

### Tests for User Story 4

> **Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T063 [P] [US4] Write unit tests for SettingsController (GET returns masked key, PUT saves and returns updated settings, POST /test verifies connection, handles validation errors) in prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/SettingsControllerTests.cs
- [ ] T064 [P] [US4] Write component tests for SettingsForm (field validation, masked API key display, save flow) and ConnectionTest (success/failure states) in prompt-babbler-app/tests/components/settings/

### Implementation for User Story 4

- [ ] T065 [US4] Implement SettingsController with GET /api/settings (return LlmSettingsResponse with masked apiKeyHint), PUT /api/settings (validate and save LlmSettingsSaveRequest), POST /api/settings/test (lightweight Azure OpenAI ping with latency measurement) per contracts/api.yaml in prompt-babbler-service/src/Api/Controllers/SettingsController.cs
- [ ] T066 [US4] Implement useSettings hook: GET/PUT settings from backend API via api-client, manage loading/error/saving state, expose settings/updateSettings/testConnection in prompt-babbler-app/src/hooks/useSettings.ts
- [ ] T067 [P] [US4] Create SettingsForm component with React Hook Form + Zod validation (endpoint URL format, required fields, deployment name constraints), masked API key display for existing settings, and save action with toast feedback in prompt-babbler-app/src/components/settings/SettingsForm.tsx
- [ ] T068 [P] [US4] Create ConnectionTest component with test button, loading spinner, success/failure result display with latency, and error message in prompt-babbler-app/src/components/settings/ConnectionTest.tsx
- [ ] T069 [P] [US4] Create LanguageSelector component with Whisper-supported language dropdown (ISO-639-1 codes), auto-detect default option, persisted to localStorage key prompt-babbler:settings:speechLang in prompt-babbler-app/src/components/settings/LanguageSelector.tsx
- [ ] T070 [US4] Implement SettingsPage with SettingsForm, ConnectionTest, and LanguageSelector sections, loading settings from backend on mount via useSettings in prompt-babbler-app/src/pages/SettingsPage.tsx
- [ ] T071 [US4] Add settings-not-configured detection banner to RecordPage and BabblePage that checks GET /api/settings isConfigured flag and displays a message directing users to Settings in prompt-babbler-app/src/components/layout/SettingsRequiredBanner.tsx

**Checkpoint**: Users can configure, save, and test their Azure OpenAI credentials. Settings persist across restarts. Unconfigured state is clearly communicated.

---

## Phase 7: User Story 5 — Manage Prompt Templates (Priority: P5)

**Goal**: Users can view built-in templates, create custom templates, edit templates, duplicate built-in templates, and delete custom templates. Templates define the system prompt used for LLM prompt generation.

**Independent Test**: View built-in templates (GitHub Copilot Prompt, General Assistant Prompt), create a new custom template, edit its system prompt, duplicate a built-in template, delete the custom template, verify built-in templates cannot be deleted.

### Tests for User Story 5

> **Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T072 [P] [US5] Write component tests for TemplateList (renders built-in + custom templates, badge indicators), TemplateEditor (create/edit with validation), and TemplateCard (actions, built-in protection) in prompt-babbler-app/tests/components/templates/

### Implementation for User Story 5

- [ ] T073 [US5] Wire built-in default template seeding on first load: import definitions from default-templates.ts (T024), persist with isBuiltIn=true and UUID v4 IDs (R15) if prompt-babbler:templates key is empty in prompt-babbler-app/src/services/local-storage.ts
- [ ] T074 [P] [US5] Create TemplateCard component displaying template name, description, isBuiltIn badge, and action dropdown (edit, duplicate, delete with delete disabled for built-in) in prompt-babbler-app/src/components/templates/TemplateCard.tsx
- [ ] T075 [P] [US5] Create TemplateEditor component with React Hook Form + Zod validation for template name (1-100 chars, unique), description (0-500 chars), and system prompt (1-10,000 chars) as a dialog or inline editor in prompt-babbler-app/src/components/templates/TemplateEditor.tsx
- [ ] T076 [P] [US5] Create TemplateList component rendering TemplateCard items with "Create Template" button at the top in prompt-babbler-app/src/components/templates/TemplateList.tsx
- [ ] T077 [US5] Implement TemplatesPage with TemplateList, create/edit dialog flows, delete with confirmation (custom only), and duplicate action for built-in templates in prompt-babbler-app/src/pages/TemplatesPage.tsx

**Checkpoint**: Users can manage prompt templates — view defaults, create custom, edit, duplicate built-in, and delete custom. All templates are available in the prompt generation flow.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Integration testing, accessibility, error resilience, documentation, and final validation across all user stories

- [ ] T078 [P] Write Aspire integration tests for API endpoints (POST /api/prompts/generate, POST /api/transcribe, GET/PUT /api/settings, POST /api/settings/test, /health, /alive) using WebApplicationFactory in prompt-babbler-service/tests/integration/Api.IntegrationTests/
- [ ] T079 [P] Add localStorage capacity monitoring: warn users when storage usage exceeds 80% of quota (FR-029, R14) by calculating serialized localStorage data size against a conservative 5 MB limit (use try/catch on setItem as primary guard; StorageManager.estimate() measures total origin quota and may overstate localStorage-specific capacity), display warning banner with storage stats, and refuse new babble creation at 100% with guidance message in prompt-babbler-app/src/components/layout/StorageWarning.tsx
- [ ] T080 [P] Run accessibility audit with jest-axe on all page components (HomePage, RecordPage, BabblePage, TemplatesPage, SettingsPage) and fix any violations in prompt-babbler-app/tests/
- [ ] T081 [P] Add loading skeleton states to all pages (BabbleList skeleton, settings loading, template list loading) using Shadcn/UI Skeleton component in prompt-babbler-app/src/pages/
- [ ] T082 [P] Add comprehensive error handling and user-friendly error messages for all edge cases: rate limiting, network failures, invalid credentials, long recordings (30+ min), storage full in prompt-babbler-app/src/components/layout/
- [ ] T083 Update README.md with project overview, architecture diagram, setup instructions, and link to quickstart.md at repository root
- [ ] T084 Run quickstart.md end-to-end validation: install dependencies, start via Aspire, configure settings, record a babble, generate a prompt, copy to clipboard
- [ ] T085 Final verification: all linting passes (dotnet format, ESLint, markdownlint), all test suites pass (MSTest unit + integration, Vitest), solution builds in Release mode, verify app loads and is interactive within 3 seconds (SC-006) via browser DevTools or Lighthouse performance audit

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — **BLOCKS all user stories**
- **User Stories (Phase 3–7)**: All depend on Foundational phase completion
  - User stories can proceed in priority order: P1 → P2 → P3 → P4 → P5
  - Some user stories can be parallelized (see below)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1) — Record a Babble**: Can start immediately after Foundational. No story dependencies. **MVP target.**
- **US2 (P2) — Generate a Prompt**: Can start after Foundational. Requires built-in templates (seeded via default-templates.ts, T024). Can be developed in parallel with US1 (different files).
- **US3 (P3) — Manage Babbles**: Can start after Foundational. Benefits from US1 to have real babbles to manage, but can use manually-created test babbles. Can be developed in parallel with other stories.
- **US4 (P4) — Configure LLM Settings**: Can start after Foundational. SettingsController is independent. Needed at runtime for US1 (transcription) and US2 (generation) to function, but not a build-time dependency.
- **US5 (P5) — Manage Prompt Templates**: Can start after Foundational. Template CRUD is independent. Built-in template definitions created in Foundational (T024), seeded on first load (T073).

### Runtime Dependencies (not build-time)

- Recording (US1) and prompt generation (US2) require LLM settings (US4) to be configured at runtime
- Prompt generation (US2) requires a babble to exist (US1 or US3) and a template to exist (US5 or built-in)
- These are handled by the settings-not-configured banner (T071) and UX flows

### Within Each User Story

1. Tests MUST be written and FAIL before implementation
1. Backend services before controllers
1. Controllers register DI in Program.cs
1. Frontend hooks before components
1. Components before pages
1. Story complete before moving to next priority

### Parallel Opportunities

**Phase 1 (Setup)**:

- T003, T005–T011 can all run in parallel (different project areas)

**Phase 2 (Foundational)**:

- T014 + T015 in parallel (domain models + interfaces)
- T018, T021, T022, T024, T027–T030 in parallel (independent infrastructure)

**Phase 3–7 (User Stories)**:

- Once Foundational completes, US1 and US4 can start in parallel (backend: different controllers, frontend: different pages)
- US3 and US5 can start in parallel (both frontend-heavy, different components/pages)
- Within each story: test tasks marked [P] run in parallel, component tasks marked [P] run in parallel

---

## Parallel Example: User Story 1

```text
# Step 1 — Launch all tests in parallel:
T031: Unit tests for AzureOpenAiTranscriptionService
T032: Unit tests for TranscriptionController
T033: Component tests for RecordButton + RecordingIndicator

# Step 2 — Backend implementation (sequential):
T034: AzureOpenAiTranscriptionService
T035: TranscriptionController

# Step 3 — Frontend hooks (sequential, useTranscription depends on useAudioRecording):
T036: useAudioRecording hook (MediaRecorder, webm/opus)
T037: useTranscription hook (POST /api/transcribe per chunk)
T038: useBabbles hook (CRUD for babbles, UUID v4)

# Step 4 — Frontend components in parallel:
T039: RecordButton component
T040: TranscriptPreview component
T041: RecordingIndicator component

# Step 5 — Page assembly (sequential, depends on hooks + components):
T042: RecordPage (per-chunk persistence, full recording flow)
T043: BrowserCheck component
```

## Parallel Example: User Story 4

```text
# Step 1 — Tests in parallel:
T063: Unit tests for SettingsController
T064: Component tests for SettingsForm + ConnectionTest

# Step 2 — Backend:
T065: SettingsController

# Step 3 — Frontend hook:
T066: useSettings hook

# Step 4 — Frontend components in parallel:
T067: SettingsForm component
T068: ConnectionTest component
T069: LanguageSelector component

# Step 5 — Page assembly:
T070: SettingsPage
T071: SettingsRequiredBanner
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
1. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
1. Complete Phase 3: User Story 1 — Record a Babble
1. **STOP and VALIDATE**: Record speech, see transcription, verify babble saves and reloads
1. The app can record and save babbles — core value delivered

### Incremental Delivery

1. **Setup + Foundational** → Foundation ready (projects compile, dev server runs)
1. **+ US1** → Record babbles → Test independently → **MVP!**
1. **+ US2** → Generate prompts from babbles → Test independently → Core workflow complete
1. **+ US3** → Edit/manage babbles → Test independently → Content management
1. **+ US4** → Configure LLM settings via UI → Test independently → Self-service setup
1. **+ US5** → Custom templates → Test independently → Power user features
1. **+ Polish** → Integration tests, accessibility, docs → Production-ready

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
1. Once Foundational is done:
   - Developer A: US1 (Record) + US2 (Generate) — backend-heavy, core flow
   - Developer B: US3 (Manage Babbles) + US5 (Templates) — frontend-heavy, independent
   - Developer C: US4 (Settings) + Polish — full-stack, cross-cutting
1. Stories complete and integrate independently via shared localStorage types and API contracts

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Tests are written FIRST (TDD mandated by plan.md Constitution Principle IV)
- US6 (Prompt History) is deferred to V2 per spec.md
- Backend uses Clean Architecture: Api → Domain ← Infrastructure
- Backend binds to localhost/127.0.0.1 only (R12) — no LAN access
- Audio format: audio/webm;codecs=opus, max 25 MB per chunk (R13)
- localStorage warns at 80% quota, refuses at 100% (R14)
- Entity IDs: UUID v4 via crypto.randomUUID() (R15)
- Interim transcription persisted after every chunk ~5s (R16)
- Frontend uses component-per-feature organization with layout/ for shared components
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
