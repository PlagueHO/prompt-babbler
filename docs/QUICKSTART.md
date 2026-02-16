# Quickstart: Prompt Babbler

Get the Prompt Babbler app running locally with Azure AI Foundry in under 10 minutes.

## Prerequisites

- [.NET SDK 10.0.100+](https://dotnet.microsoft.com/download/dotnet/10.0) (includes .NET Aspire)
- [Node.js 22.x LTS](https://nodejs.org/) and [pnpm 10.x](https://pnpm.io/) (`npm install -g pnpm`)
- [Azure CLI 2.x](https://learn.microsoft.com/cli/azure/install-azure-cli)
- An Azure subscription with **Contributor** access

## 1. Clone the repository

```bash
git clone https://github.com/PlagueHO/prompt-babbler.git
cd prompt-babbler
```

## 2. Install dependencies

```bash
# Root (markdownlint tooling)
pnpm install

# Frontend
cd prompt-babbler-app && pnpm install && cd ..

# Backend
cd prompt-babbler-service && dotnet restore PromptBabbler.slnx && cd ..
```

## 3. Configure Azure

Aspire provisions Azure AI Foundry resources automatically during local development.
You need to authenticate and provide your subscription details via
[dotnet user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
so they stay out of source control.

```bash
# Sign in to Azure CLI (use your tenant ID)
az login --tenant <your-tenant-id>

# Verify the correct subscription
az account show --query "{name:name, id:id, tenantId:tenantId}" -o table
```

```bash
cd prompt-babbler-service

# Store Azure settings in user secrets (one-time setup)
dotnet user-secrets set "Azure:SubscriptionId" "<your-subscription-id>" \
  --project src/Orchestration/AppHost
dotnet user-secrets set "Azure:TenantId" "<your-tenant-id>" \
  --project src/Orchestration/AppHost
```

> **Tip:** Find your subscription and tenant IDs with `az account show`.
> The Azure region and other non-sensitive settings are already configured in
> `launchSettings.json`.

## 4. Run the app

```bash
cd prompt-babbler-service
aspire run
```

On first run, Aspire will:

1. Provision an Azure AI Foundry resource with chat and STT model deployments
2. Start the .NET backend API
3. Start the React frontend via Vite
4. Launch the Aspire Dashboard (telemetry, logs, traces)

> **First run takes several minutes** for Azure provisioning.
> Subsequent runs reuse existing resources and start quickly.

Open the **Aspire Dashboard** (URL shown in terminal output) to find all service endpoints.

## 5. Create your first babble

1. Open the frontend URL from the Aspire Dashboard
2. Click **New Babble** and grant microphone permission
3. Speak — your speech is transcribed in near-real-time
4. Click **Stop**, then select a prompt template and click **Generate**
5. Copy the structured prompt to your clipboard

## Model configuration

The AppHost deploys two AI models by default:

| Deployment | Model | SKU |
|------------|-------|-----|
| chat | gpt-4.1 | Standard |
| stt | gpt-4o-transcribe | GlobalStandard |

Override models via environment variables in
`prompt-babbler-service/src/Orchestration/AppHost/Properties/launchSettings.json`:

| Variable | Default | Description |
|----------|---------|-------------|
| `MicrosoftFoundry__chatModelName` | `gpt-4.1` | Chat/LLM model |
| `MicrosoftFoundry__chatModelVersion` | `2025-04-14` | Chat model version |
| `MicrosoftFoundry__sttModelName` | `gpt-4o-transcribe` | Speech-to-text model |
| `MicrosoftFoundry__sttModelVersion` | `2025-03-20` | STT model version |

Ensure your subscription has available quota for the configured models in the
target region.

## Run tests

```bash
# Backend — all tests
cd prompt-babbler-service
dotnet test --solution PromptBabbler.slnx

# Backend — unit tests only
dotnet test --solution PromptBabbler.slnx --filter "TestCategory=Unit"

# Frontend
cd prompt-babbler-app
pnpm test
```

## VS Code tasks

Open `Ctrl+Shift+P` → **Tasks: Run Task** for common workflows:

| Task | Description |
|------|-------------|
| `aspire: run` | Start all services via Aspire |
| `app: dev` | Frontend dev server only |
| `app: test` | Frontend unit tests |
| `app: lint` | ESLint on frontend |
| `service: build` | Build .NET solution |
| `service: test (unit)` | .NET unit tests |
| `service: format (verify)` | Check .NET formatting |
| `markdown: lint` | Lint markdown files |

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `InvalidAuthenticationTokenTenant` | Wrong tenant. Run `az login --tenant <tenant-id>` and verify `Azure:TenantId` in user secrets. |
| `InsufficientQuota` | No quota for the model/SKU in the target region. Check quota in Azure Portal or use a different model. |
| Azure provisioning hangs | Verify secrets are set: `dotnet user-secrets list --project src/Orchestration/AppHost`. |
| Microphone not working | Check browser permissions. Ensure no other app is using the mic. |
| Transcription errors | Check the Aspire Dashboard for API errors. Verify the STT model deployed correctly. |
| `dotnet run` fails | Ensure .NET 10 SDK: `dotnet --version` → `10.0.x`. |
| `pnpm install` fails | Ensure Node.js 22.x: `node --version`. |

## Next steps

- Browse the [specs/](../specs/) folder for feature specifications and design documents
- See [infra/README.md](../infra/README.md) for planned Azure deployment architecture
- See [azure.yaml](../azure.yaml) for the Azure Developer CLI service manifest
