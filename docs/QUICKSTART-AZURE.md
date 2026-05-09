---
title: "Quickstart: Deploy to Azure"
description: Deploy Prompt Babbler to Azure using the Azure Developer CLI (azd) with infrastructure provisioned via Bicep.
---

Deploy Prompt Babbler to Azure using the Azure Developer CLI (`azd`). This provisions all required infrastructure and deploys the application with a single command.

> [!NOTE]
> Looking for local development? See [Local Development with Aspire](QUICKSTART-LOCAL.md).

## Prerequisites

### Azure Developer CLI

Install the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd):

```bash
# Windows (winget)
winget install Microsoft.Azd

# macOS (Homebrew)
brew install azd

# Linux (script)
curl -fsSL https://aka.ms/install-azd.sh | bash
```

Verify the installation:

```bash
azd version
```

### Azure CLI

Install the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli).

### Azure subscription

You need an active Azure subscription with quota for `gpt-5.3-chat` (GlobalStandard SKU) in the target region (default: **EastUS2**).

## 1. Clone the repository

```bash
git clone https://github.com/PlagueHO/prompt-babbler.git
cd prompt-babbler
```

## 2. Authenticate

Sign in to both the Azure CLI and Azure Developer CLI:

```bash
az login
azd auth login
```

The `azd auth login` command allows the Azure Developer CLI to provision resources in your subscription. The `az login` command is required for the pre-provision hook to create Entra ID app registrations if you enable Entra ID authentication.

## 3. Create an environment

Create a new `azd` environment. This stores your deployment configuration (subscription, region, resource group):

```bash
azd env new <env-name>
```

> [!NOTE]
> The `<env-name>` is used as a prefix for all resource names. Use a short, lowercase name (e.g., `dev`, `test`, or `prod`). Use a name that will result in globally unique resource names.

When prompted, select your Azure subscription and target region. The default region is **EastUS2**.

## 4. Deploy

Provision infrastructure and deploy the application:

```bash
azd up
```

> [!IMPORTANT]
> The region selected must have available quota for the `gpt-5.3-chat` model (GlobalStandard SKU). If you receive a quota error, try a different region (e.g., `swedencentral` or `eastus2`).

This single command will:

1. Provision all Azure resources via Bicep templates
1. Build the .NET API, MCP server, and React frontend
1. Deploy the API to Azure Container Apps
1. Deploy the MCP server to Azure Container Apps
1. Deploy the frontend to Azure Static Web Apps
1. Configure service connections and environment variables

### What gets provisioned

| Resource | Name | Type | Purpose |
| --- | --- | --- | --- |
| Resource Group | `rg-<env-name>` | Resource Group | Contains all Prompt Babbler resources |
| Virtual Network | `vnet-<env-name>` | Azure Virtual Network | Network isolation with dedicated ACA (`10.0.0.0/23`) and private endpoint (`10.0.2.0/24`) subnets |
| Private DNS Zone (Cosmos DB) | `privatelink.documents.azure.com` | Azure Private DNS Zone | Resolves private Cosmos DB DNS names within the VNet |
| Private DNS Zone (AI Foundry) | `privatelink.cognitiveservices.azure.com` | Azure Private DNS Zone | Resolves private AI Foundry DNS names within the VNet |
| Private DNS Zone (OpenAI) | `privatelink.openai.azure.com` | Azure Private DNS Zone | Resolves private OpenAI endpoint DNS names within the VNet |
| Log Analytics Workspace | `log-<env-name>` | Azure Monitor | Centralizes logs and metrics from all resources |
| Application Insights | `appi-<env-name>` | Azure Application Insights | Application performance monitoring and distributed tracing |
| AI Foundry (AIServices) | `aif-<env-name>` | Azure AI Services | LLM and embedding model deployments; Speech Service for real-time transcription |
| Private Endpoint (AI Foundry) | `pe-<env-name>-foundry` | Azure Private Endpoint | Enables private network access to AI Foundry |
| Cosmos DB Account | `cdb<env-name>` | Azure Cosmos DB (Serverless, NoSQL + Vector Search) | Persists babbles, generated prompts, prompt templates, and user data |
| Private Endpoint (Cosmos DB) | `pe-<env-name>-cosmosdb` | Azure Private Endpoint | Enables private network access to Cosmos DB |
| Container Apps Environment | `cae-<env-name>` | Azure Container Apps Environment | Shared managed environment with VNet integration and Log Analytics |
| Aspire Dashboard | `aspire-dashboard` | Azure Container App | OpenTelemetry dashboard for local observability |
| Container App (API) | `ca-<env-name>-api` | Azure Container App | Backend API (scale 0–3 replicas, external ingress on port 8080) |
| Container App (MCP Server) | `ca-<env-name>-mcp-server` | Azure Container App | MCP server for AI agent integrations (scale 0–3 replicas, external ingress on port 8080) |
| Static Web App | `stapp-<env-name>` | Azure Static Web App (Free) | React frontend |

