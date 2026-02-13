# Feature Specification: Prompt Babbler — Speech-to-Prompt Web Application

**Feature Branch**: `001-babble-web-app`  
**Created**: 2026-02-09  
**Status**: Draft  
**Input**: User description: "Create a web application that allows a user to use the microphone on their computer to just speak as a stream of consciousness for as long as they need to. The speech is then translated to text and formed into an appropriate prompt based on a specified system (for example a GitHub Copilot prompt). These streams of consciousness are called 'Babbles' and will be stored for the user. They can be edited and added to. A 'babble' can be used to generate a prompt for any system. The systems that can have generated prompts for can be templated. An LLM will be used to take the 'babble' text and generate the prompt - by combining with a System prompt etc. The application should be able to be run on a users machine locally as a web app and will use local storage to store the 'babbles'. In a future iteration of this app it will be able to be hosted in Azure, with a backend API and storage for the babbles. But in first iteration everything will be done/stored in browser and Azure OpenAI LLMs will be provided by the user with API Key/Microsoft Foundry endpoint."

## Clarifications

### Session 2026-02-09

- Q: How should the application access the Azure OpenAI/Foundry LLM given that CORS is not configurable on Azure OpenAI/Foundry endpoints (blocking browser-direct calls)? → A: Implement a .NET 10 backend API with Aspire AppHost. The backend proxies LLM calls to Azure OpenAI/Foundry, eliminating CORS issues. Scaffold and patterns to follow the Libris-Maleficarum repository structure (Clean Architecture: Api → Domain → Infrastructure, Aspire orchestration, React+TypeScript+Vite frontend, GitHub Actions CI/CD, VS Code tasks).
- Q: Where should babbles, templates, and prompt history be stored in V1, given the new backend API? → A: Browser local storage. The backend API's role in V1 is limited to proxying LLM calls and managing LLM settings. Full backend CRUD storage will be added in a future iteration.
- Q: How should the backend persist the Azure OpenAI API key between restarts? → A: Local config file on disk (e.g., `~/.prompt-babbler/settings.json`). Simple, survives restarts, no extra dependencies.
- Q: How should the application handle speech recognition language? → A: Default to browser locale, but provide a speech language dropdown in settings so users can select from Web Speech API supported languages.
- Q: Which user stories are in scope for V1? → A: P1–P5 (Record, Generate, Manage Babbles, Configure LLM, Manage Templates). P6 (Prompt History) is deferred to a future iteration.

## Assumptions

