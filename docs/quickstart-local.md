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

## 2. Set required Azure user secrets for local provisioning

Aspire provisions a Microsoft Foundry resource automatically on first run. Authenticate and provide subscription settings via [dotnet user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) so values stay out of source control.

```bash
# Sign in to Azure CLI (use your tenant ID)
az login --tenant <your-tenant-id>

# Verify the correct subscription
az account show --query "{name:name, id:id, tenantId:tenantId}" -o table
```

Set required AppHost user secrets:

```bash
cd prompt-babbler-service/src/Orchestration/AppHost
dotnet user-secrets set "Azure:SubscriptionId" "<your-subscription-id>"
dotnet user-secrets set "Azure:TenantId" "<your-tenant-id>"
dotnet user-secrets set "Azure:Location" "<azure-region>"
```

> [!TIP]
> Get your subscription and tenant IDs with `az account show`. Region is already configured via AppHost launch settings. Common region values include `eastus2`, `australiaeast`, and `swedencentral`.

Ensure both keys are set:

* `Azure:SubscriptionId`
* `Azure:TenantId`
* `Azure:Location`

## 3. Run the app

Make sure Docker Desktop is running, then from the repository root:

```bash
aspire run
```

In a worktree, use `aspire run --isolated` to isolate ports and local Aspire state from other instances. Isolated mode also isolates user secrets; ensure the required Azure settings are available to that instance without committing or printing secrets.

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

## Local Cosmos DB vector indexes

The vNext emulator supports `diskANN` vector indexes, not `flat` or `quantizedFlat`. In Development, the API initializes `babbles` with a DiskANN index on `/contentVector`, using 1536-dimensional Float32 vectors, cosine distance, and the `/userId` partition key. Production indexing is unchanged.

On startup, the initializer keeps compatible containers, creates missing containers, and recreates incompatible containers only after confirming they contain no documents across all partitions. If an incompatible container contains documents, startup stops with recovery instructions rather than deleting your babbles.

The vNext emulator release `EN20261008` omits embedding-policy metadata when reading a container, even after accepting it during creation. The initializer checks the returned DiskANN index and partition key, validates embedding settings when returned, and logs a warning when those settings cannot be inspected. This allows compatible populated containers to survive restarts without assuming that missing response metadata means the data must be recreated.

The emulator accepts collection updates without applying them, so changing an existing container's policy in place is not a recovery method. See [vNext emulator indexing support](https://learn.microsoft.com/azure/cosmos-db/emulator-linux#indexing-support).

### Recover an incompatible local babbles container

1. In the Aspire dashboard, identify the `cosmos` resource for the exact local AppHost/worktree and open its **Data Explorer** endpoint. Confirm this is the local emulator, not an Azure Cosmos DB account.
1. Stop application writers, including any API, MCP, seed-importer, or external client that can modify this emulator. Keep the emulator running for backup and recovery. Automatic empty-container recreation assumes there are no concurrent writers.
1. Back up every document from `prompt-babbler` > `babbles`, across all user partitions, using Data Explorer's query/export facilities or a Cosmos SDK client. Preserve all fields, including `id`, `userId`, and `contentVector`. The API export endpoint is not available if startup is blocked.
1. Verify the backup is complete, readable, and restorable before proceeding. Store it outside source control; real babbles may contain personal data.
1. Only if you accept deletion of the local container and have verified the backup, explicitly delete **only** `prompt-babbler/babbles` through that emulator's Data Explorer. Do not delete the database, other containers, Docker volumes, or another worktree's emulator.
1. Restart this AppHost using `aspire run --isolated` (or `aspire run` for a non-isolated checkout). Aspire may create an empty default-policy container first; the API will recreate that empty container with DiskANN.
1. Restore the backed-up documents into the compatible container, preserving IDs and partition keys. Verify counts across user partitions, retrieve saved babbles, and run semantic search before discarding the backup. If seed import ran during startup, account for its deterministic sample IDs when comparing counts.

> [!IMPORTANT]
> The initializer never migrates or deletes populated containers automatically. Functional vector-search results on a small dataset do not establish that an approximate-nearest-neighbor index was exercised. Azure Cosmos DB documents full scans below 1,000 vectors for DiskANN; emulator behavior is not proof of production performance, index-build behavior, or RU costs. The vNext emulator does not implement production RU accounting.

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
| `MicrosoftFoundry__chatModelName` | `gpt-5.3-chat` | Chat/LLM model |
| `MicrosoftFoundry__chatModelVersion` | `2026-03-03` | Chat model version |

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
| API reports an incompatible populated `babbles` vector policy | Follow [local container recovery](#recover-an-incompatible-local-babbles-container). Back up and verify documents before explicitly recreating only the affected local container. |
| Microphone not working | Check browser permissions. Ensure no other app is using the mic. |
| Transcription errors | Check the Aspire Dashboard for WebSocket/Speech Service errors. Verify the AIServices resource has Speech capabilities enabled and RBAC roles are assigned. |
| `aspire run` fails | Ensure .NET 10 SDK (`dotnet --version` → `10.0.x`) and Aspire CLI (`aspire --version`) are installed. |
| Frontend won't start | Ensure Node.js 22.x (`node --version`) and pnpm 10.x (`pnpm --version`) are installed. |

## Next steps

* [Deploy to Azure](quickstart-azure.md) with the Azure Developer CLI
* See the [CI/CD Setup Guide](design/cicd.md) for GitHub Actions pipeline configuration
* Browse the specs/ folder for feature specifications and design documents
* See [infra/README.md](https://github.com/PlagueHO/prompt-babbler/blob/main/infra/README.md) for Azure deployment architecture
