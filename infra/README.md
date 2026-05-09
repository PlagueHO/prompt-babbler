# Infrastructure

Bicep Infrastructure-as-Code (IaC) for deploying Prompt Babbler to Azure using [Azure Verified Modules](https://azure.github.io/Azure-Verified-Modules/) and the [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/).

## Architecture

| Resource | AVM / Custom | Purpose |
|---|---|---|
| **Azure Container Apps** | `avm/res/app/container-app` | .NET backend API |
| **Azure Static Web Apps** | `avm/res/web/static-site` | React frontend |
| **Microsoft Foundry (AI Services)** | Custom `cognitive-services/accounts` module | GPT-4o (prompt generation) + gpt-4o-transcribe (STT) via managed identity |
| **Container Apps Environment** | `avm/res/app/managed-environment` | Managed environment for Container Apps |
| **Azure Log Analytics** | `avm/res/operational-insights/workspace` | Logging and monitoring |
| **Application Insights** | `avm/res/insights/component` | Application performance monitoring |

> **Note:** The `cognitive-services/accounts` module is a custom module because the AVM version does not yet support the latest Microsoft Foundry resource pattern (tracked: [azure/bicep-registry-modules#5390](https://github.com/Azure/bicep-registry-modules/issues/5390)).

## Directory Structure

```text
infra/
├── abbreviations.json              # Azure resource naming abbreviations
├── bicepconfig.json                # Bicep compiler configuration
├── main.bicep                      # Main deployment (subscription scope)
├── main.bicepparam                 # Parameters file (azd environment variables)
├── model-deployments.json          # AI model deployment definitions
├── cognitive-services/             # Custom Foundry/Cognitive Services module
│   └── accounts/
│       ├── main.bicep              # Account-level module
│       ├── capabilityHost/         # Account capability hosts
│       ├── connection/             # Account connections
│       ├── modules/                # Helper modules (Key Vault export)
│       └── project/                # Foundry Projects
│           ├── main.bicep
│           ├── application/        # Applications & agent deployments
│           └── capabilityHost/     # Project capability hosts
└── core/
    └── security/
        └── role_foundry.bicep      # Foundry role assignments
```

## Usage

```bash
# Provision Azure resources
azd provision

# Deploy application
azd deploy

# Or do both
azd up
```

## Parameters

All parameters are supplied via `main.bicepparam` using `readEnvironmentVariable()` for azd integration:

| Parameter | Environment Variable | Default | Description |
|---|---|---|---|
| `environmentName` | `AZURE_ENV_NAME` | `azdtemp` | Environment name used in resource naming (1-18 characters) |
| `location` | `AZURE_LOCATION` | `EastUS2` | Azure region for all resources |
| `principalId` | `AZURE_PRINCIPAL_ID` | — | Deploying user/service principal object ID |
| `principalIdType` | `AZURE_PRINCIPAL_ID_TYPE` | `User` | `User` or `ServicePrincipal` |
| `enablePublicNetworkAccess` | `ENABLE_PUBLIC_NETWORK_ACCESS` | `true` | Enable public network access |
