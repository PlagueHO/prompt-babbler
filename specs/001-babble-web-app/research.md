# Research: Prompt Babbler — 001-babble-web-app

**Date**: 2026-02-11 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## R1: CORS on Azure OpenAI / Azure AI Foundry

**Decision**: Implement a .NET 10 ASP.NET Core backend API that proxies all LLM calls.

**Rationale**: Azure OpenAI and Azure AI Foundry endpoints do not expose CORS configuration headers. Browser-direct `fetch()` calls from a React app to `https://*.openai.azure.com` are blocked by the browser's same-origin policy. Microsoft documentation confirms there is no way to add `Access-Control-Allow-Origin` headers to Azure OpenAI responses. A backend proxy is the only reliable solution.

**Alternatives considered**:

- **Browser-direct with CORS proxy service** — Rejected. Adds an unnecessary third-party dependency, introduces latency, and violates Constitution Principle VI (Industry-Standard Dependencies).
- **Azure API Management CORS policy** — Rejected for V1. APIM is a cloud-hosted service that adds cost and complexity; V1 is local-first. Could be revisited for future Azure-hosted iteration.
- **Azure Functions proxy** — Rejected for V1. Same cloud-hosting concerns. Architecture supports this for future deployment.

## R2: Project Scaffold Patterns (Libris-Maleficarum)

**Decision**: Follow Libris-Maleficarum monorepo structure exactly, adapted for Prompt Babbler naming.

**Rationale**: User explicitly mandated following the Libris-Maleficarum repository patterns. Research confirmed the structure is well-organized, uses industry-standard tooling, and supports the exact tech stack needed.

**Key patterns adopted**:

| Pattern | Libris-Maleficarum | Prompt Babbler |
|---------|-------------------|----------------|
| Backend folder | `libris-maleficarum-service/` | `prompt-babbler-service/` |
| Frontend folder | `libris-maleficarum-app/` | `prompt-babbler-app/` |
| Solution file | `LibrisMaleficarum.slnx` | `PromptBabbler.slnx` |
| AppHost SDK | `Aspire.AppHost.Sdk/13.1.0` | `Aspire.AppHost.Sdk/13.1.0` |
| AppHost entry | `AppHost.cs` (not Program.cs) | `AppHost.cs` |
| ServiceDefaults | `Extensions.cs` with OpenTelemetry, health checks | Same pattern |
| Clean Architecture | Api → Domain ← Infrastructure | Same |
| Frontend Vite reference | `AddViteApp("frontend", "../../../../libris-maleficarum-app", "dev").WithPnpm()` | `AddViteApp("frontend", "../../../../prompt-babbler-app", "dev").WithPnpm()` |
| Test structure | `tests/unit/`, `tests/integration/` | Same |
| CI/CD | Reusable workflow composition in `continuous-integration.yml` | Same pattern |
| Package manager | pnpm | pnpm |
| .NET SDK | `global.json` with `"version": "10.0.100"` | Same |
| MSTest SDK | `"MSTest.Sdk": "4.1.0"` in `global.json` | Same |
| Versioning | GitVersion 6.3.x, ContinuousDelivery mode | Same |
| Markdown linting | markdownlint-cli2 via root `package.json` | Same |

**Alternatives considered**:

- **Custom project structure** — Rejected. User explicitly requested Libris-Maleficarum patterns.
- **Nx monorepo** — Rejected. Unnecessary complexity for a two-project monorepo. Violates Constitution Principle I.

## R3: Frontend UI Framework — Shadcn/UI + TailwindCSS v4

**Decision**: Use Shadcn/UI with TailwindCSS v4, initialized via `npx shadcn@latest init` for Vite.

**Rationale**: User explicitly specified Shadcn/UI and TailwindCSS. Shadcn/UI provides unstyled, accessible Radix-based primitives that are copied into the project (not installed as a dependency), giving full control. TailwindCSS v4 uses CSS-first configuration (no `tailwind.config.js` needed).

**Key components to install**:

| Component | Usage |
|-----------|-------|
| `button` | Record, Stop, Generate, Save, Delete, Copy |
| `input` | Babble title, template name, endpoint URL |
| `textarea` | Babble text editing, template system prompt, generated prompt display |
| `card` | Babble cards, template cards |
| `dialog` | Confirm delete, edit dialogs |
| `select` | Template picker, language picker, deployment name |
| `toast` (via Sonner) | Success/error notifications |
| `badge` | Built-in/custom template indicator |
| `separator` | Section dividers |
| `skeleton` | Loading states |
| `scroll-area` | Long babble text, prompt output |
| `dropdown-menu` | Babble/template action menus |

