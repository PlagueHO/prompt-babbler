<!-- markdownlint-disable-file -->
# Release Changes: MP3 Batch Transcription — Review Rework

**Related Plan**: mp3-batch-transcription-rework-plan.instructions.md
**Implementation Date**: 2026-05-02

## Summary

Address review findings from the MP3 batch transcription implementation: add file-extension validation (IV-001), language parameter validation (IV-002), Consumes attribute (IV-003), transcription error handling (IV-008), missing test coverage (IV-006), and API documentation (WI-07).

## Changes

### Added

### Modified

* prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Added file-extension validation (IV-001), language parameter validation (IV-002), `[Consumes("multipart/form-data")]` attribute (IV-003), try/catch around TranscribeAsync returning 502 (IV-008)
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerUploadTests.cs — Added three new tests: TranscriptionServiceThrows_Returns502, ValidContentTypeInvalidExtension_ReturnsBadRequest, InvalidLanguageParameter_ReturnsBadRequest
* docs/API.md — Added `POST /api/babbles/upload` endpoint documentation
* .copilot-tracking/changes/2026-05-02/mp3-batch-transcription-changes.md — Added missing ITranscriptionClientWrapper.cs and TranscriptionClientWrapper.cs entries

### Removed

## Additional or Deviating Changes

## Release Summary

**Total files affected**: 4 (0 added, 4 modified, 0 removed)

### Files Modified

* `prompt-babbler-service/src/Api/Controllers/BabbleController.cs` — File-extension validation, language parameter validation, `[Consumes]` attribute, try/catch for transcription errors returning 502
* `prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerUploadTests.cs` — Three new tests covering transcription failure (502), invalid extension, and invalid language
* `docs/API.md` — Added `POST /api/babbles/upload` endpoint documentation
* `.copilot-tracking/changes/2026-05-02/mp3-batch-transcription-changes.md` — Added missing ITranscriptionClientWrapper entries

### Validation Results

* Backend build: ✅ Passed (12/12 projects, 0 errors)
* Backend format: ✅ Passed (no violations)
* Backend unit tests: ✅ 215 passed, 0 failed
* Frontend lint: ✅ Passed (no ESLint errors)
* Frontend tests: ✅ 125 passed (26 files), 0 failed
* Frontend build: ✅ Passed
