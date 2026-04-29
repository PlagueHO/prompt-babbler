# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| {domain 1} | {Name} | {example tasks} |
| {domain 2} | {Name} | {example tasks} |
| {domain 3} | {Name} | {example tasks} |
| Code review | {Name} | Review PRs, check quality, suggest improvements |
| Testing | {Name} | Write tests, find edge cases, verify fixes |
| Scope & priorities | {Name} | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
1. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
1. Members can reassign by removing their label and adding another member's label.
1. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
1. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
1. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
1. **When two agents could handle it**, pick the one whose domain is the primary concern.
1. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
1. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
1. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
