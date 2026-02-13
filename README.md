# Prompt Babbler

A speech-to-prompt web application that captures stream-of-consciousness speech, transcribes it using Azure OpenAI Whisper, and generates structured prompts for target systems like GitHub Copilot.

## Architecture

```text
┌─────────────────────┐     ┌─────────────────────────────┐
│   React Frontend    │     │    .NET Backend API          │
│   (Vite + TS)       │────▶│    (ASP.NET Core)           │
│                     │     │                             │
│  • Record speech    │     │  • POST /api/transcribe     │
│  • Manage babbles   │     │  • POST /api/prompts/gen    │
│  • Generate prompts │     │  • GET/PUT /api/settings    │
│  • Manage templates │     │  • POST /api/settings/test  │
│                     │     │                             │
│  localStorage:      │     │  Azure OpenAI:              │
│  babbles, templates │     │  Whisper STT + LLM          │
└─────────────────────┘     └─────────────────────────────┘
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 19, TypeScript 5.x, Vite, Shadcn/UI, TailwindCSS v4 |
| Backend | .NET 10, ASP.NET Core, Clean Architecture |
| AI Services | Azure OpenAI (Whisper STT + LLM) |
| Orchestration | .NET Aspire |
| Testing | Vitest + Testing Library (frontend), MSTest + FluentAssertions (backend) |
| CI/CD | GitHub Actions |

## Quick Start

See [quickstart.md](specs/001-babble-web-app/quickstart.md) for detailed setup instructions.

### Prerequisites

- .NET SDK 10.0.100+
- Node.js 22.x LTS
- pnpm 10.x
- Azure OpenAI endpoint with LLM and Whisper deployments

### Run Locally

```bash
# Install dependencies
cd prompt-babbler-app && pnpm install && cd ..
cd prompt-babbler-service && dotnet restore PromptBabbler.slnx && cd ..

# Start via Aspire (starts both backend and frontend)
cd prompt-babbler-service
dotnet run --project src/Orchestration/AppHost/PromptBabbler.AppHost.csproj
```

### Run Tests

```bash
# Backend
cd prompt-babbler-service
dotnet test --project tests/unit/Api.UnitTests/PromptBabbler.Api.UnitTests.csproj
dotnet test --project tests/unit/Domain.UnitTests/PromptBabbler.Domain.UnitTests.csproj
dotnet test --project tests/unit/Infrastructure.UnitTests/PromptBabbler.Infrastructure.UnitTests.csproj

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
│   ├── src/services/           # API client, localStorage
│   ├── src/pages/              # Page components
│   └── tests/                  # Vitest tests
├── .github/workflows/          # CI/CD pipelines
├── specs/                      # Feature specifications
└── infra/                      # Azure IaC (planned)
```

## License

MIT
