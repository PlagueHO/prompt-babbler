# CI/CD Setup Guide

This guide covers the complete CI/CD pipeline setup for Prompt Babbler,
including managed identity creation, permissions configuration, and GitHub
repository settings.

## Prerequisites

- An Azure subscription with **Owner** or **Global Administrator** access
  (for one-time identity setup)
- [Azure CLI 2.x](https://learn.microsoft.com/cli/azure/install-azure-cli)
- A GitHub repository fork or clone of Prompt Babbler
- (Optional) [VS Code Insiders](https://code.visualstudio.com/insiders/)
  with GitHub Copilot for assisted setup via the managed identity skill

## Pipeline overview

The CI/CD pipeline is split into two GitHub Actions workflows:

### Continuous Integration (`continuous-integration.yml`)

Runs on every pull request and push to `main`:

| Job | Description |
|-----|-------------|
| **Lint Markdown** | Checks all markdown files with markdownlint |
| **Lint and Publish Bicep** | Lints and publishes infrastructure templates |
| **Build and Test Backend** | .NET format check, build, unit tests with coverage |
| **Build and Publish Frontend** | pnpm lint, Vitest tests with coverage, Vite build |

### Continuous Delivery (`continuous-delivery.yml`)

Runs on pushes to `main` when `infra/`, `prompt-babbler-service/`, or
`prompt-babbler-app/` paths change:

| Job | Description |
|-----|-------------|
| **Set Build Variables** | Determines SemVer via GitVersion |
| **Lint and Publish Bicep** | Lints and publishes Bicep templates |
| **Build and Test Backend** | Release build + unit tests |
| **Build and Publish Frontend** | Build + test + publish frontend artifact |
| **Build and Push API Container** | Docker build + push to GHCR (tag pushes only) |
| **Validate Infrastructure** | Bicep what-if against Azure |
| **E2E Test** | Provisions ephemeral infrastructure, runs tests, tears down |

Container images are pushed to `ghcr.io/plagueho/prompt-babbler-api` on
version tags (`v*`).

## 1. Create the managed identity

The pipeline authenticates to Azure using a **User-Assigned Managed Identity**
with federated credentials for GitHub Actions OIDC. No client secrets are
needed.

### Option A: Using the managed identity skill (recommended)

You can use the `azure-github-managed-identity` Copilot skill to create the
managed identity interactively in VS Code.

#### Install the agent plugin

1. Open VS Code (or VS Code Insiders)
1. Open **Settings** (`Ctrl+,`) and search for `github.copilot.chat.agentPlugins`
1. Add the following entry to the agent plugins marketplace list:

   ```text
   https://github.com/PlagueHO/plagueho.os
   ```

   See [VS Code Agent Plugins documentation](https://code.visualstudio.com/docs/copilot/customization/agent-plugins)
   for full details on adding agent plugin marketplaces.

1. Restart VS Code to load the plugin

#### Run the skill

1. Open GitHub Copilot Chat (`Ctrl+Shift+I`)
1. Ask Copilot to create the managed identity:

   ```text
   Create an Azure managed identity for GitHub Actions using the
   azure-github-managed-identity skill for the PlagueHO/prompt-babbler
   repository with a "test" environment federated credential.
   ```

1. The skill will guide you through creating:
   - A User-Assigned Managed Identity in your Azure subscription
   - A federated credential configured for the GitHub repository and environment
   - The required RBAC role assignments

### Option B: Manual creation via Azure CLI

```bash
# Variables — adjust to match your environment
SUBSCRIPTION_ID="<your-subscription-id>"
RESOURCE_GROUP="<resource-group-for-identity>"
LOCATION="eastus2"
MI_NAME="mi-github-actions-test-environment"
GITHUB_ORG="PlagueHO"
GITHUB_REPO="prompt-babbler"
ENVIRONMENT="test"

# Create the managed identity
az identity create \
  --name "$MI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION"

# Get the managed identity's principal ID and client ID
MI_PRINCIPAL_ID=$(az identity show \
  --name "$MI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query principalId -o tsv)

MI_CLIENT_ID=$(az identity show \
  --name "$MI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query clientId -o tsv)

echo "Principal (object) ID: $MI_PRINCIPAL_ID"
echo "Client (app) ID: $MI_CLIENT_ID"

# Create federated credential for the GitHub environment
az identity federated-credential create \
  --name "github-actions-${ENVIRONMENT}" \
  --identity-name "$MI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --issuer "https://token.actions.githubusercontent.com" \
  --subject "repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:${ENVIRONMENT}" \
  --audiences "api://AzureADTokenExchange"
```

> **Tip:** To also support deployments from `main` branch pushes (not tied to
> an environment), create an additional federated credential:
>
> ```bash
> az identity federated-credential create \
>   --name "github-actions-main-branch" \
>   --identity-name "$MI_NAME" \
>   --resource-group "$RESOURCE_GROUP" \
>   --issuer "https://token.actions.githubusercontent.com" \
>   --subject "repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/main" \
>   --audiences "api://AzureADTokenExchange"
> ```

## 2. Assign Azure RBAC roles

The managed identity needs these roles at the **subscription** scope:

| Role | Purpose |
|------|---------|
| **Contributor** | Create and manage Azure resources (resource groups, Cosmos DB, Container Apps, etc.) |
| **User Access Administrator** | Assign RBAC roles to managed identities created during deployment |

```bash
az role assignment create \
  --assignee-object-id "$MI_PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID"

az role assignment create \
  --assignee-object-id "$MI_PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "User Access Administrator" \
  --scope "/subscriptions/$SUBSCRIPTION_ID"
```

Verify:

```bash
az role assignment list \
  --assignee "$MI_PRINCIPAL_ID" \
  --scope "/subscriptions/$SUBSCRIPTION_ID" \
  --output table
```

## 3. Grant Microsoft Graph permissions

The Bicep templates deploy Entra ID app registrations using the Microsoft Graph
Bicep extension (`Microsoft.Graph/applications@v1.0`). This requires the
`Application.ReadWrite.All` Microsoft Graph **application permission** on the
deploying identity.

For managed identities, `az ad app permission add` does **not** work — you must
grant the Graph app role directly via the Microsoft Graph API.

> **Requirement:** This is a **one-time setup** requiring **Global
> Administrator** or **Privileged Role Administrator** privileges in the tenant.

```bash
# Get the service principal object ID for the managed identity
SP_OBJECT_ID=$(az ad sp show --id "$MI_CLIENT_ID" --query id -o tsv)

# Get the Microsoft Graph service principal object ID
GRAPH_SP_ID=$(az ad sp show \
  --id "00000003-0000-0000-c000-000000000000" --query id -o tsv)

# Get the Application.ReadWrite.All app role ID
APP_ROLE_ID=$(az ad sp show \
  --id "00000003-0000-0000-c000-000000000000" \
  --query "appRoles[?value=='Application.ReadWrite.All'].id" -o tsv)

# Grant the permission
az rest --method POST \
  --url "https://graph.microsoft.com/v1.0/servicePrincipals/${GRAPH_SP_ID}/appRoleAssignments" \
  --headers "Content-Type=application/json" \
  --body "{
    \"principalId\": \"${SP_OBJECT_ID}\",
    \"resourceId\": \"${GRAPH_SP_ID}\",
    \"appRoleId\": \"${APP_ROLE_ID}\"
  }"
```

Verify the grant:

```bash
az rest --method GET \
  --url "https://graph.microsoft.com/v1.0/servicePrincipals/${SP_OBJECT_ID}/appRoleAssignments" \
  --query "value[].{Resource:resourceDisplayName, Permission:appRoleId}" \
  -o table
```

Expected output should show `Microsoft Graph` with the
`Application.ReadWrite.All` role ID (`1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9`).

## 4. Configure GitHub repository

### Environments

Create a GitHub environment named **test** in your repository settings:

1. Go to **Settings** → **Environments** → **New environment**
1. Name: `test`
1. No additional protection rules are required for the test environment

### Secrets

Add the following **repository secrets** (Settings → Secrets and variables →
Actions → Repository secrets):

| Secret | Value | Description |
|--------|-------|-------------|
| `AZURE_TENANT_ID` | Your Microsoft Entra tenant ID | Used for federated auth |
| `AZURE_SUBSCRIPTION_ID` | Your Azure subscription ID | Target subscription |
| `AZURE_CLIENT_ID` | The managed identity's **client (app) ID** | Identity for OIDC login |

> **Important:** Use the **client ID** (app ID), not the principal (object) ID.

### Variables (optional)

| Variable | Default | Description |
|----------|---------|-------------|
| `AZURE_LOCATION` | `eastus2` | Azure region for deployments |

## 5. Verify the pipeline

1. Push a change to `main` that touches `infra/`, `prompt-babbler-service/`,
   or `prompt-babbler-app/`
1. The **Continuous Delivery** workflow should trigger
1. Check the **Validate Infrastructure** job passes (Bicep what-if)
1. Check the **E2E Test** job provisions, tests, and tears down infrastructure

### Common deployment errors

| Error | Cause | Fix |
|-------|-------|-----|
| `Burst Capacity is not supported for serverless accounts` | AVM module defaults `enableBurstCapacity` to `true` | Already fixed in `infra/main.bicep` — `enableBurstCapacity: false` |
| `Insufficient privileges to complete the operation. Graph client request id...` | Missing `Application.ReadWrite.All` Graph permission | Complete [step 3](#3-grant-microsoft-graph-permissions) |
| `does not have permission to create role assignments` | Missing `User Access Administrator` on subscription | Complete [step 2](#2-assign-azure-rbac-roles) |
| `AADSTS700024: Client assertion is not within its valid time range` | Clock skew or expired OIDC token | Re-run the workflow — transient issue |
| `No federated identity credentials found` | Federated credential subject mismatch | Verify the federated credential subject matches the GitHub environment name |

## Permissions summary

The following table summarizes all permissions required for the CI/CD managed
identity:

| Permission | Scope | Type | Purpose |
|------------|-------|------|---------|
| Contributor | Subscription | Azure RBAC | Create/manage resources |
| User Access Administrator | Subscription | Azure RBAC | Assign roles to deployed identities |
| Application.ReadWrite.All | Tenant | Microsoft Graph App Role | Create Entra ID app registrations |