RBAC roles are automatically assigned to each Container App's system-assigned managed identity: *Cognitive Services OpenAI User*, *Cognitive Services Speech User*, and *Cosmos DB Built-in Data Contributor*.

### Access the deployed app

After `azd up` completes, the CLI displays the deployed URLs:

```text
Deploying services (azd deploy)

  (✓) Done: Deploying service api
  - Endpoint: https://ca-<env-name>-api.<region>.azurecontainerapps.io

  (✓) Done: Deploying service mcp-server
  - Endpoint: https://ca-<env-name>-mcp-server.<region>.azurecontainerapps.io

  (✓) Done: Deploying service frontend
  - Endpoint: https://<static-web-app-name>.azurestaticapps.net
```

Open the frontend endpoint in your browser to start using Prompt Babbler.

The app runs in **anonymous single-user mode** by default — no sign-in is required. All data is stored under a synthetic `_anonymous` user identity.

## Model configuration

The deployment provisions these AI model deployments by default:

| Deployment | Model | Version | SKU | Capacity |
| --- | --- | --- | --- | --- |
| `chat` | `gpt-5.3-chat` | `2026-03-03` | GlobalStandard | 50 |
| `embedding` | `text-embedding-3-small` | `1` | GlobalStandard | 120 |

Speech-to-text uses **Azure AI Speech Service** (real-time streaming), which is included in the same AIServices resource — no separate model deployment is needed.

