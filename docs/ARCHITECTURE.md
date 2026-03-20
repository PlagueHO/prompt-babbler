# Architecture

This document describes the architecture, tech stack, project structure, and API surface of Prompt Babbler.

## Overview

![Architecture](images/architecture.svg)

Prompt Babbler is a speech-to-prompt web application with two deployment modes:

- **Local development** — .NET Aspire orchestrates all services (API, frontend, Cosmos DB emulator, Azure AI Foundry cloud resource).
- **Azure deployment** — Azure Developer CLI (`azd`) provisions Container Apps, Static Web Apps, Cosmos DB, AI Foundry, and networking infrastructure via Bicep.

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

## Project Structure

```text
prompt-babbler/
├── prompt-babbler-service/     # .NET backend (Clean Architecture)
│   ├── src/Api/                # ASP.NET Core API controllers
│   ├── src/Domain/             # Business models & interfaces
│   ├── src/Infrastructure/     # Azure OpenAI SDK, Cosmos DB, Speech SDK
│   ├── src/Orchestration/      # Aspire AppHost + ServiceDefaults
│   └── tests/                  # Unit + integration tests
├── prompt-babbler-app/         # React frontend
│   ├── src/components/         # UI components (Shadcn/UI + custom)
│   ├── src/hooks/              # Custom React hooks
│   ├── src/services/           # API client, transcription stream
│   ├── src/pages/              # Page components
│   └── tests/                  # Vitest tests
├── infra/                      # Azure Bicep infrastructure
│   ├── main.bicep              # Main deployment template
│   ├── main.bicepparam         # Parameters (azd environment vars)
│   ├── model-deployments.json  # AI model deployment definitions
│   ├── entra-id/               # Entra ID app registration Bicep
│   ├── hooks/                  # azd preprovision hooks
│   └── cognitive-services/     # Custom Foundry module
├── docs/                       # Documentation
│   ├── QUICKSTART-LOCAL.md     # Local development guide
│   ├── QUICKSTART-AZURE.md     # Azure deployment guide
│   ├── API.md                  # Full API reference
│   └── CICD.md                 # CI/CD pipeline setup
├── .github/workflows/          # CI/CD pipelines (12 workflows)
└── specs/                      # Feature specifications
```

## Backend Architecture

The backend follows [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) with strict dependency direction:

- **Domain** (`src/Domain/`) — Business models (C# records with `init` properties), repository and service interfaces. No external dependencies.
- **Infrastructure** (`src/Infrastructure/`) — Azure service implementations: Cosmos DB repositories, Azure OpenAI prompt generation, Azure Speech SDK transcription. Depends on Domain.
- **Api** (`src/Api/`) — ASP.NET Core controllers, middleware, dependency injection registration. Depends on Domain and Infrastructure.
- **Orchestration** (`src/Orchestration/`) — .NET Aspire AppHost and ServiceDefaults. Orchestrates all resources for local development.

### Authentication Modes

The backend supports two authentication modes, determined by the presence of the `AzureAd:ClientId` configuration value:

| Mode | Trigger | Behavior |
|------|---------|----------|
| **Anonymous single-user** | `AzureAd:ClientId` is empty | All requests use synthetic `_anonymous` user identity. No sign-in required. |
| **Entra ID multi-user** | `AzureAd:ClientId` is set | JWT Bearer token validation via Microsoft.Identity.Web. User ID from Entra ID object ID claim. |

### Data Layer

Azure Cosmos DB (serverless) with 4 containers in database `prompt-babbler`:

| Container | Partition Key | Description |
|-----------|---------------|-------------|
| `babbles` | `/userId` | User speech transcriptions |
| `generated-prompts` | `/babbleId` | Generated prompts (child of babble) |
| `prompt-templates` | `/userId` | Prompt templates (built-in use `_builtin` userId) |
| `users` | `/userId` | User profiles and settings |

Cascade delete: deleting a babble also deletes all its generated prompts.

## Frontend Architecture

Single-page application built with React 19:

- **Routing** — React Router v7 with `BrowserRouter`
- **UI** — Shadcn/UI (New York style) + Radix UI primitives + Lucide icons
- **Styling** — TailwindCSS v4 with CSS variable theming
- **Forms** — React Hook Form + Zod v4 validation
- **Auth** — Conditional `<MsalProvider>` wrapping based on `isAuthConfigured` flag (MSAL_CLIENT_ID presence at build time)
- **Telemetry** — OpenTelemetry browser SDK sending traces/metrics to Aspire Dashboard via HTTP OTLP

## API Endpoints

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

See [API.md](API.md) for the full API reference with request/response schemas.

## Azure Infrastructure

When deployed to Azure via `azd up`, the following resources are provisioned (see [infra/README.md](../infra/README.md) for details):

| Resource | Purpose |
|----------|---------|
| Azure Container App | .NET backend API (managed identity, port 8080) |
| Azure Static Web App | React frontend (free tier) |
| Azure Cosmos DB | Serverless NoSQL (4 containers) |
| Azure AI Foundry (AIServices) | LLM (gpt-5.3-chat) + Speech Service |
| Container Apps Environment | Managed environment with VNet integration |
| Virtual Network | Private networking with subnets and private endpoints |
| Private DNS Zones | Private link DNS for Cosmos DB and Cognitive Services |
| Log Analytics Workspace | Centralized logging and diagnostics |
| Application Insights | APM and distributed tracing |

RBAC roles assigned to the Container App managed identity:

- Cognitive Services OpenAI User
- Cognitive Services Speech User
- Cosmos DB Built-in Data Contributor

## CI/CD

GitHub Actions with 12 workflows covering CI, CD, IaC validation, linting, and E2E testing. See [CICD.md](CICD.md) for pipeline configuration.

| Workflow | Trigger | Description |
|----------|---------|-------------|
| **Continuous Integration** | PR + push to main | Lint, build, test (frontend + backend) |
| **Continuous Delivery** | Push to main (path-filtered) | Build, test, Docker push, validate infra, E2E |
