# Project Context

- **Owner:** Daniel Scott-Raynsford
- **Project:** Prompt Babbler — speech-to-prompt web application that captures stream-of-consciousness speech, transcribes it using Azure AI Foundry, and generates structured prompts for target systems like GitHub Copilot
- **Stack:** React 19 + TypeScript 5.9 + Vite 8 (frontend), .NET 10 + ASP.NET Core (backend), Azure AI Foundry (LLM + Speech), .NET Aspire (orchestration), Azure Bicep (infrastructure)
- **Created:** 2026-03-19

## Core Context

### Architecture

- **Clean Architecture:** Domain (records + interfaces, zero NuGet deps) → Infrastructure (Azure SDKs) → Api (controllers + DI)
- **Dual deployment:** Anonymous mode (`_anonymous` userId) and Entra ID multi-user mode
- **Real-time transcription:** WebSocket at `/api/transcribe/stream` using Azure AI Speech Service (not OpenAI), PushAudioInputStream (16kHz/16-bit/mono PCM)
- **Prompt generation:** SSE streaming via `/api/prompts/generate` — `data: {"name":"..."}` → `data: {"text":"..."}` → `data: [DONE]`
- **Cosmos DB containers:** `babbles` (pk: `/userId`), `generated-prompts` (pk: `/babbleId`), `prompt-templates` (pk: `/userId`), `users` (pk: `/userId`)

### Key Files

- `prompt-babbler-service/src/Domain/` — business models and interfaces
- `prompt-babbler-service/src/Infrastructure/` — Azure SDK implementations
- `prompt-babbler-service/src/Api/` — ASP.NET Core controllers
- `prompt-babbler-service/src/Orchestration/AppHost/` — Aspire AppHost
- `prompt-babbler-app/src/` — React frontend
- `infra/` — Azure Bicep infrastructure
- `.github/workflows/` — CI/CD pipelines (16 workflows)

### Key Decisions (from decisions.md)

- IChatClient via `AsIChatClient()` (NOT `AsChatClient()`)
- Microsoft Agent Framework for complex LLM interactions
- System.Text.Json with camelCase (never Newtonsoft)
- Controller-level validation (no FluentValidation/DataAnnotations)
- Singleton repositories, Transient prompt generation
- MSAL useRef pattern for stable token callbacks
- Speech SDK STS token exchange (AAD → issueToken → 10-min cache)
- Test pyramid: many unit, few integration (Docker for Cosmos), minimal E2E
- Bicep with AVM first, pure Bicep as fallback

## Learnings

📌 Team initialized on 2026-03-19 — cast from Firefly universe
📌 Role: Lead / Architect — scope, decisions, code review, triage