- **Single-user, local-first**: This iteration is a single-user application running on the user's machine. The .NET backend API and React frontend both run locally via Aspire AppHost. There is no authentication, user accounts, or cloud-hosted storage.
- **Backend API proxies LLM and STT calls**: Azure OpenAI endpoints do not support CORS for browser-direct calls. A .NET 10 ASP.NET Core backend API handles all LLM and Whisper STT communication, accepting requests from the frontend and forwarding them to the configured Azure OpenAI endpoint. API keys are stored and used server-side only.
- **Aspire AppHost orchestration**: Local development uses .NET Aspire to orchestrate the backend API and (optionally) other services via a single `dotnet run --project src/Orchestration/AppHost` command.
- **Libris-Maleficarum scaffold patterns**: The project structure, CI/CD pipelines, VS Code tasks, devcontainer, coding conventions, and tooling follow the patterns established in [Libris-Maleficarum](https://github.com/PlagueHO/Libris-Maleficarum). Specifically: Clean Architecture (Api/Domain/Infrastructure/Orchestration), React 19 + TypeScript + Vite frontend, GitHub Actions workflows, MSTest + FluentAssertions for backend, Vitest + Testing Library for frontend.
- **Browser speech recognition**: The Azure OpenAI Whisper model will be used for converting speech to text, called through the backend API. The browser's MediaRecorder API captures audio, which is sent in chunks to the backend for transcription. Users will need a compatible browser that supports the MediaRecorder API (Chrome, Edge, Safari, Firefox).
- **Azure OpenAI only for first iteration**: The LLM provider for prompt generation is Azure OpenAI, accessed via API key and endpoint configured in the backend. Other LLM providers may be added in future iterations.
- **Local storage persistence for babbles**: Babbles, templates, and prompt history are stored in browser local storage in this iteration. In future iterations, the backend API will provide server-side storage. Users are responsible for understanding that clearing browser data will remove their babbles.
- **No offline LLM**: Prompt generation and speech transcription require an active internet connection to reach the Azure OpenAI endpoint via the backend. Audio recording works offline (can be transcribed when connectivity is available).
- **Built-in starter templates**: The application ships with a small set of default prompt templates (e.g., GitHub Copilot prompt, general assistant prompt) that users can customize or extend.
- **No export/import in first iteration**: Export and import of babbles is not in scope for this iteration but designed to be added later.
- **Future Azure hosting**: The architecture is designed so the backend API can be deployed to Azure (e.g., Azure Container Apps) in a future iteration with minimal changes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Record a Babble (Priority: P1)

A user opens the Prompt Babbler web application in their browser and clicks a "Record" button. They speak freely — a stream of consciousness — for as long as they need. The application listens via the microphone and converts their speech to text in real time. When the user stops recording, the transcribed text is saved as a new "Babble" in local storage with a timestamp and an auto-generated title.

**Why this priority**: This is the core value proposition of the application. Without the ability to capture speech as text, no other feature is useful.

**Independent Test**: Can be fully tested by opening the app, granting microphone access, speaking, and verifying that the transcribed text appears (in ~5-second intervals) and is persisted across page reloads.

**Acceptance Scenarios**:

1. **Given** the user has opened the application and granted microphone permission, **When** they click "Record" and begin speaking, **Then** the application captures audio and displays transcribed text in near-real-time (~5-second intervals) as each audio chunk is processed by Azure OpenAI Whisper.
1. **Given** the user is currently recording a babble, **When** they click "Stop", **Then** the full transcribed text is saved as a new Babble with a timestamp and auto-generated title derived from the first few words.
1. **Given** the user has recorded a babble, **When** they refresh the page or close and reopen the browser, **Then** the babble is still present in their babble list.
1. **Given** the user denies microphone permission, **When** they click "Record", **Then** the application displays a clear message explaining that microphone access is required and how to enable it.

---

### User Story 2 — Generate a Prompt from a Babble (Priority: P2)

A user selects an existing babble and chooses a target system template (e.g., "GitHub Copilot Prompt"). The application combines the babble text with the selected template's system prompt and sends it to the configured Azure OpenAI LLM. The LLM processes the raw stream-of-consciousness text and returns a well-structured prompt appropriate for the target system. The generated prompt is displayed to the user and can be copied to the clipboard.

**Why this priority**: This is the second core value — transforming raw thoughts into usable prompts. It depends on having babbles (P1) but delivers the key differentiating value.

**Independent Test**: Can be tested by selecting a pre-existing babble, choosing a template, triggering generation, and verifying the output is a coherent, structured prompt that matches the template's intended format.

**Acceptance Scenarios**:

1. **Given** the user has a saved babble and has configured their Azure OpenAI settings, **When** they select the babble and choose a prompt template and click "Generate", **Then** the application sends the babble text combined with the template to the LLM and displays the generated prompt.
1. **Given** the prompt generation is in progress, **When** the user is waiting, **Then** the application shows a loading indicator and the generated text streams in progressively as it is received.
1. **Given** a prompt has been generated, **When** the user clicks "Copy to Clipboard", **Then** the generated prompt text is copied to the system clipboard and a confirmation is shown.
1. **Given** the Azure OpenAI settings are not configured, **When** the user attempts to generate a prompt, **Then** the application displays a message directing them to configure their LLM settings first.
1. **Given** the LLM request fails (e.g., invalid API key, network error), **When** the error occurs, **Then** the application displays an understandable error message and allows the user to retry.

---

### User Story 3 — Manage Babbles (Priority: P3)

A user views a list of all their saved babbles, ordered by most recent. They can open any babble to view the full text, edit the transcribed text to correct errors or add additional thoughts, rename the babble, or delete a babble they no longer need.

**Why this priority**: Users need to be able to manage, correct, and refine their babbles before generating prompts. Speech recognition is imperfect, so editing capability is essential for quality output.

**Independent Test**: Can be tested by creating several babbles, then viewing the list, editing one babble's text, renaming another, and deleting a third, verifying all changes persist.

**Acceptance Scenarios**:

1. **Given** the user has multiple saved babbles, **When** they open the application, **Then** they see a list of all babbles sorted by most recently created/modified, showing title and date.
1. **Given** the user opens an existing babble, **When** they edit the transcribed text and save, **Then** the updated text is persisted and the modification date is updated.
1. **Given** the user opens an existing babble, **When** they click "Record More" and speak additional content, **Then** the new speech is appended to the existing babble text.
1. **Given** the user wants to remove a babble, **When** they click "Delete" and confirm the action, **Then** the babble is permanently removed from storage.
1. **Given** the user renames a babble, **When** they change the title and save, **Then** the new title is displayed in the babble list.

---

### User Story 4 — Configure LLM Settings (Priority: P4)

A user navigates to a settings area where they can enter their Azure OpenAI API key and endpoint (or Azure AI Foundry endpoint), plus deployment names for both the LLM (prompt generation) and Whisper (speech-to-text) models. The settings are saved via the backend API to a local config file on disk (`~/.prompt-babbler/settings.json`) and used for all subsequent prompt generation and transcription requests. The user can test the connection to verify their settings are correct.

**Why this priority**: Without LLM configuration, prompt generation (P2) cannot work. However, this is a one-time setup task, so it is lower priority than the core user flows.

**Independent Test**: Can be tested by entering API credentials, saving them, verifying they persist across page reloads, and using the "Test Connection" feature to confirm connectivity.

**Acceptance Scenarios**:

1. **Given** the user opens the settings area, **When** they enter an Azure OpenAI endpoint, API key, LLM deployment name, and Whisper deployment name and click "Save", **Then** the credentials are persisted securely via the backend API to a local config file on disk.
1. **Given** the user has saved LLM settings, **When** they click "Test Connection", **Then** the application makes a lightweight request to the endpoint and displays whether the connection succeeded or failed.
1. **Given** the user has previously saved LLM settings, **When** they return to the settings area, **Then** the endpoint is displayed but the API key is masked (e.g., showing only the last 4 characters).
1. **Given** the user wants to change their LLM settings, **When** they update the endpoint, API key, or deployment names and save, **Then** the new settings replace the old ones and are used for future prompt generation and transcription.

---

### User Story 5 — Manage Prompt Templates (Priority: P5)

A user views the available prompt templates that define how babbles are transformed into prompts for different target systems. They can view built-in templates, create new custom templates, edit existing templates, and delete custom templates. Each template includes a name, description, and a system prompt that instructs the LLM on how to format the output.

**Why this priority**: Templates are essential for generating prompts for different systems, but built-in defaults provide immediate value. Custom template management extends flexibility for power users.

**Independent Test**: Can be tested by viewing the list of built-in templates, creating a new custom template with a name and system prompt, editing it, and then using it to generate a prompt from a babble.

**Acceptance Scenarios**:

1. **Given** the user opens the templates area, **When** the application loads, **Then** a list of available templates is shown, including built-in defaults (e.g., "GitHub Copilot Prompt", "General Assistant Prompt").
1. **Given** the user wants a new template, **When** they click "Create Template" and fill in the name, description, and system prompt text, and save, **Then** the new template appears in the template list and is available for prompt generation.
1. **Given** the user modifies a template's system prompt, **When** they save the changes, **Then** future prompt generations using that template use the updated system prompt.
1. **Given** the user deletes a custom template, **When** they confirm deletion, **Then** the template is removed; built-in templates cannot be deleted but can be duplicated and customized.

---

### User Story 6 — View Prompt History (Priority: P6) *(Deferred to V2)*

A user can view previously generated prompts for a babble. Each generated prompt records which template was used and when it was generated. The user can revisit, copy, or regenerate prompts.

**Why this priority**: This enables users to compare outputs from different templates or regenerate prompts after editing a babble. It's a convenience feature that builds on the core workflow.

**Deferred**: This user story is out of scope for V1. It will be implemented in a future iteration alongside backend storage.

**Independent Test**: Can be tested by generating multiple prompts from the same babble with different templates, then viewing the prompt history and verifying each entry shows the template used and timestamp.

**Acceptance Scenarios**:

1. **Given** a user has generated one or more prompts from a babble, **When** they view the babble's prompt history, **Then** they see a list of all generated prompts with template name and generation date.
1. **Given** the user views a previously generated prompt, **When** they click "Copy", **Then** the prompt text is copied to the clipboard.
1. **Given** the user wants to regenerate a prompt, **When** they click "Regenerate" on a history entry, **Then** a new prompt is generated using the same template and the new result is added to the history.

---

### Edge Cases

- What happens when the user speaks for a very long time (e.g., 30+ minutes)? The application should handle long-running recordings gracefully without data loss, periodically persisting interim text.
- What happens when the browser does not support the MediaRecorder API? The application should detect this on load and display a clear message listing supported browsers (Chrome 49+, Edge 79+, Safari 14.1+, Firefox 25+).
- What happens when local storage is full? The application should warn the user when storage is nearing capacity and prevent data loss by refusing to create new babbles rather than silently failing.
- What happens when the user loses internet during prompt generation? The application should display an error and preserve the babble; the user can retry generation when connectivity is restored.
- What happens when the Azure OpenAI endpoint returns rate-limiting errors? The application should display a user-friendly message suggesting they wait before retrying.
- What happens when the user accidentally navigates away during recording? The application should warn the user (via browser navigation prompt) that an active recording will be lost.

## Requirements *(mandatory)*

### Functional Requirements

#### Recording & Transcription

- **FR-001**: System MUST allow users to record speech via their computer's microphone, capture audio using the browser MediaRecorder API, and transcribe it to text via the Azure OpenAI Whisper model through the backend API.
- **FR-002**: System MUST display a near-real-time preview of the transcribed text as the user speaks, with transcription results appearing in ~5-second intervals as audio chunks are processed by Whisper.
- **FR-003**: System MUST save the completed transcription as a new Babble when the user stops recording.
- **FR-004**: System MUST auto-generate a babble title from the first few words of the transcription, with the option for the user to rename it.
- **FR-005**: System MUST handle long recording sessions (30+ minutes) without data loss, periodically persisting interim transcription data.
- **FR-006**: System MUST warn the user before navigating away from an active recording session.

#### Babble Management

- **FR-007**: System MUST persist all babbles in browser local storage so they survive page refreshes and browser restarts.
- **FR-008**: System MUST display a list of all saved babbles, sorted by most recently created or modified.
- **FR-009**: Users MUST be able to open, view, and edit the text of any saved babble.
- **FR-010**: Users MUST be able to append additional speech to an existing babble by recording more content.
- **FR-011**: Users MUST be able to rename a babble.
- **FR-012**: Users MUST be able to delete a babble with a confirmation step.

#### Prompt Generation

- **FR-013**: System MUST allow users to select a babble and a prompt template and generate a structured prompt via an LLM.
- **FR-014**: System MUST send the babble text combined with the template's system prompt to the backend API, which forwards the request to the configured Azure OpenAI endpoint.
- **FR-015**: System MUST display the generated prompt to the user with a copy-to-clipboard action.
- **FR-016**: System MUST show a loading state during prompt generation, with progressive streaming of the response where supported.
- **FR-017**: System MUST display clear, user-friendly error messages when prompt generation fails (network errors, invalid credentials, rate limiting).
- **FR-018**: System MUST display the most recently generated prompt for a babble. *(Full prompt history with multiple entries per babble is deferred to V2.)*

#### Template Management

- **FR-019**: System MUST include built-in default prompt templates (at minimum: GitHub Copilot Prompt, General Assistant Prompt).
- **FR-020**: Users MUST be able to create custom prompt templates with a name, description, and system prompt text.
- **FR-021**: Users MUST be able to edit and delete custom prompt templates.
- **FR-022**: System MUST NOT allow deletion of built-in templates; users may duplicate them for customization.
- **FR-023**: System MUST persist all custom templates in browser local storage.

#### LLM Configuration

- **FR-024**: System MUST provide a settings area for users to enter their Azure OpenAI endpoint URL, API key (or Azure AI Foundry endpoint), LLM model/deployment name, and Whisper model/deployment name.
- **FR-025**: System MUST persist LLM settings (including Whisper deployment name) in a local config file on disk (e.g., `~/.prompt-babbler/settings.json`) on the backend, so settings survive backend restarts. The frontend displays the API key masked (e.g., showing only the last 4 characters).
- **FR-026**: System MUST provide a "Test Connection" feature that sends a lightweight request through the backend API to verify the LLM endpoint is reachable and credentials are valid.
- **FR-027**: System MUST prevent prompt generation and display a helpful message if LLM settings have not been configured.

#### Browser Compatibility & Resilience

- **FR-028**: System MUST detect whether the browser supports the MediaRecorder API on load and display a clear message if not supported.
- **FR-029**: System MUST warn the user when local storage is nearing capacity.
- **FR-030**: System MUST run locally on the user's machine using the .NET Aspire AppHost to orchestrate the backend API and frontend. A single command (`dotnet run --project src/Orchestration/AppHost`) starts all services.
- **FR-031**: System MUST default the transcription language to auto-detect and provide a setting for users to select a preferred language from Whisper's supported languages (ISO-639-1 codes).

### Key Entities

- **Babble**: A captured stream-of-consciousness transcription. Key attributes: unique identifier, title, transcribed text content, creation date, last modified date, list of associated generated prompts.
- **Prompt Template**: A reusable template that defines how a babble should be transformed into a prompt for a specific target system. Key attributes: unique identifier, name, description, system prompt text, whether it is built-in or custom, creation date.
- **Generated Prompt**: The output of combining a babble with a template via the LLM. Key attributes: unique identifier, associated babble identifier, associated template identifier, generated prompt text, generation date.
- **LLM Settings**: The user's Azure OpenAI configuration. Key attributes: endpoint URL, API key, LLM model/deployment name, Whisper model/deployment name. Stored server-side in a local config file (`~/.prompt-babbler/settings.json`); the frontend interacts with LLM settings via a backend settings API.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can go from opening the application to having their first babble recorded and saved in under 2 minutes (excluding microphone permission grant on first use).
- **SC-002**: Users can generate a well-structured prompt from a babble in under 30 seconds (excluding LLM response time, which depends on the provider).
- **SC-003**: 90% of users can successfully record a babble, generate a prompt, and copy it to clipboard on their first attempt without external help.
- **SC-004**: Speech transcription uses the Azure OpenAI Whisper model for consistent quality across all supported browsers, with near-real-time display (~5-second chunk latency).
- **SC-005**: All user data (babbles, templates, settings, prompt history) persists reliably across browser sessions with zero data loss under normal usage.
- **SC-006**: The application loads and is interactive within 3 seconds on a standard broadband connection.
- **SC-007**: Users can manage (create, edit, delete) babbles and templates with no more than 3 clicks per operation.
- **SC-008**: The application provides clear, actionable feedback for all error conditions (unsupported browser, missing LLM config, network failures, storage limits) within 2 seconds of the error occurring.
