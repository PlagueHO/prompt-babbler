---
applyTo: '.copilot-tracking/changes/2026-05-02/mp3-batch-transcription-rework-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: MP3 Batch Transcription — Review Rework

## Overview

Address rework items identified in the review of the MP3 batch transcription implementation. Two Major findings (security hardening, error handling) and four Minor findings (language validation, content negotiation, missing test, API docs).

## Objectives

### Review Findings (Major)

* IV-001: Add file-extension validation to `UploadAudio` endpoint — content-type alone is spoofable — Source: review finding
* IV-008: Add try/catch around `TranscribeAsync` returning 502 on Azure API failure — matches existing patterns — Source: review finding

### Review Findings (Minor)

* IV-002: Validate `language` parameter max length and BCP-47 pattern — Source: review finding
* IV-003: Add `[Consumes("multipart/form-data")]` attribute — Source: review finding
* IV-006: Add unit test for `TranscribeAsync` throwing exception — Source: review finding
* Update changes log with missing `ITranscriptionClientWrapper.cs` and `TranscriptionClientWrapper.cs` — Source: review finding
* WI-07: Update `docs/API.md` with new `POST /api/babbles/upload` endpoint — Source: AGENTS.md checklist

## Context Summary

### Project Files

* prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Target for IV-001, IV-002, IV-003, IV-008
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerUploadTests.cs — Target for IV-006 and new validation tests
* docs/API.md — Target for WI-07
* .copilot-tracking/changes/2026-05-02/mp3-batch-transcription-changes.md — Update with missing files

### References

* .copilot-tracking/reviews/2026-05-02/mp3-batch-transcription-plan-review.md — Source review
* .copilot-tracking/plans/2026-05-02/mp3-batch-transcription-plan.instructions.md — Original plan
* .github/copilot-instructions.md — Coding standards
* AGENTS.md — Project conventions

## Implementation Checklist

### [x] Implementation Phase 1: Controller Security and Error Handling

<!-- parallelizable: false -->

* [x] Step 1.1: Add file-extension validation (IV-001)
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-rework-details.md (Lines 11-40)
* [x] Step 1.2: Add language parameter validation (IV-002)
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-rework-details.md (Lines 42-60)
* [x] Step 1.3: Add Consumes attribute (IV-003)
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-rework-details.md (Lines 62-75)
* [x] Step 1.4: Add try/catch around TranscribeAsync (IV-008)
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-rework-details.md (Lines 77-110)
* [x] Step 1.5: Validate backend builds and formats
  * Run `dotnet build PromptBabbler.slnx` in prompt-babbler-service/
  * Run `dotnet format PromptBabbler.slnx --verify-no-changes` in prompt-babbler-service/

### [x] Implementation Phase 2: Unit Tests

<!-- parallelizable: false -->

* [x] Step 2.1: Add test for TranscribeAsync throwing exception (IV-006)
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-rework-details.md (Lines 112-140)
* [x] Step 2.2: Add test for invalid file extension
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-rework-details.md (Lines 142-165)
* [x] Step 2.3: Add test for invalid language parameter
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-rework-details.md (Lines 167-190)
* [x] Step 2.4: Run all unit tests
  * Run `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit` in prompt-babbler-service/

### [x] Implementation Phase 3: Documentation

<!-- parallelizable: true -->

* [x] Step 3.1: Update docs/API.md with POST /api/babbles/upload endpoint
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-rework-details.md (Lines 192-260)
* [x] Step 3.2: Update changes log with missing wrapper files
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-rework-details.md (Lines 262-275)

### [x] Implementation Phase 4: Validation

<!-- parallelizable: false -->

* [x] Step 4.1: Run full project validation
  * Execute `dotnet build PromptBabbler.slnx` in prompt-babbler-service/
  * Execute `dotnet format PromptBabbler.slnx --verify-no-changes` in prompt-babbler-service/
  * Execute `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit` in prompt-babbler-service/
  * Execute `pnpm lint` in prompt-babbler-app/
  * Execute `pnpm test -- --run` in prompt-babbler-app/
  * Execute `pnpm run build` in prompt-babbler-app/

## Dependencies

* None — all rework items are self-contained fixes to existing code

## Success Criteria

* File-extension validation rejects files with wrong extensions regardless of content-type
* Azure API failures return 502 with user-friendly message instead of raw 500
* Language parameter rejects excessively long or malformed values
* All new and existing unit tests pass
* docs/API.md documents the upload endpoint
* Changes log accounts for all created files
