---
name: "error-recovery"
description: "Standard recovery patterns for all squad agents. When something fails, adapt — don't just report the failure."
domain: "reliability, agent-coordination"
confidence: "high"
license: MIT
---

# Error Recovery Patterns

Standard recovery patterns for all squad agents. When something fails, **adapt** — don't just report the failure.

---

## 1. Retry with Backoff

**When:** Transient failures — API timeouts, rate limits, network errors, temporary service unavailability.

**Pattern:**

1. Wait briefly, then retry (start at 2s, double each attempt)
1. Maximum 3 retries before escalating
1. Log each attempt with the error received

**Example:** API call returns 429 Too Many Requests → wait 2s → retry → wait 4s → retry → wait 8s → retry → escalate if still failing.

---

## 2. Fallback Alternatives

**When:** Primary tool or approach fails and an alternative exists.

**Pattern:**

1. Attempt primary approach
1. On failure, identify alternative tool/method
1. Try the alternative with the same intent
1. Document which alternative was used and why

**Example:** Primary CLI tool fails → fall back to direct API call for the same operation.

---

## 3. Diagnose-and-Fix

**When:** Build failures, test failures, linting errors — structured errors with actionable output.

**Pattern:**

1. Read the full error output carefully
1. Identify the root cause from error messages
1. Attempt a targeted fix
1. Re-run to verify the fix
1. Maximum 3 fix-retry cycles before escalating

**Example:** Build fails with a type error → check for missing import → add it → rebuild.

---

## 4. Escalate with Context

**When:** Recovery attempts have been exhausted, or the failure requires human judgment.

**Pattern:**

1. Summarize what was attempted and what failed
1. Include the exact error messages
1. State what you believe the root cause is
1. Suggest next steps or who might be able to help
1. Hand off to the coordinator or the appropriate specialist

**Example:** After 3 failed build attempts → "Build fails on line 42 with null reference. Tried X, Y, Z. Likely a design issue in the Foo module. Recommend the code owner review."

---

## 5. Graceful Degradation

**When:** A non-critical step fails but the overall task can still deliver value.

**Pattern:**

1. Determine if the failed step is critical to the task outcome
1. If non-critical, log the failure and continue
1. Deliver partial results with a clear note of what was skipped
1. Offer to retry the skipped step separately

**Example:** Generating a report with 5 sections — section 3 data source is unavailable → produce the report with 4 sections, note that section 3 was skipped and why.

---

## Applying These Patterns

Each agent should reference these patterns in their charter's `## Error Recovery` section, tailored to their domain. The charter should list the agent's most common failure modes and map each to the appropriate pattern above.

**Selection guide:**

| Failure Type | Primary Pattern | Fallback Pattern |
|---|---|---|
| Network/API transient | Retry with Backoff | Escalate with Context |
| Tool/dependency missing | Fallback Alternatives | Escalate with Context |
| Build/test error | Diagnose-and-Fix | Escalate with Context |
| Auth/permissions | Retry with Backoff | Escalate with Context |
| Non-critical data missing | Graceful Degradation | — |
| Unknown/novel error | Escalate with Context | — |