**Form handling**: React Hook Form + `@hookform/resolvers/zod` + Zod for type-safe validation.

**Alternatives considered**:

- **Material UI** — Rejected. User specified Shadcn/UI.
- **Radix UI directly** — Rejected. Shadcn/UI provides the same Radix primitives with pre-styled defaults and TailwindCSS integration.
- **Redux Toolkit / RTK Query** — Not needed for V1. Babble/template state lives in localStorage; only two API calls (generate prompt, manage settings). Simple `fetch` + React hooks suffice. Can be added if state management becomes complex.

## R4: Speech-to-Text — Azure OpenAI Whisper via Backend

**Decision**: Use the Azure OpenAI Whisper model for speech-to-text, called via the backend API as a proxy. Audio is captured in the browser using the `MediaRecorder` API and sent in chunks to the backend, which forwards each chunk to the Azure OpenAI Whisper `/audio/transcriptions` endpoint.

**Rationale**: The Azure OpenAI Whisper model provides high-quality, multilingual speech-to-text transcription. Because Azure OpenAI endpoints do not support CORS, the backend must proxy all STT calls (same pattern as prompt generation). Using the Azure OpenAI Whisper deployment means the user only needs **one** Azure resource — the same Azure OpenAI resource used for LLM prompt generation — with an additional Whisper model deployment. This avoids requiring a separate Azure Speech Service resource.

**Architecture — Chunked HTTP Transcription (No WebSocket Required)**:

1. **Browser**: `navigator.mediaDevices.getUserMedia({ audio: true })` → `MediaRecorder` captures audio
1. **Chunking**: Every ~5 seconds, recording is stopped and restarted. Each stop produces a self-contained audio blob (WebM/OGG format) via the `onstop` event
1. **Upload**: Each audio chunk is sent via `POST /api/transcribe` (multipart/form-data) to the backend
1. **Backend proxy**: Backend receives the audio file, forwards it to `POST {endpoint}/openai/deployments/{whisperDeploymentName}/audio/transcriptions?api-version=2024-02-01` with the API key
1. **Response**: Whisper returns the transcribed text for that chunk. Backend returns it to the frontend
1. **Display**: Frontend appends the transcribed text to the running transcript, giving near-real-time display with ~5-second latency per chunk

**Why WebSockets are NOT required for V1**:

- The Whisper model API is file-based (not streaming) — it accepts a complete audio file and returns the full transcription. There is no benefit to a persistent connection.
- Each chunk is an independent HTTP POST/response cycle, fitting the standard request-response pattern.
- The 5-second chunk interval provides acceptable near-real-time feedback.
- SignalR or WebSockets can be added later if true word-by-word real-time is needed (via Azure Speech Service SDK), but this would require a separate Azure resource.

**Key implementation details**:

- **Audio format**: `MediaRecorder` produces WebM (Chrome/Edge) or OGG (Firefox). Whisper API accepts both formats (supports: flac, mp3, mp4, mpeg, mpga, m4a, ogg, wav, webm).
- **Chunk strategy**: Stop/restart `MediaRecorder` every ~5 seconds. Each stop produces a complete, self-contained audio file with proper container headers. The gap between stop and restart is negligible (<50ms).
- **Chunk size**: At typical audio bitrates (128 kbps), a 5-second chunk is ~80 KB — well under Whisper's 25 MB file limit.
- **Language**: The Whisper API accepts an optional `language` parameter (ISO-639-1 code). Default is auto-detect. User can configure a preferred language in settings.
- **Azure OpenAI integration**: Uses the same `endpoint` and `apiKey` as prompt generation. Requires an additional `whisperDeploymentName` in `LlmSettings`.
- **Interim display**: While a chunk is being transcribed, the frontend shows a recording animation/indicator. Transcribed text appears when each chunk completes.
- **Long recordings**: No practical limit — chunks are processed independently. A 30-minute recording produces ~360 chunks (5s each), each transcribed separately.
- **Error handling**: If a chunk fails to transcribe, the error is displayed but recording continues. The user can re-record or edit the gap.

