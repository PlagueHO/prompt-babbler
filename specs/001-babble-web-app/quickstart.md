# Quickstart: Prompt Babbler — 001-babble-web-app

**Date**: 2026-02-12 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10.0.100+ | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| .NET Aspire | 13.1+ | Included in .NET SDK or `dotnet workload install aspire` |
| Node.js | 22.x LTS | <https://nodejs.org/> |
| pnpm | 10.x | `npm install -g pnpm` |
| Azure CLI | 2.x | <https://learn.microsoft.com/cli/azure/install-azure-cli> |
| Git | 2.x | <https://git-scm.com/> |
| Browser | Chrome 49+ / Edge 79+ / Safari 14.1+ / Firefox 25+ | (for MediaRecorder API support) |
| Azure Subscription | Active subscription with Contributor access | <https://portal.azure.com/> |

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

## Configure Azure for Local Provisioning

Aspire automatically provisions Azure AI Foundry resources during local development.
This requires Azure CLI authentication and your Azure subscription details.

See: [Aspire Local Azure Provisioning](https://aspire.dev/integrations/cloud/azure/local-provisioning/)

### 1. Sign in to Azure CLI

Sign in to the Azure CLI, targeting the tenant that contains your subscription:

```bash
az login --tenant <your-tenant-id>
```

Verify the correct subscription is active:

```bash
az account show --query "{name:name, id:id, tenantId:tenantId}" -o table
```

### 2. Set user secrets

Aspire reads Azure configuration from [dotnet user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets).
This keeps sensitive values out of source control.

```bash
cd prompt-babbler-service

# Initialize user secrets for the AppHost project (only needed once)
dotnet user-secrets init --project src/Orchestration/AppHost/PromptBabbler.AppHost.csproj

# Set your Azure subscription ID (required)
dotnet user-secrets set "Azure:SubscriptionId" "<your-subscription-id>" --project src/Orchestration/AppHost

# Set your Azure tenant ID (required)
dotnet user-secrets set "Azure:TenantId" "<your-tenant-id>" --project src/Orchestration/AppHost
```

Replace `<your-subscription-id>` and `<your-tenant-id>` with your actual values.
You can find these by running `az account show`.

> **Note**: The Azure region (`swedencentral`) and credential source (`AzureCli`) are
> configured in `launchSettings.json` and do not need to be set as user secrets.

### 3. Verify model quota (optional)

The AppHost deploys two AI models by default:

| Deployment | Default Model | SKU |
|------------|--------------|-----|
| `chat` | gpt-4.1 | Standard |
| `stt` | gpt-4o-transcribe | GlobalStandard |

Model names and versions can be overridden via `MicrosoftFoundry__*` environment
variables in `launchSettings.json`. Ensure your subscription has available quota
for the configured models in the target region.

## Run Locally (Aspire)

The entire application starts with a single command:

```bash
cd prompt-babbler-service
aspire run
```

Or using `dotnet run`:

```bash
cd prompt-babbler-service
dotnet run --project src/Orchestration/AppHost/PromptBabbler.AppHost.csproj
```

On first run, Aspire will:

1. **Provision Azure resources** — creates an AI Foundry resource group and deploys the chat and STT models
2. **Start the backend API** (port assigned by Aspire)
3. **Start the frontend app** via Vite dev server (proxied by Aspire)
4. **Launch the Aspire Dashboard** for telemetry, logs, and traces

> **First run takes several minutes** while Azure resources are provisioned.
> Subsequent runs reuse the existing resources and start much faster.

Open the Aspire Dashboard (URL shown in terminal output) to see all services and their endpoints.

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

The Azure AI Foundry endpoint and model deployments are automatically configured
by Aspire and injected into the API service. No manual endpoint or API key
configuration is needed.

To customize model deployments, edit the `MicrosoftFoundry__*` environment
variables in `launchSettings.json`:

| Variable | Default | Description |
|----------|---------|-------------|
| `MicrosoftFoundry__chatModelName` | `gpt-4.1` | Chat/LLM model name |
| `MicrosoftFoundry__chatModelVersion` | `2025-04-14` | Chat model version |
| `MicrosoftFoundry__sttModelName` | `gpt-4o-transcribe` | Speech-to-text model name |
| `MicrosoftFoundry__sttModelVersion` | `2025-03-20` | STT model version |

## First Babble

1. Click **New Babble** or the **Record** button
1. Grant microphone permission when prompted
1. Speak your stream of consciousness
1. Watch as your speech is transcribed in near-real-time (~5 second chunks via the configured STT model)
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
| `InvalidAuthenticationTokenTenant` | Azure CLI is signed into the wrong tenant. Run `az login --tenant <your-tenant-id>` and ensure `Azure:TenantId` is set in user secrets. |
| `InsufficientQuota` | Your subscription has no available quota for the model/SKU in the target region. Check quota in the Azure Portal or switch to a model with capacity. |
| Azure provisioning hangs | Ensure `Azure:SubscriptionId` and `Azure:TenantId` are set: `dotnet user-secrets list --project src/Orchestration/AppHost`. |
| Microphone not working | Check browser microphone permissions. Ensure no other app is using the mic. |
| Transcription not appearing | Check the Aspire Dashboard for API errors. Ensure the STT model deployed successfully. |
| `dotnet run` fails | Ensure .NET 10 SDK is installed: `dotnet --version` should show `10.0.x`. |
| pnpm install fails | Ensure Node.js 22.x: `node --version`. Install pnpm: `npm install -g pnpm`. |
| Aspire Dashboard not loading | The dashboard URL is shown in terminal output. Port may differ from default. |
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
