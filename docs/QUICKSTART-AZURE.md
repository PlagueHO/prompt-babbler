# Quickstart: Deploy to Azure

Deploy Prompt Babbler to Azure using the [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/). By default the app deploys in **anonymous single-user mode**. An optional section covers enabling **Entra ID authentication** for multi-user support.

> **Looking for local development?** See [Local Development with Aspire](QUICKSTART-LOCAL.md).

## Prerequisites

- [Azure CLI 2.x](https://learn.microsoft.com/cli/azure/install-azure-cli)
- [Azure Developer CLI (`azd`)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- An Azure subscription with **Contributor** access

## 1. Clone the repository

```bash
git clone https://github.com/PlagueHO/prompt-babbler.git
cd prompt-babbler
```

## 2. Authenticate

```bash
# Sign in to Azure Developer CLI
azd auth login

# Sign in to Azure CLI (used by azd for Bicep deployments)
az login
```

## 3. Create an environment

```bash
azd env new <environment-name>
```

Set the Azure region. Ensure the region has available quota for the `gpt-5.3-chat` model with GlobalStandard SKU:

```bash
azd env set AZURE_LOCATION <region>
```

> **Example regions:** `swedencentral`, `eastus2`, `westus3`. Check [Azure OpenAI model availability](https://learn.microsoft.com/azure/ai-services/openai/concepts/models) for supported regions.

## 4. Deploy

```bash
azd up
```

This single command provisions all Azure infrastructure and deploys both the API and frontend. On first run this takes several minutes.

### What gets provisioned

| Resource | Purpose |
|----------|---------|
| **Resource Group** | Container for all resources (`rg-<env-name>`) |
| **Azure Container App** | .NET backend API (port 8080, system-assigned managed identity) |
| **Azure Static Web App** | React frontend (free tier) |
| **Azure Cosmos DB** | Serverless NoSQL — 4 containers: `babbles`, `generated-prompts`, `prompt-templates`, `users` |
| **Azure AI Foundry (AIServices)** | LLM (`gpt-5.3-chat`) + Speech Service for real-time transcription |
| **Container Apps Environment** | Managed environment with VNet integration |
| **Virtual Network** | Private networking (10.0.0.0/16) with subnets for Container Apps and private endpoints |
| **Private DNS Zones** | Private link DNS for Cosmos DB and Cognitive Services |
| **Log Analytics Workspace** | Centralized logging and diagnostics |
| **Application Insights** | Application performance monitoring and distributed tracing |

**RBAC roles** are automatically assigned to the Container App's managed identity: *Cognitive Services OpenAI User*, *Cognitive Services Speech User*, and *Cosmos DB Built-in Data Contributor*.

### Access the deployed app

After `azd up` completes, the Static Web App URL is shown in the output. You can also retrieve it with:

```bash
azd env get-values | grep FRONTEND
```

The app runs in **anonymous single-user mode** — no sign-in is required. All data is stored under a synthetic `_anonymous` user identity.

## 5. Enable Entra ID authentication (optional)

To enable multi-user support with Microsoft Entra ID (Azure AD) authentication, configure the deployment to create app registrations.

### Prerequisites for Entra ID

The deploying principal (your Azure account) requires **`Application.ReadWrite.All`** Microsoft Graph permission. This can be granted via Entra ID roles:

- **Application Administrator**, **Cloud Application Administrator**, or **Application Developer**

See the [CI/CD Setup Guide](CICD.md) for details on granting these permissions.

### Enable and deploy

1. **Set the Entra ID flag** in your `azd` environment:

   ```bash
   azd env set ENABLE_ENTRA_AUTH true
   ```

1. **Run `azd up`** (or re-run if you already deployed in single-user mode):

   ```bash
   azd up
   ```

   The [preprovision hook](../infra/hooks/preprovision.ps1) automatically:

   - Creates an **API app registration** (`prompt-babbler-api`) with an `access_as_user` OAuth2 scope
   - Creates a **SPA app registration** (`prompt-babbler-spa`) with redirect URIs for `localhost:5173` and the production Static Web App hostname
   - Creates service principals for both applications
   - Stores the client IDs in the `azd` environment (`AZURE_AD_API_CLIENT_ID`, `AZURE_AD_SPA_CLIENT_ID`)

   The main Bicep template reads these client IDs and injects the `AzureAd__ClientId`, `AzureAd__TenantId`, and `AzureAd__Instance` environment variables into the Container App. The frontend SPA receives `MSAL_CLIENT_ID` and `MSAL_TENANT_ID` at build time.

   > This step is **idempotent** — re-running `azd up` reuses existing app registrations and does not create duplicates.

1. **Verify** the client IDs were stored:

   ```bash
   azd env get-value AZURE_AD_API_CLIENT_ID
   azd env get-value AZURE_AD_SPA_CLIENT_ID
   ```

### How authentication works

When Entra ID client IDs are configured:

- **Backend**: The API enables JWT Bearer token validation via `Microsoft.Identity.Web`. Requests must include a valid `access_as_user` scope token. The user's Entra ID object ID becomes the Cosmos DB partition key, isolating each user's data.
- **Frontend**: The SPA wraps the app in an MSAL `<MsalProvider>` and acquires tokens silently before API calls. Users sign in via the standard Microsoft login flow.
- **WebSocket**: The transcription WebSocket endpoint (`/api/transcribe/stream`) accepts tokens via the `?access_token=` query parameter.

When client IDs are **not** configured (default), both the backend and frontend operate in anonymous single-user mode.

## Model configuration

The AI model deployment is defined in [`infra/model-deployments.json`](../infra/model-deployments.json):

| Deployment | Model | SKU | Capacity |
|------------|-------|-----|----------|
| gpt-5.3-chat | gpt-5.3-chat | GlobalStandard | 50 |

Speech-to-text uses **Azure AI Speech Service** (real-time streaming), which is part of the same AIServices resource — no separate model deployment is needed.

To use a different model, edit `model-deployments.json` before running `azd up`.

## Update and redeploy

| Command | Use case |
|---------|----------|
| `azd deploy` | Code changes only (rebuilds and redeploys API container + frontend SPA) |
| `azd provision` | Infrastructure changes only (updates Bicep resources) |
| `azd up` | Both infrastructure and code changes |

## Tear down

To remove all provisioned Azure resources:

```bash
azd down --purge
```

> The `--purge` flag permanently deletes soft-deleted resources like Cognitive Services accounts.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `InsufficientQuota` | No quota for the model/SKU in the target region. Check quota in Azure Portal or change `AZURE_LOCATION`. |
| `InvalidAuthenticationTokenTenant` | Wrong tenant. Run `az login --tenant <tenant-id>`. |
| Deployment fails with Graph permission error | The deploying principal needs `Application.ReadWrite.All`. See [CI/CD Setup Guide](CICD.md). |
| `ENABLE_ENTRA_AUTH` set but no app registrations created | Check that `AZURE_AD_API_CLIENT_ID` is not already set: `azd env get-value AZURE_AD_API_CLIENT_ID`. The hook skips if already provisioned. To force re-creation, clear it: `azd env set AZURE_AD_API_CLIENT_ID ""`. |
| Static Web App returns 404 | Ensure `azd deploy` completed successfully for the `frontend` service. Check the SWA deployment logs. |
| Container App not starting | Check Container Apps logs in the Azure Portal or via `az containerapp logs show`. Verify the managed identity has the required RBAC roles. |
| API returns 401 Unauthorized | If Entra ID is enabled, ensure the frontend is acquiring tokens with the correct scope (`api://prompt-babbler-api/access_as_user`). |

## Next steps

- [Local development with Aspire](QUICKSTART-LOCAL.md) for running locally
- See the [CI/CD Setup Guide](CICD.md) for GitHub Actions pipeline configuration
- See [infra/README.md](../infra/README.md) for detailed infrastructure documentation
