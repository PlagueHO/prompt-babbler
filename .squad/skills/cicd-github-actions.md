# Skill: CI/CD & GitHub Actions

## Confidence: medium

## Overview

prompt-babbler uses 16 GitHub Actions workflows organized into continuous integration, continuous delivery, and squad automation. Authentication uses federated OIDC (no client secrets). Container images are published to GitHub Container Registry. Infrastructure is managed via Azure Developer CLI (`azd`).

## Workflow Architecture

### Continuous Integration (triggered on PRs and pushes to main)

```yaml
continuous-integration.yml (orchestrator)
├── lint-markdown.yml             # markdownlint-cli2
├── lint-and-publish-bicep.yml    # bicep lint + artifact
├── build-and-publish-backend-service.yml
│   ├── dotnet-lint (dotnet format --verify-no-changes)
│   ├── dotnet-build-test (build + unit tests + coverage)
│   └── publish-api-app (dotnet publish)
└── build-and-publish-frontend-app.yml
    ├── pnpm lint (ESLint)
    ├── vitest run --coverage
    └── pnpm build (Vite)
```

### Continuous Delivery (triggered on pushes to main, version tags)

```yaml
continuous-delivery.yml (orchestrator)
├── set-build-variables.yml       # GitVersion SemVer
├── lint-and-publish-bicep.yml
├── build-and-publish-backend-service.yml (Release config)
├── build-and-publish-frontend-app.yml
├── build-and-push-api-container  # Only on v* tags → GHCR
├── validate-infrastructure.yml   # Bicep what-if
└── e2e-test.yml                  # Ephemeral Azure infra
```

### Squad Automation

```yaml
squad-triage.yml          # Ralph triages issues labeled 'squad'
squad-issue-assign.yml    # Auto-assigns when squad:{member} label applied
squad-heartbeat.yml       # Ralph monitors progress on close/label events
sync-squad-labels.yml     # Syncs GitHub labels from team roster
```

## Key Patterns

### Reusable Workflows

Most workflows are `workflow_call` (reusable). They accept inputs and secrets, produce artifacts and outputs. The orchestrator workflows (`continuous-integration.yml`, `continuous-delivery.yml`) compose them.

### Artifact Sharing

| Artifact | Producer | Consumer |
|----------|----------|----------|
| `infrastructure_bicep` | lint-and-publish-bicep | validate-infrastructure |
| `api-app-published` | build-and-publish-backend | build-and-push-container |
| `prompt-babbler-frontend` | build-and-publish-frontend | deployment |
| `dotnet-test-results` | build-and-publish-backend | PR checks |
| `frontend-vitest-test-results` | build-and-publish-frontend | PR checks |

### Authentication

- **Federated OIDC** via `azure/login@v3` — no client secrets
- **GHCR**: `${{ github.token }}` for container push
- **Permissions**: Explicitly declared per workflow (contents:read, packages:write, etc.)

### Test Execution

**Backend (dotnet):**

```bash
dotnet test --filter TestCategory=Unit \
  --coverage --coverage-output-format cobertura \
  --report-trx
```

- Test categories: `[TestCategory("Unit")]`, `[TestCategory("Integration")]`
- Coverage: coverlet → cobertura → ReportGenerator HTML
- Results: `.trx` files + sticky PR comment

**Frontend (vitest):**

```bash
pnpm exec vitest run --coverage
```

- Reporters: json, json-summary, html, cobertura, junit, github-actions
- Coverage: `@vitest/coverage-v8`

### Container Image Publishing

Only on version tags (`refs/tags/v*`):

```yaml
tags: |
  ghcr.io/plagueho/prompt-babbler-api:latest
  ghcr.io/plagueho/prompt-babbler-api:${{ version }}
```

### Infrastructure Validation

Bicep what-if deployment (`azure/bicep-deploy@v2` with operation `whatIf`):

- Subscription-scoped
- Uses artifact from lint-and-publish-bicep
- No actual resource changes

### E2E Testing

- Provisions ephemeral Azure infrastructure (anonymous mode — no Entra ID)
- Runs tests against real Azure resources
- Always tears down infrastructure (even on failure)
- 3-phase cleanup: azd down → fallback resource group delete → purge soft-deleted Cognitive Services

## Versioning

- **GitVersion 6.3.x** for automatic SemVer from git history
- Configuration: `GitVersion.yml` at repo root
- Outputs: `FullSemVer`, `MajorMinorPatch`, `SemVer`, `NuGetVersion`

## Dependency Management

**Dependabot** (`dependabot.yml`) with grouped updates:

| Ecosystem | Schedule | Groups |
|-----------|----------|--------|
| GitHub Actions | Weekly (Monday) | ci prefix |
| NuGet | Weekly | Aspire, OpenTelemetry, Testing, Azure |
| npm/pnpm (app) | Weekly | React, Testing, Tailwind, ESLint, Vite |
| npm/pnpm (root) | Weekly | Markdown linting |

## Tooling Versions

| Tool | Version | Setup |
|------|---------|-------|
| .NET SDK | 10.0.100 | `actions/setup-dotnet@v4` |
| Node.js | 22 | `actions/setup-node@v4` |
| pnpm | 10 | `pnpm/action-setup@v4` |
| Azure CLI | Latest | `azure/login@v3` |
| azd | Latest | `azure/setup-azd@v2` |
| GitVersion | 6.3.x | `gittools/actions@v3.2.0` |
| Bicep | Latest | `azure/bicep-deploy@v2` |
| Docker Buildx | Latest | `docker/setup-buildx-action@v3` |
