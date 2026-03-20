# Prompt Babbler

[![CI][ci-shield]][ci-url]
[![CD][cd-shield]][cd-url]
[![License][license-shield]][license-url]
[![Azure][azure-shield]][azure-url]
[![IaC][iac-shield]][iac-url]

A speech-to-prompt web application that captures stream-of-consciousness speech, transcribes it using Azure AI Foundry, and generates structured prompts for target systems like GitHub Copilot.

## Architecture

![Architecture](docs/images/architecture.svg)

### API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/babbles` | GET, POST | List/create babbles |
| `/api/babbles/{id}` | GET, PUT, DELETE | Read/update/delete babble |
| `/api/babbles/{id}/prompts` | GET, POST | List/create generated prompts |
| `/api/babbles/{id}/prompts/{promptId}` | GET, DELETE | Read/delete generated prompt |
| `/api/templates` | GET, POST | List/create prompt templates |
| `/api/templates/{id}` | GET, PUT, DELETE | Read/update/delete template |
| `/api/user` | GET | Get user profile |
| `/api/user/settings` | PUT | Update user settings |
| `/api/babbles/{id}/generate` | POST | Generate prompt (SSE streaming) |
| `/api/babbles/{id}/generate-title` | POST | Generate babble title |
| `/api/transcribe/stream` | WebSocket | Real-time speech transcription |
| `/api/status` | GET | Health check |

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | React 19, TypeScript 5.9, Vite 8, Shadcn/UI, TailwindCSS v4, React Router 7 |
| Backend | .NET 10, ASP.NET Core, Clean Architecture |
| AI Services | Azure AI Foundry (LLM chat + Speech STT via Aspire integration) |
| Data | Azure Cosmos DB (serverless, NoSQL) |
| Orchestration | .NET Aspire (Azure AI Foundry + Cosmos DB provisioning) |
| Networking | Azure VNET with private endpoints for Cosmos DB and AI Foundry |
| Infrastructure | Azure Bicep with Azure Verified Modules (AVM) |
| Auth | Microsoft Entra ID (MSAL + JWT), single-user anonymous mode |
| Testing | Vitest + Testing Library (frontend), MSTest SDK + FluentAssertions + NSubstitute (backend) |
| CI/CD | GitHub Actions (12 workflows: CI, CD, IaC validation, linting, E2E) |

## Quick Start

See [docs/QUICKSTART.md](docs/QUICKSTART.md) for detailed setup instructions.

### Prerequisites

- .NET SDK 10.0.100+
- Node.js 22.x LTS
- pnpm 10.x
- Azure CLI (for Azure authentication)
- Azure subscription with Contributor access

### Run Locally

```bash
# Install dependencies
cd prompt-babbler-app && pnpm install && cd ..
cd prompt-babbler-service && dotnet restore PromptBabbler.slnx && cd ..

# Configure Azure credentials (one-time setup — see docs/QUICKSTART.md for details)
az login --tenant <your-tenant-id>
cd prompt-babbler-service
dotnet user-secrets set "Azure:SubscriptionId" "<your-subscription-id>" --project src/Orchestration/AppHost
dotnet user-secrets set "Azure:TenantId" "<your-tenant-id>" --project src/Orchestration/AppHost

# Start via Aspire (starts both backend and frontend)
cd prompt-babbler-service
dotnet run --project src/Orchestration/AppHost/PromptBabbler.AppHost.csproj
```

> **Note:** On first run, Aspire automatically provisions an Azure resource group
> and Azure AI Foundry resources (chat + STT model deployments) in your subscription.
> This takes several minutes. Subsequent runs reuse existing resources.

### Run Tests

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

## Project Structure

```text
prompt-babbler/
├── prompt-babbler-service/     # .NET backend (Clean Architecture)
│   ├── src/Api/                # ASP.NET Core API controllers
│   ├── src/Domain/             # Business models & interfaces
│   ├── src/Infrastructure/     # Azure OpenAI SDK, file settings
│   ├── src/Orchestration/      # Aspire AppHost + ServiceDefaults
│   └── tests/                  # Unit + integration tests
├── prompt-babbler-app/         # React frontend
│   ├── src/components/         # UI components
│   ├── src/hooks/              # Custom React hooks
│   ├── src/services/           # API client, transcription stream
│   ├── src/pages/              # Page components
│   └── tests/                  # Vitest tests
├── .github/workflows/          # CI/CD pipelines (12 workflows)
├── specs/                      # Feature specifications
└── infra/                      # Azure Bicep infrastructure (VNET, Cosmos DB, AI Foundry, RBAC)
```

## License

MIT

<!-- Badge reference links -->
[ci-shield]: https://img.shields.io/github/actions/workflow/status/PlagueHO/prompt-babbler/continuous-integration.yml?branch=main&label=CI
[ci-url]: https://github.com/PlagueHO/prompt-babbler/actions/workflows/continuous-integration.yml
[cd-shield]: https://img.shields.io/github/actions/workflow/status/PlagueHO/prompt-babbler/continuous-delivery.yml?branch=main&label=CD
[cd-url]: https://github.com/PlagueHO/prompt-babbler/actions/workflows/continuous-delivery.yml
[license-shield]: https://img.shields.io/github/license/PlagueHO/prompt-babbler
[license-url]: https://github.com/PlagueHO/prompt-babbler/blob/main/LICENSE
[azure-shield]: https://img.shields.io/badge/Azure-Solution%20Accelerator-0078D4?logo=microsoftazure&logoColor=white
[azure-url]: https://azure.microsoft.com/
[iac-shield]: https://img.shields.io/badge/Infrastructure%20as%20Code-Bicep-5C2D91?logo=azurepipelines&logoColor=white
[iac-url]: https://learn.microsoft.com/azure/azure-resource-manager/bicep/overview
