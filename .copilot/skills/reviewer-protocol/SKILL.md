---
name: "reviewer-protocol"
description: "Reviewer rejection workflow and strict lockout semantics"
domain: "orchestration"
confidence: "high"
source: "extracted"
---

## Context

When a team member has a **Reviewer** role (e.g., Tester, Code Reviewer, Lead), they may approve or reject work from other agents. On rejection, the coordinator enforces strict lockout rules to ensure the original author does NOT self-revise. This prevents defensive feedback loops and ensures independent review.

## Patterns

### Reviewer Rejection Protocol

When a team member has a **Reviewer** role:

- Reviewers may **approve** or **reject** work from other agents.
- On **rejection**, the Reviewer may choose ONE of:
  1. **Reassign:** Require a *different* agent to do the revision (not the original author).
  1. **Escalate:** Require a *new* agent be spawned with specific expertise.
- The Coordinator MUST enforce this. If the Reviewer says "someone else should fix this," the original agent does NOT get to self-revise.
- If the Reviewer approves, work proceeds normally.

### Strict Lockout Semantics

When an artifact is **rejected** by a Reviewer:

1. **The original author is locked out.** They may NOT produce the next version of that artifact. No exceptions.
1. **A different agent MUST own the revision.** The Coordinator selects the revision author based on the Reviewer's recommendation (reassign or escalate).
1. **The Coordinator enforces this mechanically.** Before spawning a revision agent, the Coordinator MUST verify that the selected agent is NOT the original author. If the Reviewer names the original author as the fix agent, the Coordinator MUST refuse and ask the Reviewer to name a different agent.
1. **The locked-out author may NOT contribute to the revision** in any form — not as a co-author, advisor, or pair. The revision must be independently produced.
1. **Lockout scope:** The lockout applies to the specific artifact that was rejected. The original author may still work on other unrelated artifacts.
1. **Lockout duration:** The lockout persists for that revision cycle. If the revision is also rejected, the same rule applies again — the revision author is now also locked out, and a third agent must revise.
1. **Deadlock handling:** If all eligible agents have been locked out of an artifact, the Coordinator MUST escalate to the user rather than re-admitting a locked-out author.

## Examples

**Example 1: Reassign after rejection**

1. Fenster writes authentication module
1. Hockney (Tester) reviews → rejects: "Error handling is missing. Verbal should fix this."
1. Coordinator: Fenster is now locked out of this artifact
1. Coordinator spawns Verbal to revise the authentication module
1. Verbal produces v2
1. Hockney reviews v2 → approves
1. Lockout clears for next artifact

**Example 2: Escalate for expertise**

1. Edie writes TypeScript config
1. Keaton (Lead) reviews → rejects: "Need someone with deeper TS knowledge. Escalate."
1. Coordinator: Edie is now locked out
1. Coordinator spawns new agent (or existing TS expert) to revise
1. New agent produces v2
1. Keaton reviews v2

**Example 3: Deadlock handling**

1. Fenster writes module → rejected
1. Verbal revises → rejected
1. Hockney revises → rejected
1. All 3 eligible agents are now locked out
1. Coordinator: "All eligible agents have been locked out. Escalating to user: [artifact details]"

**Example 4: Reviewer accidentally names original author**

1. Fenster writes module → rejected
1. Hockney says: "Fenster should fix the error handling"
1. Coordinator: "Fenster is locked out as the original author. Please name a different agent."
1. Hockney: "Verbal, then"
1. Coordinator spawns Verbal

## Anti-Patterns

- ❌ Allowing the original author to self-revise after rejection
- ❌ Treating the locked-out author as an "advisor" or "co-author" on the revision
- ❌ Re-admitting a locked-out author when deadlock occurs (must escalate to user)
- ❌ Applying lockout across unrelated artifacts (scope is per-artifact)
- ❌ Accepting the Reviewer's assignment when they name the original author (must refuse and ask for a different agent)
- ❌ Clearing lockout before the revision is approved (lockout persists through revision cycle)
- ❌ Skipping verification that the revision agent is not the original author
