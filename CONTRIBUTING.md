---
title: Contributing Guide
description: How to contribute code, documentation, and issues to Prompt Babbler.
---

## Before You Start

Thanks for helping improve Prompt Babbler.

* Search existing issues and pull requests before opening a new one.
* For bugs, features, or maintenance requests, use the issue templates in [.github/ISSUE_TEMPLATE](.github/ISSUE_TEMPLATE).
* Keep changes focused. Small, reviewable pull requests move faster.

## Development Setup

1. Clone the repository.
1. Install prerequisites listed in [README.md](README.md).
1. Use the commands in [AGENTS.md](AGENTS.md) to build, test, and lint.

## Dependency Maintenance

Dependabot follows the `dependabot-monthly-v1` policy. Version updates run on the first day of each month at 09:00 in `Pacific/Auckland`. The IANA timezone handles daylight saving changes. GitHub may queue a run, so the start time is not guaranteed.

Each configured ecosystem and directory has one minor/patch group named `<ecosystem>-minor-and-patch`. The matching `<ecosystem>-security` group handles security updates. The configured ecosystems are:

| Ecosystem | Version-update group | Security-update group |
|---|---|---|
| `github-actions` | `github-actions-minor-and-patch` | `github-actions-security` |
| `devcontainers` | `devcontainers-minor-and-patch` | `devcontainers-security` |
| `docker` | `docker-minor-and-patch` | `docker-security` |
| `dotnet-sdk` | `dotnet-sdk-minor-and-patch` | `dotnet-sdk-security` |
| `nuget` | `nuget-minor-and-patch` | `nuget-security` |
| `npm` | `npm-minor-and-patch` | `npm-security` |

Major version updates remain individual pull requests. The existing `react` and `react-dom` major-version exclusions apply to version updates only; they do not block security update pull requests. Dependabot can wait for a release to be three days old before it includes that release in a version update. Security updates are event-driven and do not wait for the monthly schedule or version-update cooldown.

To request an update check before the next scheduled run, open **Insights > Dependency graph > Dependabot** on GitHub and select **Check for updates**. This manual check bypasses the monthly window. Normal CI and human review still apply.

## Pull Request Expectations

* Use a clear title and describe what changed and why.
* Include tests for behavior changes.
* Update docs when API, workflow, or user-facing behavior changes.
* Keep CI green.

## Validation Checklist

Run the checks relevant to your changes before opening a pull request.

```bash
# Repo root
pnpm lint:md

# Frontend
cd prompt-babbler-app
pnpm lint
pnpm test
pnpm run build

# Backend
cd ../prompt-babbler-service
dotnet restore PromptBabbler.slnx
dotnet build PromptBabbler.slnx
dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit
dotnet format PromptBabbler.slnx --verify-no-changes --severity error
```

## Code Standards

* Follow repository guidance in [AGENTS.md](AGENTS.md) and [.github/copilot-instructions.md](.github/copilot-instructions.md).
* Preserve existing conventions and naming patterns.
* Add or update tests for behavior changes.
* Avoid unrelated refactors in the same pull request.

## Security

Do not open public issues for security vulnerabilities. Follow [SECURITY.md](SECURITY.md).
