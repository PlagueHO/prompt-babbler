---
title: Infrastructure
description: Bicep IaC for deploying Prompt Babbler to Azure using Azure Verified Modules and the Azure Developer CLI.
---

## Overview

Bicep Infrastructure-as-Code (IaC) for deploying Prompt Babbler to Azure using [Azure Verified Modules](https://azure.github.io/Azure-Verified-Modules/) and the [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/).

## Architecture

| Resource | AVM / Custom | Purpose |
|---|---|---|
| **Azure Container Apps** (API) | `avm/res/app/container-app` | .NET backend API |
| **Azure Container Apps** (MCP Server) | `avm/res/app/container-app` | .NET MCP Server |
| **Azure Static Web Apps** | `avm/res/web/static-site` | React frontend |
| **Microsoft Foundry (AI Services)** | Custom `cognitive-services/accounts` module | `gpt-5.3-chat` (prompt generation), `text-embedding-3-small` (vector embeddings), Speech API (STT) via managed identity |
| **Azure Cosmos DB** | `avm/res/document-db/database-account` | Serverless NoSQL data store with vector search |
| **Container Apps Environment** | `avm/res/app/managed-environment` | Managed environment for Container Apps |
| **Azure Virtual Network** | `avm/res/network/virtual-network` | Private networking for Container Apps and private endpoints |
| **Azure Log Analytics** | `avm/res/operational-insights/workspace` | Logging and monitoring |
| **Application Insights** | `avm/res/insights/component` | Application performance monitoring |

> **Note:** The `cognitive-services/accounts` module is a custom module because the AVM version does not yet support the latest Microsoft Foundry resource pattern (tracked: [azure/bicep-registry-modules#5390](https://github.com/Azure/bicep-registry-modules/issues/5390)).

## Directory Structure

```text
infra/
├── abbreviations.json              # Azure resource naming abbreviations
├── aspire-dashboard.bicep          # Aspire Dashboard container app
├── bicepconfig.json                # Bicep compiler configuration
├── cosmos-babbles-vector-container.bicep  # Babbles container with vector search config
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
├── core/
│   └── security/
│       ├── role_cosmosdb.bicep     # Cosmos DB role assignments
│       └── role_foundry.bicep      # Foundry role assignments
├── entra-id/
│   └── app-registrations.bicep     # Entra ID app registration helpers
└── hooks/
    ├── preprovision.ps1            # Pre-provision hook (PowerShell)
    └── preprovision.sh             # Pre-provision hook (Bash)
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
| `resourceGroupName` | — | `rg-{environmentName}` | Azure resource group name |
| `principalId` | `AZURE_PRINCIPAL_ID` | — | Deploying user/service principal object ID |
| `principalIdType` | `AZURE_PRINCIPAL_ID_TYPE` | `User` | `User` or `ServicePrincipal` |
| `enablePublicNetworkAccess` | `ENABLE_PUBLIC_NETWORK_ACCESS` | `true` | Enable public network access |
| `apiClientId` | `AZURE_AD_API_CLIENT_ID` | `''` | Entra ID API app registration client ID (leave empty for anonymous mode) |
| `spaClientId` | `AZURE_AD_SPA_CLIENT_ID` | `''` | Entra ID SPA app registration client ID (leave empty to disable auth) |
| `mcpClientId` | `AZURE_AD_MCP_CLIENT_ID` | `''` | Entra ID MCP Server app registration client ID |
| `staticWebAppLocation` | `AZURE_STATIC_WEB_APP_LOCATION` | `''` | Static Web App region override (must be one of the supported regions) |
| `staticWebAppCustomDomain` | `AZURE_STATIC_WEB_APP_CUSTOM_DOMAIN` | `''` | Optional custom domain hostname for the Static Web App |
| `containerImageApi` | `AZURE_CONTAINER_APP_API_IMAGE` | `ghcr.io/plagueho/prompt-babbler-api:latest` | Container image for the API Container App |
| `containerImageMcpServer` | `AZURE_CONTAINER_APP_MCP_SERVER_IMAGE` | `ghcr.io/plagueho/prompt-babbler-mcp-server:latest` | Container image for the MCP Server Container App |
| `accessCode` | `ACCESS_CODE` | `''` | Optional access code for single-user mode protection |
