# Squad Team

> Prompt Babbler — speech-to-prompt web application

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. Does not generate domain artifacts. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Mal | Lead | `.squad/agents/mal/charter.md` | ✅ Active |
| Inara | Frontend Dev | `.squad/agents/inara/charter.md` | ✅ Active |
| Kaylee | Backend Dev | `.squad/agents/kaylee/charter.md` | ✅ Active |
| Wash | DevOps/Infra | `.squad/agents/wash/charter.md` | ✅ Active |
| Zoe | Tester | `.squad/agents/zoe/charter.md` | ✅ Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | — | 🔄 Monitor |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**

- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs
- Boilerplate/scaffolding generation
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**

- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas

**🔴 Not suitable — route to squad member instead:**

- Architecture decisions and system design
- Multi-system integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical changes (auth, encryption, access control)
- Performance-critical paths requiring benchmarking
- Changes requiring cross-team discussion

## Project Context

- **Owner:** Daniel Scott-Raynsford
- **Stack:** React 19 + TypeScript 5.9 + Vite 8 (frontend), .NET 10 + ASP.NET Core (backend), Azure AI Foundry (LLM + Speech), .NET Aspire (orchestration), Azure Bicep (infrastructure)
- **Description:** Speech-to-prompt web app that captures stream-of-consciousness speech, transcribes it using Azure AI Foundry, and generates structured prompts for target systems like GitHub Copilot
- **Created:** 2026-03-19
