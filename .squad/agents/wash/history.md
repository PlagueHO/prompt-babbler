# Project Context

- **Owner:** Daniel Scott-Raynsford
- **Project:** Prompt Babbler — speech-to-prompt web application
- **Stack:** Azure Bicep (AVM modules), GitHub Actions (16 workflows), .NET Aspire 13.1.1, azd, GitVersion 6.3.x, GHCR container images
- **Created:** 2026-03-19

## Core Context

### Infrastructure (Bicep)

- **infra/main.bicep** — 7 AVM modules + pure Bicep for AI Foundry resources
- **AVM pattern:** Use Azure Verified Modules first (`br/public:avm/res/{provider}/{type}:{version}`), fall back to pure Bicep when unavailable
- Microsoft Graph Bicep extension v1.0:0.2.0-preview for Entra ID app registrations
- Service principals must be explicitly created (NOT auto-created by Graph API)
- `bicepconfig.json` has `microsoftGraphV1` experimental extension enabled
- Cosmos DB: serverless, 4 containers with defined partition keys
- RBAC roles: `Cognitive Services OpenAI User`, `Cognitive Services Speech User`, `Cosmos DB Built-in Data Contributor`
- Container images: `ghcr.io/plagueho/prompt-babbler-api`
- **VNET integration:** Virtual network with private endpoints for Cosmos DB and Foundry, Container Apps Environment VNET-integrated but public-facing (external ingress)

### CI/CD (GitHub Actions)

- 16 workflows total: continuous-integration, continuous-delivery, IaC validation, linting (eslint, dotnet-format, bicep, markdown), E2E, squad automation
- Federated OIDC credentials — no client secrets
- GitVersion 6.3.x for SemVer — version flows from git tags through CI
- Artifact sharing between CI/CD jobs
- Dependabot: npm, NuGet, GitHub Actions, Docker

### Aspire Hosting

- `PromptBabbler.AppHost.csproj` — orchestrates frontend (npm), backend (ASP.NET), Azure AI resources
- Auto-provisions Azure resource group + AI Foundry on first run
- Aspire.Hosting.Azure.AIFoundry 13.1.2-preview for AI model deployment
- `launchSettings.json` has `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL` for browser OTel

### Deployment

- `azure.yaml` — azd project definition
- Preprovision hooks for Entra ID setup
- Dual deployment: anonymous (no auth) + Entra ID multi-user

### Key Files

- `infra/main.bicep` — primary infrastructure definition
- `infra/bicepconfig.json` — Bicep compiler config + Graph extension
- `.github/workflows/` — all CI/CD workflows
- `azure.yaml` — azd project config
- `prompt-babbler-service/src/Orchestration/AppHost/` — Aspire hosting
- `GitVersion.yml` — versioning config

## Learnings

📌 Team initialized on 2026-03-19 — cast from Firefly universe
📌 Role: DevOps/Infra — Bicep, GitHub Actions, Aspire, Azure deployment, RBAC
📌 VNET with private endpoints (2026-03-19): Added Azure VNET (`br/public:avm/res/network/virtual-network:0.1.5`) with two subnets: one for Container Apps Environment (10.0.0.0/23, no delegation required for Consumption environments), one for private endpoints (10.0.2.0/24, network policies disabled). Private DNS zones for Cosmos DB (`privatelink.documents.azure.com`), Cognitive Services (`privatelink.cognitiveservices.azure.com`), and OpenAI (`privatelink.openai.azure.com`) linked to VNET. Cosmos DB and Foundry both have private endpoints via AVM module `privateEndpoints` parameter. Container Apps Environment uses `infrastructureSubnetId` (not `infrastructureSubnetResourceId`) with `internal: false` to remain publicly accessible. Control via `enablePublicNetworkAccess` param: when true, hybrid mode (public + private); when false, private-only via VNET.
📌 VNET orchestration complete (2026-03-20): Orchestration log, session log, and decision merged. Kaylee notified for backend connectivity validation.
