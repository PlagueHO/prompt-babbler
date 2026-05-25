---
title: Security Patterns
description: Security patterns for logging, authentication boundaries, and data handling in Prompt Babbler.
---

## Purpose

This page documents the minimum security controls we enforce in code and CI for Prompt Babbler.

## Logging Safety

Prompt Babbler treats user-provided transcription text and access-code values as sensitive.

* Never log raw transcription content from `e.Result.Text`, `evt.Text`, request body payloads, or equivalent fields.
* Never log access-code values from headers, query strings, configuration, or local variables.
* For operational diagnostics, log metadata only.
  * Use `textLength`, `durationMs`, `offsetTicks`, counters, and boolean flags.
* Use structured logging placeholders that describe metadata, not user content.
* Keep central OpenTelemetry log sanitization enabled in `prompt-babbler-service/src/Orchestration/ServiceDefaults/Logging`.

## Authentication and Access Control

* Keep `[Authorize]` and `[RequiredScope("access_as_user")]` on protected controllers.
* Validate access-code values with constant-time comparison (`FixedTimeEquals`).
* Reject invalid access-code requests with `401 Unauthorized` and generic error responses.
* Do not return diagnostic details that reveal secrets, tokens, or internal configuration.

## Input Validation

* Validate external input at controller boundaries.
* Return `BadRequest` for invalid input before invoking downstream services.
* Avoid logging rejected payload content.

## AI Agent and CI Guardrails

These controls are enforced to reduce regression risk:

* Repository instructions in `.github/copilot-instructions.md`, `AGENTS.md`, and `.github/instructions/logging-security.instructions.md` explicitly prohibit unsafe logging.
* CI includes a log-safety check in `.github/workflows/continuous-integration.yml` that fails pull requests when unsafe log patterns are introduced.
* CI supports a narrow regex allowlist in `.github/log-safety-allowlist.txt` for known-safe exceptions. Keep entries minimal and specific.

## Related Documentation

* [Authentication](../authentication.md)
* [Architecture](../architecture.md)
* [API Reference](../api.md)
