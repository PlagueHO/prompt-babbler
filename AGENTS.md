# Prompt Babbler — Agent Instructions

> [!IMPORTANT]
> The AGENT mantra:
> Every line of code is tech debt. Always pay down tech debt whenever adding new features. Simplicity, readability, testability and maintainability are more important than cleverness or optimization. SOLID, DRY, KISS, YAGNI, and other good software design principles are your friends. If it hurts, refactor.

See `.github/copilot-instructions.md` for code style and patterns.

## Layout

```text
prompt-babbler/
├── prompt-babbler-app/        # React 19 + Vite frontend (TypeScript)
├── prompt-babbler-service/    # .NET 10 backend (ASP.NET Core + Aspire)
│   ├── src/
│   │   ├── Api/               # REST controllers, middleware, health checks
│   │   ├── Domain/            # Models, interfaces — no infrastructure deps
│   │   ├── Infrastructure/    # Cosmos DB repos, Azure AI services, DI
│   │   └── Orchestration/     # Aspire AppHost + ServiceDefaults
│   └── tests/
│       ├── unit/              # MSTest + FluentAssertions
│       └── integration/       # Aspire integration tests
├── infra/                     # Azure Bicep IaC
├── docs/                      # Architecture, API ref, quickstarts
├── tests/smoke/               # PowerShell smoke tests
└── .github/workflows/         # CI/CD pipelines
```

## Commands

```bash
# Run everything locally (Aspire orchestrates all components)
aspire run

# --- Backend (.NET) ---
cd prompt-babbler-service
dotnet restore PromptBabbler.slnx
dotnet build PromptBabbler.slnx
dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit
dotnet format PromptBabbler.slnx --verify-no-changes --severity error

# --- Frontend (React) ---
cd prompt-babbler-app
pnpm install
pnpm run build
pnpm test
pnpm lint

# --- Markdown lint (repo root) ---
pnpm install
pnpm lint:md

# --- Infrastructure ---
bicep lint ./infra/main.bicep
```

## Modifying an API Endpoint — Checklist

1. Add or update the domain model in `Domain/Models/`
1. Add or update the interface in `Domain/Interfaces/`
1. Implement the service in `Infrastructure/Services/`
1. Add or update the controller in `Api/Controllers/`
1. Add unit tests with `[TestCategory("Unit")]` in `tests/unit/`
1. Run `dotnet format` and `dotnet test --filter TestCategory=Unit`
1. Update `docs/API.md` if the public API surface changed

## CI Pipeline

- **Lint Markdown**: runs `pnpm lint:md`; fails on any markdownlint violation
- **Lint and Publish Bicep**: runs `bicep lint ./infra/main.bicep`; fails on Bicep errors
- **Lint Backend**: runs `dotnet format --verify-no-changes`; fails if code is not formatted
- **Build and Test Backend**: `dotnet build` then `dotnet test --filter TestCategory=Unit`; fails on build errors or test failures
- **Build and Test Frontend**: `pnpm install`, `pnpm lint`, `vitest run`, `pnpm run build`; fails on lint errors, test failures, or build errors

## Conventions

| Concern | Rule |
|---|---|
| Backend naming | PascalCase types, `_camelCase` private fields, `I` prefix for interfaces |
| Frontend naming | camelCase functions/variables, PascalCase components, kebab-case files |
| Indentation | 4 spaces (C#), 2 spaces (TypeScript/JSON/YAML) |
| Line endings | LF; newline at end of file; no trailing whitespace |
| Numbered lists | Always use `1.` for auto-numbering |
| Commits | Conventional Commits format |
| **Test categories** | **Every MSTest class must have `[TestCategory("Unit")]` or `[TestCategory("Integration")]`** |
| **Sealed classes** | **All C# classes and records must be `sealed` unless designed for inheritance** |