**Browser support matrix** (MediaRecorder API — much broader than Web Speech API):

| Browser | Status |
|---------|--------|
| Chrome 49+ | Full support (WebM output) |
| Edge 79+ | Full support (WebM output) |
| Safari 14.1+ | Full support (MP4/CAF output) |
| Firefox 25+ | Full support (OGG output) |
| Chrome Android | Full support |
| Safari iOS 14.5+ | Full support |

**Advantages over Web Speech API**:

- **Firefox supported** — Web Speech API is unsupported in Firefox; MediaRecorder + Whisper works in all major browsers
- **Consistent quality** — Whisper model provides consistent transcription quality across all browsers, unlike browser-dependent speech recognition engines
- **Multilingual** — Whisper supports 99 languages with auto-detection; no reliance on browser-specific language support
- **Offline recording** — Audio can be captured and stored offline, transcribed when connectivity is available
- **Single Azure resource** — Same Azure OpenAI resource handles both LLM and STT

**Alternatives considered**:

- **Web Speech API (browser-native)** — Rejected. Not supported in Firefox. Transcription quality varies by browser. No user control over the STT model. Makes the app browser-dependent for a core feature.
- **Azure Speech Service (real-time via Speech SDK)** — Rejected for V1. Requires a **separate** Azure resource (Azure AI Services / Speech), different authentication (Speech key + region), and the `Microsoft.CognitiveServices.Speech` NuGet package. The real-time Speech SDK uses `PushAudioInputStream` with continuous recognition (`Recognizing`/`Recognized` events) which provides true word-by-word streaming but requires WebSocket/SignalR for browser-to-backend audio streaming. Significantly more complex. Could be offered as an alternative STT provider in V2.
- **Whisper.js (local/in-browser)** — Rejected. Large model download (~150MB), GPU-dependent performance, experimental. Violates Constitution Principle I (YAGNI).
- **Azure OpenAI GPT-4o Realtime API** — Rejected. Uses WebSockets for bidirectional audio/text streaming. Designed for conversational AI, not pure transcription. More expensive and complex than Whisper for the STT use case.

## R5: Azure OpenAI SDK — .NET Integration

**Decision**: Use `Azure.AI.OpenAI` NuGet package (v2.1+ stable) with `AzureOpenAIClient` and API key authentication.

**Rationale**: Official Microsoft SDK for Azure OpenAI. Thread-safe client, supports streaming via `CompleteChatStreaming()`, compatible with .NET 10.

**Key implementation patterns**:

```csharp
// Client creation (singleton via DI)
var client = new AzureOpenAIClient(
    new Uri(settings.Endpoint),
    new ApiKeyCredential(settings.ApiKey));
var chatClient = client.GetChatClient(settings.DeploymentName);

// Streaming generation
var updates = chatClient.CompleteChatStreaming([
    new SystemChatMessage(templateSystemPrompt),
    new UserChatMessage(babbleText)
]);
```

**Streaming to frontend**: Use Server-Sent Events (SSE) or chunked transfer encoding from the API controller. The frontend reads the stream via `EventSource` or `fetch` with `ReadableStream`.

**Alternatives considered**:

- **Semantic Kernel** — Rejected for V1. Adds abstraction over Azure.AI.OpenAI without providing value for the simple prompt generation use case. Violates Constitution Principle I.
- **Direct HTTP calls** — Rejected. The official SDK handles retries, token management, and error handling. Reinventing this violates Constitution Principle VI.

## R6: Aspire AppHost — Frontend Integration

**Decision**: Use `AddViteApp()` from `Aspire.Hosting.JavaScript` (v13.1.0) with `.WithPnpm()`.

**Rationale**: Aspire 13.0 renamed the package from `Aspire.Hosting.NodeJs` to `Aspire.Hosting.JavaScript` and introduced `AddViteApp()` as a first-class method. It auto-configures dev/build scripts and handles service discovery.

**AppHost configuration** (V1 — no Azure resources):

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.PromptBabbler_Api>("api");

