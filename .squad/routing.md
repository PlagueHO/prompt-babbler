# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| React components, hooks, UI | Inara ⚛️ | Build pages, fix styling, add components, MSAL integration, OTel browser |
| .NET backend, APIs, Cosmos DB | Kaylee 🔧 | API endpoints, services, repositories, Speech SDK, AI integration |
| Bicep, CI/CD, Aspire, deployment | Wash ⚙️ | Infrastructure changes, workflow updates, RBAC, container builds |
| Code review | Mal 🏗️ | Review PRs, check architecture, enforce Clean Architecture |
| Testing | Zoe 🧪 | Write tests, find edge cases, verify fixes, a11y testing |
| Scope & priorities | Mal 🏗️ | What to build next, trade-offs, decisions, triage |
| Architecture & cross-cutting | Mal 🏗️ | System design, Clean Architecture boundaries, API contracts |
| Async issue work (bugs, tests, small features) | @copilot 🤖 | Well-defined tasks matching capability profile |
| Session logging | Scribe 📋 | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, evaluate @copilot fit, assign `squad:{member}` label | Mal 🏗️ |
| `squad:mal` | Architecture or cross-cutting work | Mal 🏗️ |
| `squad:inara` | Frontend work (React, UI, hooks) | Inara ⚛️ |
| `squad:kaylee` | Backend work (.NET, APIs, Cosmos) | Kaylee 🔧 |
| `squad:wash` | Infrastructure work (Bicep, CI/CD, Aspire) | Wash ⚙️ |
| `squad:zoe` | Testing work (unit, integration, a11y) | Zoe 🧪 |
| `squad:{name}` | Pick up issue and complete the work | Named member |
| `squad:copilot` | Assign to @copilot for autonomous work (if enabled) | @copilot 🤖 |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, evaluating @copilot's capability profile, assigning the right `squad:{member}` label, and commenting with triage notes.
1. **@copilot evaluation:** The Lead checks if the issue matches @copilot's capability profile (🟢 good fit / 🟡 needs review / 🔴 not suitable). If it's a good fit, the Lead may route to `squad:copilot` instead of a squad member.
1. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
1. When `squad:copilot` is applied and auto-assign is enabled, `@copilot` is assigned on the issue and picks it up autonomously.
1. Members can reassign by removing their label and adding another member's label.
1. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

### Lead Triage Guidance for @copilot

When triaging, the Lead should ask:

1. **Is this well-defined?** Clear title, reproduction steps or acceptance criteria, bounded scope → likely 🟢
1. **Does it follow existing patterns?** Adding a test, fixing a known bug, updating a dependency → likely 🟢
1. **Does it need design judgment?** Architecture, API design, UX decisions → likely 🔴
1. **Is it security-sensitive?** Auth, encryption, access control → always 🔴
1. **Is it medium complexity with specs?** Feature with clear requirements, refactoring with tests → likely 🟡

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
1. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
1. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
1. **When two agents could handle it**, pick the one whose domain is the primary concern.
1. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
1. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
1. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
1. **@copilot routing** — when evaluating issues, check @copilot's capability profile in `team.md`. Route 🟢 good-fit tasks to `squad:copilot`. Flag 🟡 needs-review tasks for PR review. Keep 🔴 not-suitable tasks with squad members.
