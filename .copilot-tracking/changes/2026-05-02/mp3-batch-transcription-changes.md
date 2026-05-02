<!-- markdownlint-disable-file -->
# Release Changes: MP3 File Upload Batch Transcription

**Related Plan**: mp3-batch-transcription-plan.instructions.md
**Implementation Date**: 2026-05-02

## Summary

Add MP3/audio file upload with batch transcription via Azure Fast Transcription API. Includes backend endpoint, frontend upload button/hook, and unit tests.

## Changes

### Added

* prompt-babbler-service/src/Domain/Interfaces/IFileTranscriptionService.cs — New domain interface for file-based batch transcription
* prompt-babbler-service/src/Infrastructure/Services/AzureFastTranscriptionService.cs — Implementation using Azure.AI.Speech.Transcription SDK
* prompt-babbler-app/src/hooks/useFileUpload.ts — React hook for file upload state management
* prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/AzureFastTranscriptionServiceTests.cs — Unit tests for AzureFastTranscriptionService
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerUploadTests.cs — Unit tests for BabbleController.UploadAudio endpoint
* prompt-babbler-service/src/Infrastructure/Services/ITranscriptionClientWrapper.cs — Wrapper interface for TranscriptionClient to enable unit testing
* prompt-babbler-service/src/Infrastructure/Services/TranscriptionClientWrapper.cs — Sealed wrapper delegating to Azure SDK TranscriptionClient
* prompt-babbler-app/tests/hooks/useFileUpload.test.ts — Vitest tests for useFileUpload hook

### Modified

* prompt-babbler-service/Directory.Packages.props — Added Azure.AI.Speech.Transcription v1.0.0-beta.2 package version entry
* prompt-babbler-service/src/Infrastructure/PromptBabbler.Infrastructure.csproj — Added PackageReference for Azure.AI.Speech.Transcription
* prompt-babbler-service/src/Infrastructure/DependencyInjection.cs — Registered TranscriptionClient singleton and IFileTranscriptionService
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Added UploadAudio endpoint and GenerateTitleFromText helper; added IFileTranscriptionService constructor parameter
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs — Updated constructor call to include new IFileTranscriptionService parameter
* prompt-babbler-app/src/services/api-client.ts — Added uploadAudioFile function using raw fetch with FormData
* prompt-babbler-app/src/pages/HomePage.tsx — Added upload button, hidden file input, useFileUpload hook usage, uploadError ErrorBanner

### Removed

None

## Additional or Deviating Changes

* Upload endpoint uses `User.GetUserIdOrAnonymous()` instead of `User.GetUserId()`
  * `GetUserId()` does not exist in the codebase; `GetUserIdOrAnonymous()` is the established pattern across all controllers

## Release Summary

**Total files affected**: 13 (6 added, 7 modified, 0 removed)

### Files Created

* `prompt-babbler-service/src/Domain/Interfaces/IFileTranscriptionService.cs` — Domain interface for batch file transcription
* `prompt-babbler-service/src/Infrastructure/Services/AzureFastTranscriptionService.cs` — Azure Fast Transcription API implementation (sealed, primary constructor, structured logging)
* `prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/AzureFastTranscriptionServiceTests.cs` — Unit tests (happy path, empty result, locale defaults)
* `prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerUploadTests.cs` — Upload endpoint unit tests (null file, unsupported type, empty transcription, success)
* `prompt-babbler-app/src/hooks/useFileUpload.ts` — React hook with loading/error state, auth token integration
* `prompt-babbler-app/tests/hooks/useFileUpload.test.ts` — 6 Vitest tests covering all observable behaviors

### Files Modified

* `prompt-babbler-service/Directory.Packages.props` — Added Azure.AI.Speech.Transcription v1.0.0-beta.2
* `prompt-babbler-service/src/Infrastructure/PromptBabbler.Infrastructure.csproj` — Added PackageReference
* `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs` — TranscriptionClient singleton + IFileTranscriptionService registration
* `prompt-babbler-service/src/Api/Controllers/BabbleController.cs` — Added UploadAudio POST endpoint with 500 MB limit and GenerateTitleFromText helper
* `prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs` — Updated constructor to include IFileTranscriptionService parameter
* `prompt-babbler-app/src/services/api-client.ts` — Added uploadAudioFile function (raw fetch, FormData, no Content-Type header)
* `prompt-babbler-app/src/pages/HomePage.tsx` — Upload button, hidden file input, useFileUpload hook, uploadError ErrorBanner

### Validation Results

* Backend build: ✅ Passed (0 errors, 0 warnings)
* Backend format: ✅ Passed (no violations)
* Backend unit tests: ✅ 212 passed, 0 failed
* Frontend lint: ✅ Passed (no ESLint errors)
* Frontend build: ✅ Passed (pre-existing @protobufjs eval warning only)
* Frontend tests: ✅ 125 passed (26 test files), 0 failed

### Deployment Notes

* No new environment variables or Aspire configuration needed — `TranscriptionClient` uses the existing Azure Cognitive Services endpoint and `TokenCredential` already registered
* The `POST /api/babbles/upload` endpoint inherits class-level `[Authorize]` + `[RequiredScope("access_as_user")]`
* Request size limit is set to 500 MB via `[RequestSizeLimit]` on the endpoint
