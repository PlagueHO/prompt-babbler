# Skill: Azure Bicep Infrastructure

## Confidence: medium

## Overview

prompt-babbler infrastructure is defined in Azure Bicep using Azure Verified Modules (AVM) as the primary module source, falling back to pure Bicep when AVM modules are not available or not up-to-date. Deployment is managed via Azure Developer CLI (`azd`) with subscription-scoped templates.

## Module Strategy

**Prefer AVM modules** from `br/public:avm/res/...` — they are validated, tested, and maintained by Microsoft.

**Fall back to pure Bicep** when:

- No AVM module exists for the resource type
- The AVM module doesn't support required features (e.g., AI Foundry V2 capabilities)

Current AVM modules in use:

| Resource | AVM Module | Version |
|----------|-----------|---------|
| Resource Group | `avm/res/resources/resource-group` | 0.4.3 |
| Log Analytics | `avm/res/operational-insights/workspace` | 0.15.0 |
| App Insights | `avm/res/insights/component` | 0.7.1 |
| Cosmos DB | `avm/res/document-db/database-account` | 0.19.0 |
| Container Apps Env | `avm/res/app/managed-environment` | 0.10.0 |
| Container App | `avm/res/app/container-app` | 0.12.0 |
| Static Web App | `avm/res/web/static-site` | 0.7.0 |

Current pure Bicep fallbacks:

| Resource | File | Reason |
|----------|------|--------|
| AI Foundry (Cognitive Services) | `cognitive-services/accounts/main.bicep` | AVM doesn't support AI Foundry V2 yet |

## Naming Conventions

Resource names use abbreviation prefix + environment name:

```bicep
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Examples:
'rg-${environmentName}'              // Resource Group
'log-${environmentName}'             // Log Analytics
'appi-${environmentName}'            // App Insights
'aif-${environmentName}'             // AI Foundry
'cdb${resourceToken}'                // Cosmos DB (no hyphen, token for uniqueness)
'cae-${environmentName}'             // Container Apps Environment
'ca-${environmentName}-api'          // Container App
'stapp-${environmentName}'           // Static Web App
```

Full abbreviation list is in `infra/abbreviations.json`.

## Deployment Architecture

### Scope

Subscription-level deployment (`targetScope = 'subscription'`). Resource group is created by the template.

### Parameters

```bicep
param environmentName string          // 1-24 chars, used in all resource names
param location string                 // Azure region
param principalId string              // User/SP for role assignments
param enablePublicNetworkAccess bool  // Network access control (default: true)
param apiClientId string              // Entra ID API app (optional)
param spaClientId string              // Entra ID SPA app (optional)
```

### Key Outputs

- Container App FQDN and resource ID
- Static Web App hostname
- AI Foundry project endpoint
- Cosmos DB endpoint
- Entra ID client IDs

## Cosmos DB Schema

Database: `prompt-babbler` (serverless)

| Container | Partition Key | Purpose |
|-----------|---------------|---------|
| `prompt-templates` | `/userId` | Prompt templates (built-in + user) |
| `babbles` | `/userId` | Transcribed speech recordings |
| `generated-prompts` | `/babbleId` | Generated prompts per babble |
| `users` | `/userId` | User profiles and settings |

## RBAC Assignments

### Deploying Principal

| Role | Purpose |
|------|---------|
| Contributor | Resource management |
| Cognitive Services OpenAI Contributor | Model deployment management |
| Cognitive Services Speech User | Speech recognition |

### Container App Managed Identity

| Role | Scope | Purpose |
|------|-------|---------|
| Cognitive Services OpenAI User | AI Foundry | Model inference calls |
| Cognitive Services Speech User | AI Foundry | Speech transcription |
| Cosmos DB Built-in Data Contributor | Cosmos DB | Data read/write |

Role definitions are in `core/security/role_foundry.bicep` (50+ Azure role definitions).

## Entra ID App Registrations

Defined in `entra-id/app-registrations.bicep` using Microsoft Graph Bicep extension:

```bicep
extension microsoftGraphV1

resource apiApp 'Microsoft.Graph/applications@v1.0' = { ... }
resource spaApp 'Microsoft.Graph/applications@v1.0' = { ... }
resource apiSp 'Microsoft.Graph/servicePrincipals@v1.0' = { ... }
```

**Important:** Service principals must be explicitly created — they are NOT auto-created by the Graph API (unlike the Azure Portal UI).

## Bicep Configuration

`bicepconfig.json` enables:

- Microsoft Graph Bicep extension: `microsoftgraph/v1.0:0.2.0-preview`
- Linter rule: `no-unused-params` at warning level

## Azure Developer CLI (azd)

`azure.yaml` defines two services:

1. **api** — Container App host, .NET, Dockerfile at `src/Api/Dockerfile`
1. **frontend** — Static Web App host, JS, dist folder

### Preprovision Hook

`infra/hooks/preprovision.{sh,ps1}` creates Entra ID app registrations via Azure CLI when `ENABLE_ENTRA_AUTH=true`. Idempotent — skips if client IDs already exist.

## Model Deployments

Configured in `infra/model-deployments.json`:

```json
[{
  "model": { "format": "OpenAI", "name": "gpt-5.3-chat", "version": "2026-03-03" },
  "name": "gpt-5.3-chat",
  "sku": { "name": "GlobalStandard", "capacity": 50 }
}]
```
