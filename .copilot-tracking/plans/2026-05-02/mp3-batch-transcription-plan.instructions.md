---
applyTo: '.copilot-tracking/changes/2026-05-02/mp3-batch-transcription-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: MP3 File Upload Batch Transcription

## Overview

Add an Upload button on the home panel that lets users upload audio files (MP3, WAV, WebM, OGG) for batch transcription via the Azure Fast Transcription API, creating a babble from the transcribed text.

## Objectives

### User Requirements

* Add an Upload button (outline variant) on the home panel alongside the existing "New Babble" button — Source: user task request
* Implement batch transcription of uploaded audio files using Azure Fast Transcription API — Source: user task request
* Backend receives file via multipart/form-data, transcribes in a single API call, creates a babble — Source: user task request
* Navigate to the babble detail page after successful upload/transcription — Source: user task request

### Derived Objectives

* Add `Azure.AI.Speech.Transcription` package to central package management — Derived from: SDK required for Fast Transcription API
* Create `IFileTranscriptionService` domain interface — Derived from: project convention (interfaces in Domain/Interfaces/)
* Register `TranscriptionClient` and service implementation in DI — Derived from: project DI pattern
* Configure request size limit (500 MB) on the upload endpoint — Derived from: ASP.NET Core default is 28.6 MB, requirement is 500 MB
* Add frontend `uploadAudioFile` function using raw `fetch` with FormData — Derived from: `fetchJson` hardcodes JSON content-type
* Create `useFileUpload` hook following existing hook patterns — Derived from: project convention (hooks in src/hooks/)
* Add unit tests for the new transcription service — Derived from: project testing conventions

## Context Summary

### Project Files

* prompt-babbler-service/src/Api/Controllers/BabbleController.cs - Target for new upload endpoint (insert after line ~330)
* prompt-babbler-service/src/Domain/Interfaces/ITranscriptionService.cs - Existing realtime interface; new file-based interface needed
* prompt-babbler-service/src/Infrastructure/DependencyInjection.cs - DI registration target (72 lines total)
* prompt-babbler-service/src/Infrastructure/Services/AzureSpeechTranscriptionService.cs - Existing realtime service (pattern reference)
* prompt-babbler-service/Directory.Packages.props - Central package management (47 lines)
* prompt-babbler-app/src/pages/HomePage.tsx - Upload button location (line 42, flex gap-2 div)
* prompt-babbler-app/src/services/api-client.ts - New uploadAudioFile function target (~370 lines)
* prompt-babbler-app/src/types/index.ts - Babble type definition (no changes needed)

### References

* .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md - Primary research document
* .copilot-tracking/research/subagents/2026-05-02/mp3-upload-codebase-verification.md - Codebase verification
* AGENTS.md - Project conventions and commands
* .github/copilot-instructions.md - Coding standards

### Standards References

* .github/copilot-instructions.md — Sealed classes, DI patterns, controller attributes, test categories
* AGENTS.md — Build/test commands, naming conventions, API endpoint checklist

## Implementation Checklist

### [x] Implementation Phase 1: Backend Domain and Infrastructure

<!-- parallelizable: true -->

* [x] Step 1.1: Add Azure.AI.Speech.Transcription package to Directory.Packages.props
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 15-30)
* [x] Step 1.2: Create IFileTranscriptionService interface in Domain/Interfaces/
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 32-56)
* [x] Step 1.3: Create AzureFastTranscriptionService in Infrastructure/Services/
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 58-107)
* [x] Step 1.4: Register TranscriptionClient and service in DependencyInjection.cs
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 109-140)
* [x] Step 1.5: Add upload endpoint to BabbleController
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 142-210)
* [x] Step 1.6: Validate backend builds and formats correctly
  * Run `dotnet build PromptBabbler.slnx` in prompt-babbler-service/
  * Run `dotnet format PromptBabbler.slnx --verify-no-changes` in prompt-babbler-service/

### [x] Implementation Phase 2: Frontend Upload Feature

<!-- parallelizable: true -->

* [x] Step 2.1: Add uploadAudioFile function to api-client.ts
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 212-258)
* [x] Step 2.2: Create useFileUpload hook
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 260-312)
* [x] Step 2.3: Add upload button and file input to HomePage.tsx
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 314-390)
* [x] Step 2.4: Validate frontend builds and lints correctly
  * Run `pnpm lint` in prompt-babbler-app/
  * Run `pnpm run build` in prompt-babbler-app/

### [x] Implementation Phase 3: Unit Tests

<!-- parallelizable: false -->

* [x] Step 3.1: Create AzureFastTranscriptionServiceTests
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 392-460)
* [x] Step 3.2: Create BabbleController upload endpoint tests
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 462-540)
* [x] Step 3.3: Create useFileUpload hook tests
  * Details: .copilot-tracking/details/2026-05-02/mp3-batch-transcription-details.md (Lines 542-600)

### [x] Implementation Phase 4: Validation

<!-- parallelizable: false -->

* [x] Step 4.1: Run full project validation
  * Execute `dotnet build PromptBabbler.slnx` in prompt-babbler-service/
  * Execute `dotnet format PromptBabbler.slnx --verify-no-changes` in prompt-babbler-service/
  * Execute `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit` in prompt-babbler-service/
  * Execute `pnpm lint` in prompt-babbler-app/
  * Execute `pnpm test` in prompt-babbler-app/
  * Execute `pnpm run build` in prompt-babbler-app/
* [ ] Step 4.2: Fix minor validation issues
  * Iterate on lint errors and build warnings
  * Apply fixes directly when corrections are straightforward
* [ ] Step 4.3: Report blocking issues
  * Document issues requiring additional research
  * Provide user with next steps and recommended planning

## Planning Log

See .copilot-tracking/plans/logs/2026-05-02/mp3-batch-transcription-log.md for discrepancy tracking, implementation paths considered, and suggested follow-on work.

## Dependencies

* Azure.AI.Speech.Transcription v1.0.0-beta.2 NuGet package
* Existing Azure Cognitive Services endpoint (already provisioned for real-time transcription)
* Azure.Identity (already in project) for DefaultAzureCredential/TokenCredential
* lucide-react Upload icon (already available in the package)

## Success Criteria

* User can click Upload button on HomePage and select an audio file — Traces to: user requirement (Upload button)
* File is sent to POST /api/babbles/upload as multipart/form-data — Traces to: user requirement (backend receives file)
* Backend transcribes file via Azure Fast Transcription API and creates babble — Traces to: user requirement (batch transcription)
* User is navigated to /babble/{id} with success toast after upload completes — Traces to: user requirement (navigate to babble detail)
* Audio files up to 500 MB are supported — Traces to: research finding (Fast Transcription API limit)
* All existing unit tests pass — Traces to: derived objective (no regressions)
* New unit tests cover transcription service and upload endpoint — Traces to: project convention (test coverage)
* Backend builds and formats without errors — Traces to: CI pipeline requirements
* Frontend builds and lints without errors — Traces to: CI pipeline requirements
