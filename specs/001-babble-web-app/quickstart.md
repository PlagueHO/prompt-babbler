# Quickstart: Prompt Babbler — 001-babble-web-app

**Date**: 2026-02-11 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10.0.100+ | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| Node.js | 22.x LTS | <https://nodejs.org/> |
| pnpm | 10.x | `npm install -g pnpm` |
| Git | 2.x | <https://git-scm.com/> |
| Browser | Chrome 49+ / Edge 79+ / Safari 14.1+ / Firefox 25+ | (for MediaRecorder API support) |
| Azure OpenAI | Active endpoint + API key + LLM deployment + Whisper deployment | <https://portal.azure.com/> |

## Clone & Setup

```bash
git clone https://github.com/PlagueHO/prompt-babbler.git
cd prompt-babbler
git checkout 001-babble-web-app
```

## Install Dependencies

### Root (markdownlint)

```bash
pnpm install
```

### Backend (.NET)

```bash
cd prompt-babbler-service
dotnet restore PromptBabbler.slnx
cd ..
```

### Frontend (React)

```bash
cd prompt-babbler-app
pnpm install
cd ..
```

## Run Locally (Aspire)

The entire application starts with a single command:

```bash
cd prompt-babbler-service
dotnet run --project src/Orchestration/AppHost/PromptBabbler.AppHost.csproj
```

This starts:

- **Backend API** at `http://localhost:5000` (port assigned by Aspire)
- **Frontend app** at `http://localhost:5173` (Vite dev server, proxied by Aspire)
- **Aspire Dashboard** at `https://localhost:18888` (telemetry, logs, traces)

Open the Aspire Dashboard to see all services and their endpoints.

## VS Code Tasks

Use the VS Code task runner (`Ctrl+Shift+P` → "Tasks: Run Task"):

| Task | Description |
|------|-------------|
| `aspire: run` | Start all services via Aspire AppHost |
| `app: dev` | Start frontend dev server only |
| `app: test` | Run frontend unit tests |
| `app: test (watch)` | Run frontend tests in watch mode |
| `app: lint` | Run ESLint on frontend |
| `app: build` | Build frontend for production |
| `service: build` | Build .NET solution |
| `service: test` | Run all .NET tests |
| `service: test (unit)` | Run .NET unit tests only |
| `service: format` | Format .NET code |
| `service: format (verify)` | Check .NET formatting |
| `markdown: lint` | Lint markdown files |

## Configure LLM Settings

1. Open the app in your browser (URL from Aspire Dashboard)
1. Navigate to **Settings**
1. Enter your Azure OpenAI details:
   - **Endpoint**: e.g., `https://my-resource.openai.azure.com/`
   - **API Key**: Your Azure OpenAI API key
   - **LLM Deployment Name**: e.g., `gpt-4o-mini` (for prompt generation)
   - **Whisper Deployment Name**: e.g., `whisper` (for speech-to-text)
1. Click **Save**
1. Click **Test Connection** to verify

Settings are saved to `~/.prompt-babbler/settings.json` and survive restarts.

## First Babble

1. Click **New Babble** or the **Record** button
1. Grant microphone permission when prompted
1. Speak your stream of consciousness
1. Watch as your speech is transcribed in near-real-time (~5 second chunks via Whisper)
1. Click **Stop** when finished
1. Review and edit the transcribed text if needed
1. Select a prompt template (e.g., "GitHub Copilot Prompt")
1. Click **Generate**
1. Copy the structured prompt to your clipboard

## Run Tests

### Backend

```bash
cd prompt-babbler-service

# All tests
dotnet test --solution PromptBabbler.slnx

# Unit tests only
dotnet test --solution PromptBabbler.slnx --filter "TestCategory=Unit"

# With coverage
dotnet test --solution PromptBabbler.slnx --filter "TestCategory=Unit" --coverage --coverage-output-format cobertura
```

### Frontend

```bash
cd prompt-babbler-app

# All tests
pnpm test

# Watch mode
pnpm test:watch

# With coverage
pnpm test -- --coverage
```

## Build for Production

### Backend

```bash
cd prompt-babbler-service
dotnet build PromptBabbler.slnx --configuration Release
dotnet publish src/Api/PromptBabbler.Api.csproj --configuration Release --output ./publish/api
```

### Frontend

```bash
cd prompt-babbler-app
pnpm build
# Output in dist/
```text

## Project Structure Quick Reference

```text
prompt-babbler/
├── prompt-babbler-service/         # .NET backend
│   ├── src/Api/                    # ASP.NET Core API (3 controllers: prompts, transcription, settings)
│   ├── src/Domain/                 # Business models & interfaces
│   ├── src/Infrastructure/         # Azure OpenAI SDK, file settings
│   ├── src/Orchestration/AppHost/  # Aspire orchestration
│   ├── src/Orchestration/ServiceDefaults/  # Shared telemetry/health
│   └── tests/                      # Unit + integration tests
├── prompt-babbler-app/             # React frontend
│   ├── src/components/             # UI components (recording, babbles, prompts, etc.)
│   ├── src/hooks/                  # Custom hooks (audio recording, transcription, localStorage, API)
│   ├── src/services/               # API client, localStorage service
│   └── src/pages/                  # Page-level components
├── .github/workflows/              # CI/CD pipelines
└── specs/                          # Feature specifications
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Microphone not working | Check browser microphone permissions. Ensure no other app is using the mic. |
| Transcription not appearing | Verify Azure OpenAI settings — ensure Whisper deployment name is correct and endpoint is reachable. |
| "LLM settings not configured" | Go to Settings and enter your Azure OpenAI endpoint, API key, LLM deployment name, and Whisper deployment name. |
| `dotnet run` fails | Ensure .NET 10 SDK is installed: `dotnet --version` should show `10.0.x`. |
| pnpm install fails | Ensure Node.js 22.x: `node --version`. Install pnpm: `npm install -g pnpm`. |
| Aspire Dashboard not loading | Check <https://localhost:18888>. The port may differ — check terminal output. |
| localStorage full warning | Delete old babbles you no longer need. Each babble uses ~5-50 KB. |

## Azure Developer CLI (vNext)

> **Not required for V1.** The repository is pre-structured for future Azure deployment.

The repository includes placeholder files for the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/):

- **`azure.yaml`** — AZD service manifest (declares backend and frontend as deployable services)
- **`infra/README.md`** — Documents planned Bicep IaC modules (Azure Container Apps, Azure OpenAI, etc.)

When vNext adds deployment support, you will be able to:

```bash
# Provision Azure resources and deploy
azd up

# Or separately
azd provision   # Create Azure resources via Bicep
azd deploy      # Deploy code to provisioned resources
```

See `infra/README.md` for the planned infrastructure architecture.