To use a different model, edit [`infra/model-deployments.json`](https://github.com/PlagueHO/prompt-babbler/blob/main/infra/model-deployments.json) before running `azd up`.

## Environment configuration

All infrastructure parameters are read from `azd` environment variables and passed to `infra/main.bicepparam` at provisioning time. Use `azd env set <KEY> <VALUE>` to configure any of these before running `azd up` or `azd provision`.

### Required

These values are set automatically by `azd` and do not need to be configured manually.

| Variable | Description |
| --- | --- |
| `AZURE_ENV_NAME` | Environment name; used as a prefix for all resource names |
| `AZURE_LOCATION` | Primary Azure region (default: `EastUS2`) |
| `AZURE_PRINCIPAL_ID` | Object ID of the user or service principal running the deployment |
| `AZURE_PRINCIPAL_ID_TYPE` | `User` or `ServicePrincipal` (default: `User`) |

### Optional

| Variable | Default | Description |
| --- | --- | --- |
| `AZURE_STATIC_WEB_APP_LOCATION` | *(same as primary)* | Override region for the Static Web App. Must be one of: `centralus`, `eastasia`, `eastus2`, `westeurope`, `westus2` |
| `AZURE_STATIC_WEB_APP_CUSTOM_DOMAIN` | *(empty)* | Optional custom domain hostname for the Static Web App (for example, `app.contoso.com`). Leave empty to disable custom domain binding. |
| `AZURE_CONTAINER_APP_API_IMAGE` | `ghcr.io/plagueho/prompt-babbler-api:latest` | Container image deployed to the API Container App |
| `AZURE_CONTAINER_APP_MCP_SERVER_IMAGE` | `ghcr.io/plagueho/prompt-babbler-mcp-server:latest` | Container image deployed to the MCP Server Container App |
| `ENABLE_PUBLIC_NETWORK_ACCESS` | `true` | Set to `false` to restrict all resources to private network access only |

### Authentication

| Variable | Default | Description |
| --- | --- | --- |
| `ACCESS_CODE` | *(empty)* | Optional access code for single-user deployments. Leave empty for anonymous mode |
| `ENABLE_ENTRA_AUTH` | `false` | Set to `true` to enable Entra ID multi-user authentication |
| `AZURE_AD_API_CLIENT_ID` | *(set by preprovision hook)* | API app registration client ID; written automatically when `ENABLE_ENTRA_AUTH=true` |
| `AZURE_AD_SPA_CLIENT_ID` | *(set by preprovision hook)* | SPA app registration client ID; written automatically when `ENABLE_ENTRA_AUTH=true` |
| `AZURE_AD_MCP_CLIENT_ID` | *(set by preprovision hook)* | MCP server app registration client ID; written automatically when `ENABLE_ENTRA_AUTH=true` |

## Authentication

Prompt Babbler supports three authentication modes for Azure deployments. Configure the appropriate variables before running `azd up`.

### Anonymous mode (default)

No additional configuration is needed. All requests are accepted without an access check and attributed to the `_anonymous` user:

```bash
azd up
```

### Access code mode

Protect the deployment with a shared password. When configured, the frontend shows a modal requiring the code before any interaction, and the backend rejects API requests that omit it:

```bash
azd env set ACCESS_CODE "your-access-code"
azd up
```

To remove access code protection, clear the value:

```bash
azd env set ACCESS_CODE ""
azd up
```

For CI/CD deployments, store the access code as a GitHub Actions secret named `ACCESS_CODE`. The delivery pipeline passes it through to the infrastructure provisioning step automatically.

To configure a production custom domain in CI/CD, add an environment secret named `AZURE_STATIC_WEB_APP_CUSTOM_DOMAIN` with the domain hostname value (for example, `app.contoso.com`). The pipeline passes this into `azd`, and the Static Web App AVM module configures the custom domain during provisioning.

> [!IMPORTANT]
> Ensure your DNS CNAME record points your custom domain host to the deployed Static Web App default hostname (`AZURE_STATIC_WEB_APP_DEFAULT_HOSTNAME`). Domain validation can take time while DNS changes propagate.

### Entra ID mode

Enable multi-user authentication backed by your Entra ID tenant. The pre-provision hook creates the required app registrations automatically:

```bash
azd env set ENABLE_ENTRA_AUTH true
azd up
```

The [preprovision hook](https://github.com/PlagueHO/prompt-babbler/blob/main/infra/hooks/preprovision.ps1) automatically:

* Creates an **API app registration** (`prompt-babbler-api`) with an `access_as_user` OAuth2 scope
* Creates a **SPA app registration** (`prompt-babbler-spa`) with redirect URIs for `localhost:5173` and the production Static Web App hostname
* Creates an **MCP server app registration** (`prompt-babbler-mcp-server`)
* Stores the client IDs in the `azd` environment (`AZURE_AD_API_CLIENT_ID`, `AZURE_AD_SPA_CLIENT_ID`, `AZURE_AD_MCP_CLIENT_ID`)

> [!NOTE]
> This step is idempotent — re-running `azd up` reuses existing app registrations and does not create duplicates.

The deploying principal requires **`Application.ReadWrite.All`** Microsoft Graph permission. This can be granted via the **Application Administrator**, **Cloud Application Administrator**, or **Application Developer** Entra ID role. See the [CI/CD Setup Guide](CICD.md) for details.

When Entra ID is enabled:

* **Backend**: The API validates JWT Bearer tokens via `Microsoft.Identity.Web`. Requests must include a valid `access_as_user` scope token. The user's Entra ID object ID becomes the Cosmos DB partition key, isolating each user's data.
* **Frontend**: The SPA wraps the app in an MSAL `<MsalProvider>` and acquires tokens silently before API calls. Users sign in via the standard Microsoft login flow.
* **MCP server**: The MCP server validates tokens using its own app registration.
* **WebSocket**: The transcription WebSocket endpoint (`/api/transcribe/stream`) accepts tokens via the `?access_token=` query parameter.

## Update and redeploy

After making code changes, redeploy with:

```bash
azd deploy
```

To update infrastructure (Bicep template changes):

```bash
azd provision
```

To update everything (infrastructure + code):

```bash
azd up
```

## Tear down

Remove all Azure resources created by the deployment:

```bash
azd down
```

> [!WARNING]
> This permanently deletes all resources in the `rg-<env-name>` resource group, including any data stored in Cosmos DB.

To force deletion without confirmation and purge soft-deleted resources (such as Cognitive Services accounts):

```bash
azd down --force --purge
```

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `azd` command not found | Install Azure Developer CLI: `winget install Microsoft.Azd` (Windows) or see [install docs](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) |
| Quota error during provisioning | Ensure your subscription has `gpt-5.3-chat` GlobalStandard quota in the target region. Try `swedencentral` or `eastus2`. |
| `azd auth login` fails | Run `az login` first, then retry `azd auth login`. Ensure your account has Contributor access to the target subscription. |
| `InvalidAuthenticationTokenTenant` | Wrong tenant. Run `az login --tenant <tenant-id>`. |
| Deployment times out | AI Foundry model deployments can take several minutes. Re-run `azd up` — it resumes from where it left off. |
| Deployment fails with Graph permission error | The deploying principal needs `Application.ReadWrite.All`. See [CI/CD Setup Guide](CICD.md). |
| `ENABLE_ENTRA_AUTH` set but no app registrations created | Check that `AZURE_AD_API_CLIENT_ID` is not already set: `azd env get-value AZURE_AD_API_CLIENT_ID`. The hook skips if already provisioned. To force re-creation, clear it: `azd env set AZURE_AD_API_CLIENT_ID ""`. |
| Frontend can't reach API | Check that the Container App is running in the Azure Portal. Verify environment variables are set correctly with `azd env get-values`. |
| Container App not starting | Check Container Apps logs in the Azure Portal or via `az containerapp logs show`. Verify the managed identity has the required RBAC roles. |
| Static Web App returns 404 | Ensure `azd deploy` completed successfully for the `frontend` service. Check the SWA deployment logs in the Azure Portal. |
| API returns 401 Unauthorized | If Entra ID is enabled, ensure the frontend is acquiring tokens with the correct scope (`api://prompt-babbler-api/access_as_user`). |

## Next steps

* [Local development with Aspire](QUICKSTART-LOCAL.md) for running locally
* [CI/CD Setup Guide](CICD.md) for GitHub Actions pipeline configuration
* [Infrastructure documentation](https://github.com/PlagueHO/prompt-babbler/blob/main/infra/README.md) for detailed resource and networking information
