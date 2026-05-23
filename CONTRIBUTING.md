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
