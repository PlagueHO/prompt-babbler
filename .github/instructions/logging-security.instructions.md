---
description: "Required secure logging rules for transcription and access-control paths"
applyTo: 'prompt-babbler-service/src/**/*.cs'
---

# Logging Security Instructions

Apply these rules to all C# files in `prompt-babbler-service/src`.

## Required Rules

* Do not log request bodies, transcription text, access-code values, tokens, or secrets.
* For transcription events, log metadata only:
  * `textLength`, event counters, `durationMs`, and offsets.
* For access-code validation flows, log only generic context and booleans:
  * HTTP method, `hasHeader`, `hasQueryValue`, status outcomes.
* Do not use log template placeholders for raw content values, including:
  * `{Text}`, `{Transcript}`, `{Transcription}`, `{AccessCode}`.
* Use structured logging and stable property names for metadata fields.

## Defense In Depth

* Central OpenTelemetry sanitization in `src/Orchestration/ServiceDefaults/Logging` must remain enabled.
* Sanitization is a safety net. Safe call-site logging is still required.

## Validation

Before finalizing changes:

1. Check updated log statements for raw user content or secret values.
1. Confirm metadata-only placeholders are used.
1. Ensure CI log-safety checks pass.