var frontend = builder.AddViteApp("frontend", "../../../../prompt-babbler-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
```

**Key difference from Libris-Maleficarum**: No Cosmos DB or Azure Storage resources — V1 uses localStorage and a local config file. This makes the AppHost significantly simpler.

**Alternatives considered**:

- **Run frontend separately** — Rejected. Aspire orchestration provides service discovery, unified logging, and single-command startup. Required by spec (FR-030).
- **Docker Compose** — Rejected. Aspire is the prescribed orchestration tool, provides better .NET integration, and is the future Azure deployment path.

## R7: V1 Scope — Backend Minimalism

**Decision**: V1 backend has exactly three controller groups: PromptController (generate prompts), TranscriptionController (Whisper STT proxy), and SettingsController (CRUD LLM/STT settings).

**Rationale**: Babbles, templates, and prompt state all live in browser localStorage. The backend's only responsibilities are:

1. Proxy LLM calls to Azure OpenAI (CORS workaround)
1. Proxy Whisper STT calls to Azure OpenAI (CORS workaround)
1. Persist LLM/STT settings to `~/.prompt-babbler/settings.json`
1. Provide health checks and OpenTelemetry (via Aspire ServiceDefaults)

This minimizes backend complexity while maintaining Clean Architecture for future expansion (adding Cosmos DB, blob storage, auth, etc.).

**Domain layer (minimal)**: Contains `LlmSettings` model (with `WhisperDeploymentName`) and interfaces `ISettingsService`, `IPromptGenerationService`, `ITranscriptionService`.

**Infrastructure layer (minimal)**: Contains `FileSettingsService` (reads/writes config file), `AzureOpenAiPromptGenerationService` (wraps Azure.AI.OpenAI SDK for LLM), and `AzureOpenAiTranscriptionService` (wraps Azure.AI.OpenAI SDK for Whisper STT).

**Alternatives considered**:

- **No backend at all** — Rejected. CORS blocks browser-direct Azure OpenAI calls (both LLM and Whisper).
- **Full CRUD backend for babbles** — Rejected for V1. YAGNI — localStorage is sufficient for single-user local-first. Deferred per spec clarification Q2.

## R8: CI/CD Pipeline — Adapted from Libris-Maleficarum

**Decision**: Reusable workflow composition pattern matching Libris-Maleficarum, minus Bicep/infrastructure workflows (deferred).

**Rationale**: User explicitly requested CI/CD matching Libris-Maleficarum's `.github/workflows/` structure and style. V1 does not include Azure infrastructure, so Bicep and deployment workflows are omitted.

**Workflows for V1**:

| Workflow | Purpose | Based on |
|----------|---------|----------|
| `continuous-integration.yml` | Main orchestrator — calls reusable workflows | Same pattern |
| `build-and-publish-backend-service.yml` | .NET restore/build/format/test/publish | Same pattern, no Docker (V1) |
| `build-and-publish-frontend-app.yml` | pnpm install/lint/test/build | Same pattern |
| `lint-markdown.yml` | markdownlint-cli2 | Same |
| `set-build-variables.yml` | GitVersion semantic versioning | Same |

**Deferred workflows**: `lint-and-publish-bicep.yml`, `validate-infrastructure.yml`, `continuous-deployment.yml` — added when Azure hosting is implemented.

**Key differences from Libris-Maleficarum**:

- No Docker image build/push (V1 is local-only)
- Path filters use `prompt-babbler-service/` and `prompt-babbler-app/`
- Working directories updated to match Prompt Babbler folder names

## R9: State Management — No Redux in V1

**Decision**: Use React hooks + Context for state management. No Redux/RTK Query.

**Rationale**: V1 state is simple:

- Babbles, templates, last prompt → localStorage (accessed via custom `useLocalStorage` hook)
- LLM settings → backend API (two endpoints)
- Recording state → component-local state in `useAudioRecording` hook
- Transcription → `useTranscription` hook with fetch to `/api/transcribe`
- Prompt generation → `usePromptGeneration` hook with fetch + streaming

There are no complex state interactions, no optimistic updates, no cache invalidation, and no shared state between distant components that would justify Redux overhead.

**Alternatives considered**:

- **Redux Toolkit / RTK Query** — Rejected for V1. Only two API endpoints. Premature abstraction violates Constitution Principle I. Can be added later if complexity grows.
- **Zustand** — Rejected. Similar reasoning — the built-in React primitives (useState, useContext, custom hooks) are sufficient.
- **TanStack Query** — Considered but deferred. Only two API endpoints don't justify the dependency. Worth revisiting when backend CRUD is added.

## R10: Azure Developer CLI (azd) — vNext Deployability

**Decision**: Scaffold an `azure.yaml` at repository root and an `infra/` directory with a `README.md` placeholder. Actual Bicep templates (`main.bicep`, `main.parameters.json`, modules for Container Apps, Container Registry, Log Analytics, etc.) are deferred to vNext.

**Rationale**: The user explicitly requested that the solution be structured for Azure Developer CLI (`azd`) deployment beyond MVP. AZD expects:

- `azure.yaml` at repository root — declares services (backend, frontend) and their build/deploy targets
- `infra/` directory — contains Bicep IaC (Infrastructure-as-Code) modules invoked by `azd provision` and `azd deploy`

In V1 the application is local-only with no Azure hosting, so writing full Bicep templates now would violate Constitution Principle I (Simplicity & YAGNI). However, establishing the directory structure and manifest file costs nothing and signals deployment intent.

The natural vNext target architecture is:

- **Azure Container Apps** for the .NET backend (Aspire projects map directly to ACA via `azd`)
- **Azure Static Web Apps** or a second Container App for the React frontend build artifacts
- **Azure OpenAI** (already used in V1 — accessed directly via managed identity instead of user-supplied keys)

**Alternatives considered**:

- **Write full Bicep now** — Rejected. No Azure deployment in V1 scope. Premature IaC is wasted effort and violates YAGNI.
- **Terraform instead of Bicep** — Rejected. Constitution Principle VII mandates Azure-First, and Bicep is the native Azure IaC language with first-class `azd` integration.
- **No placeholder at all** — Rejected. The cost is a single YAML file and a README. Establishing structure now avoids re-scaffolding later and documents intent for contributors.

## R11: Speech-to-Text Strategy — Web Speech API (V1) → Azure OpenAI STT (vNext)

**Decision**: V1 uses the browser's Web Speech API for real-time transcription. vNext will migrate to a server-side Azure OpenAI STT model.

**Rationale**: The Web Speech API is free, requires zero backend infrastructure for speech recognition, and provides real-time interim results with continuous mode — all of which are ideal for a local-first V1. However, it has limitations:

- Browser support is inconsistent (no Firefox)
- Accuracy varies by browser engine
- No control over the underlying model
- Requires an active internet connection (Chrome sends audio to Google servers)

Azure AI Foundry offers several STT models from multiple publishers. The exact model will be chosen in vNext, but candidates include:

- **gpt-4o-transcribe** (OpenAI) — High-accuracy STT, standard-paygo pricing, 57 languages
- **gpt-4o-mini-transcribe** (OpenAI) — Cost-effective STT, same language support
- **gpt-4o-transcribe-diarize** (OpenAI) — STT with speaker identification
- **whisper** (OpenAI) — Original Whisper model, standard-paygo
- Other non-OpenAI STT models available in the Azure AI Foundry model catalog (to be evaluated in vNext)

The vNext migration path would:

1. Record audio in the browser using the MediaRecorder API (already cross-browser compatible)
1. Stream or POST audio chunks to the .NET backend
1. Backend calls an STT model deployed in Azure AI Foundry (model TBD — could be OpenAI or another provider's model deployed directly, not proxied)
1. Return transcription results to the frontend via SSE or WebSocket

This approach decouples transcription from browser engine quality and enables consistent behavior across all browsers including Firefox. Using Azure AI Foundry as the model hosting platform means the backend calls the Foundry-deployed model directly via the Azure AI Inference SDK or the model's native SDK — not a proxy pattern.

**Alternatives considered**:

- **Azure AI Speech Service (Cognitive Services)** — Viable alternative. Offers real-time STT with WebSocket streaming. However, Azure AI Foundry provides a unified model catalog with multiple STT options and aligns with Principle VII (Azure-First, single platform consolidation).
- **Use STT in V1** — Rejected. Adds backend complexity (audio streaming endpoint, chunking, MediaRecorder integration) that isn't needed when Web Speech API works adequately for the MVP use case. Violates Principle I (YAGNI).
- **Stay on Web Speech API forever** — Rejected. The browser inconsistencies and lack of Firefox support make it unsuitable for a production-quality product beyond MVP.
- **Proxy approach (backend forwards audio to external endpoint)** — Rejected per user preference. vNext will deploy the STT model directly within Azure AI Foundry and call it natively from the .NET backend.
