---
title: Quickstart Local Development with Aspire
description: Run Prompt Babbler locally with Aspire, including local services, optional access-code protection, and automatic sample babble seeding.
author: Prompt Babbler Team
ms.date: 2026-05-17
ms.topic: how-to
keywords:
  - aspire
  - local development
  - prompt babbler
  - seed data
estimated_reading_time: 10
---

## Quickstart: Local Development with Aspire

Get Prompt Babbler running locally in anonymous single-user mode using .NET Aspire. Aspire orchestrates all services (API, frontend, Cosmos DB emulator, Microsoft Foundry) with a single command — including dependency installation, builds, and service startup.

> **Looking to deploy to Azure?** See [Deploy to Azure with Azure Developer CLI](quickstart-azure.md).

## Prerequisites

### .NET SDK 10.0

Install the [.NET SDK 10.0.100+](https://dotnet.microsoft.com/download/dotnet/10.0).
Verify:

```bash
dotnet --version
# Expected: 10.0.x
```

### Aspire CLI

Install the [Aspire CLI](https://aspire.dev/get-started/install-cli/) using the install script:

On **Windows** (PowerShell):

```powershell
irm https://aspire.dev/install.ps1 | iex
```

On **macOS/Linux** (bash):

```bash
curl -fsSL https://aspire.dev/install.sh | sh
```

Verify:

```bash
aspire --version
```

> **Tip:** To update an existing installation, re-run the install script.

### Node.js and pnpm

Install [Node.js 22.x LTS](https://nodejs.org/) and [pnpm 10.x](https://pnpm.io/):

```bash
npm install -g pnpm
```

Verify:

```bash
node --version   # Expected: v22.x.x
pnpm --version   # Expected: 10.x.x
```

### Azure CLI

Install the [Azure CLI 2.x](https://learn.microsoft.com/cli/azure/install-azure-cli). Verify:

```bash
az --version
```

### Docker Desktop

Install [Docker Desktop](https://www.docker.com/products/docker-desktop/). It must be running before you start Aspire — the Cosmos DB preview emulator runs as a Docker container.

### Azure subscription

You need an Azure subscription with **Contributor** access. Microsoft Foundry is a cloud resource even for local development — Aspire provisions it automatically on first run.

## 1. Clone the repository

```bash
git clone https://github.com/PlagueHO/prompt-babbler.git
cd prompt-babbler
```

## 2. Configure Azure credentials

Aspire provisions a Microsoft Foundry resource automatically on first run. You need to authenticate and provide your subscription details via [dotnet user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) so they stay out of source control.

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

> **Tip:** Find your subscription and tenant IDs with `az account show`. The Azure region and other non-sensitive settings are already configured in `launchSettings.json`.

## 3. Run the app

Make sure Docker Desktop is running, then from the repository root:

```bash
aspire run
```

Aspire handles all dependency installation (NuGet restore, pnpm install), builds, and service orchestration automatically. On first run it will:

1. Restore .NET packages and build the backend
1. Install frontend npm packages and start the Vite dev server
1. Provision a Microsoft Foundry resource with a chat model deployment
1. Start the Cosmos DB preview emulator in Docker (with Data Explorer)
1. Start the .NET backend API (with WebSocket support for real-time transcription)
1. Launch the Aspire Dashboard (telemetry, logs, traces)

> **First run takes several minutes** for Microsoft Foundry provisioning. Subsequent runs reuse existing resources and start quickly.

The app runs in **anonymous single-user mode** — no sign-in is required. All API endpoints are accessible and data is stored under a synthetic `_anonymous` user identity.

On startup, Aspire also runs a seed import step that loads 20 sample babbles from `samples/babbles/babbles.json`. The seed data is idempotent, so restarting the app updates the same records instead of creating duplicates.

Open the **Aspire Dashboard** (URL shown in terminal output) to find all service endpoints.

## Seeded sample data

The local developer experience includes seeded sample babbles automatically.

* The seed importer runs after the API is ready.
* The frontend does not wait for the seeding step to finish.
* Seed records use deterministic IDs so repeated local runs do not create duplicates.
* Sample data covers multiple use cases, including coding, writing, planning, research, image prompting, incident analysis, and recipes.

If you change `samples/babbles/babbles.json`, the next local run upserts those records by ID.

## Protect with an access code (optional)

In single-user mode the app is open by default. To restrict access, set an access code. When configured, the frontend shows a modal dialog requiring the code before any interaction, and the backend rejects API requests that don't provide it.

Set the `ACCESS_CODE` environment variable before running `aspire run`:

```bash
$env:ACCESS_CODE = "<your-access-code>"
aspire run
```

Alternatively, set it in `prompt-babbler-service/src/Api/appsettings.Development.json`:

```json
{
  "AccessControl": {
    "AccessCode": "<your-access-code>"
  }
}
```

Leave the value empty (or omit the variable) to disable access code protection.

## 4. Create your first babble

1. Open the frontend URL from the Aspire Dashboard
1. Click **New Babble** and grant microphone permission
1. Speak — your speech is transcribed in near-real-time
1. Click **Stop**, then select a prompt template and click **Generate**
1. Copy the structured prompt to your clipboard

## Model configuration

The AppHost deploys one AI model by default:

| Deployment | Model | Version | SKU |
|------------|-------|---------|-----|
| chat | gpt-5.3-chat | 2026-03-03 | GlobalStandard |

Speech-to-text uses **Azure AI Speech Service** (real-time streaming), which is part of the same AIServices resource — no separate model deployment is needed.

Override the chat model via environment variables in `prompt-babbler-service/src/Orchestration/AppHost/Properties/launchSettings.json`:

| Variable | Default | Description |
|----------|---------|-------------|
| `MicrosoftFoundry__chatModelName` | `gpt-4.1` | Chat/LLM model |
| `MicrosoftFoundry__chatModelVersion` | `2025-04-14` | Chat model version |

Ensure your subscription has available quota for the configured models in the target region.

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
| Cosmos DB emulator won't start | Ensure Docker Desktop is running. Check for port conflicts. |
| Microphone not working | Check browser permissions. Ensure no other app is using the mic. |
| Transcription errors | Check the Aspire Dashboard for WebSocket/Speech Service errors. Verify the AIServices resource has Speech capabilities enabled and RBAC roles are assigned. |
| `aspire run` fails | Ensure .NET 10 SDK (`dotnet --version` → `10.0.x`) and Aspire CLI (`aspire --version`) are installed. |
| Frontend won't start | Ensure Node.js 22.x (`node --version`) and pnpm 10.x (`pnpm --version`) are installed. |

## Next steps

* [Deploy to Azure](quickstart-azure.md) with the Azure Developer CLI
* See the [CI/CD Setup Guide](cicd.md) for GitHub Actions pipeline configuration
* Browse the specs/ folder for feature specifications and design documents
* See [infra/README.md](https://github.com/PlagueHO/prompt-babbler/blob/main/infra/README.md) for Azure deployment architecture
