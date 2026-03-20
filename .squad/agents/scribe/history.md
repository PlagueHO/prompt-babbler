# Project Context

- **Owner:** Daniel Scott-Raynsford
- **Project:** Prompt Babbler — speech-to-prompt web application
- **Stack:** React 19 + .NET 10 + Azure AI Foundry + Aspire + Bicep
- **Created:** 2026-03-19

## Core Context

### Team (Firefly universe)

- 🏗️ Mal — Lead (scope, decisions, code review, triage)
- ⚛️ Inara — Frontend Dev (React, TypeScript, shadcn/ui, MSAL, OTel)
- 🔧 Kaylee — Backend Dev (.NET, Azure AI, Cosmos DB, Clean Architecture)
- ⚙️ Wash — DevOps/Infra (Bicep, GitHub Actions, Aspire, Azure)
- 🧪 Zoe — Tester (MSTest, Vitest, FluentAssertions, test pyramid)
- 📋 Scribe — Session Logger (me — silent, background)
- 🔄 Ralph — Work Monitor

### Key Directories

- `.squad/decisions.md` — shared decision log (I merge to this)
- `.squad/decisions/inbox/` — drop-box for agent decisions
- `.squad/log/` — session logs
- `.squad/orchestration-log/` — per-spawn logs
- `.squad/agents/{name}/history.md` — per-agent learnings

## Recent Updates

📌 Team initialized on 2026-03-19
📌 13 architectural decisions seeded in decisions.md
📌 7 skill files created in .squad/skills/

## Learnings

📌 Team cast from Firefly universe — Mal, Inara, Kaylee, Wash, Zoe + Scribe + Ralph
📌 Windows compatibility: use temp file + `-F` for git commits, never `git -C`
