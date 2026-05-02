<!-- markdownlint-disable-file -->
# Review Log: MP3 File Upload Batch Transcription

## Metadata

| Field | Value |
|---|---|
| **Review Date** | 2026-05-02 |
| **Plan** | .copilot-tracking/plans/2026-05-02/mp3-batch-transcription-plan.instructions.md |
| **Changes Log** | .copilot-tracking/changes/2026-05-02/mp3-batch-transcription-changes.md |
| **Research** | .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md |
| **Status** | ⚠️ Needs Rework |

## Severity Summary

| Severity | Count |
|---|---|
| Critical | 0 |
| Major | 2 |
| Minor | 8 |

## RPI Validation Findings

### Phase 1: Backend Domain and Infrastructure — ✅ Passed

All 6 steps fully implemented. Two minor deviations — both justified:

1. **ITranscriptionClientWrapper introduced** (Minor, improvement): Wraps sealed `TranscriptionClient` SDK type to enable unit testing with NSubstitute. Anticipate by the plan's Step 3.1 notes. *Not listed in changes log*.
2. **User.GetUserIdOrAnonymous() instead of User.GetUserId()** (Minor, necessary): Plan referenced `GetUserId()` which doesn't exist; all controllers use `GetUserIdOrAnonymous()`.

### Phase 2: Frontend Upload Feature — ✅ Passed

All 4 steps fully implemented. Zero deviations. Named exports, `@/` path alias, hook patterns, and FormData handling all follow project conventions exactly.

### Phase 3: Unit Tests — ✅ Passed

All 3 steps fully implemented with **bonus test coverage** beyond plan requirements:

| Step | Planned Tests | Actual Tests |
|---|---|---|
| 3.1 AzureFastTranscriptionServiceTests | 4 | 5 (+ CancellationToken) |
| 3.2 BabbleControllerUploadTests | 6 | 7 (+ ValidWavFile) |
| 3.3 useFileUpload.test.ts | 5 | 6 (+ auth token verification) |

All test classes are `sealed`, have `[TestCategory("Unit")]`, and use MSTest + FluentAssertions + NSubstitute (backend) / Vitest + Testing Library (frontend).

### Phase 4: Validation — ✅ Passed

All validation commands passed. No blocking issues.

## Implementation Quality Findings

### Security ⚠️

| ID | Severity | Finding | File | Recommendation |
|---|---|---|---|---|
| IV-001 | **Major** | Content-type validation relies solely on client-provided `file.ContentType` — trivially spoofable. Malicious data with `audio/mpeg` MIME type will be accepted and forwarded to Azure. | BabbleController.cs | Add file-extension validation (`Path.GetExtension(file.FileName)` against `.mp3`, `.wav`, `.webm`, `.ogg`). Optionally validate magic bytes. |
| IV-002 | Minor | `language` form parameter is unvalidated — could be excessively long string sent to external API. | BabbleController.cs | Validate max 20 chars, BCP-47 pattern. |
| IV-003 | Minor | Missing `[Consumes("multipart/form-data")]` attribute. | BabbleController.cs | Add attribute to explicitly restrict content negotiation. |

### Architecture Conformance ✅

No findings. Clean domain/infrastructure separation, correct DI patterns, all classes sealed.

### Code Quality ✅

| ID | Severity | Finding | File | Recommendation |
|---|---|---|---|---|
| IV-004 | Minor | Mixed constructor styles (primary constructor in services, traditional in controllers). | AzureFastTranscriptionService.cs vs BabbleController.cs | Acceptable — both valid; no action. |

### Frontend Quality ✅

| ID | Severity | Finding | File | Recommendation |
|---|---|---|---|---|
| IV-005 | Minor | File input `accept` attribute is client-side only, not enforcement. | HomePage.tsx | Already correctly backed by server-side validation. No action. |

### Testing Quality ✅

| ID | Severity | Finding | File | Recommendation |
|---|---|---|---|---|
| IV-006 | Minor | No test for TranscribeAsync throwing (Azure failure scenario). Controller has no try/catch — will 500. | BabbleControllerUploadTests.cs | Add test for exception scenario; implement try/catch (see IV-008). |

### Potential Issues ⚠️

| ID | Severity | Finding | File | Recommendation |
|---|---|---|---|---|
| IV-008 | **Major** | No error handling around `_fileTranscriptionService.TranscribeAsync()`. Azure API failures propagate as raw 500 instead of user-friendly 502. Other similar endpoints (GeneratePrompt, GenerateTitle) catch Azure errors and return 502. | BabbleController.cs | Wrap in try/catch returning `StatusCode(502, "An error occurred during transcription. Please try again.")` |
| IV-009 | Minor | No concurrent upload guard beyond `isUploading` button disable. | useFileUpload.ts | Acceptable — button disabled state is sufficient. |
| IV-010 | Minor | TranscriptionClientWrapper doesn't set `Definition` on TranscriptionOptions (auto-detection). | TranscriptionClientWrapper.cs | Acceptable — Azure auto-detects. |

## Validation Command Outputs

| Command | Status |
|---|---|
| `dotnet build PromptBabbler.slnx` | ✅ Passed (0 warnings, 0 errors) |
| `dotnet format --verify-no-changes --severity error` | ✅ Passed (exit 0) |
| `dotnet test --filter TestCategory=Unit` | ✅ 212 passed, 0 failed |
| `pnpm lint` | ✅ Passed (no ESLint errors) |
| `pnpm test -- --run` | ✅ 125 passed (26 files), 0 failed |
| `pnpm run build` | ✅ Passed (pre-existing @protobufjs eval warning only) |
| IDE diagnostics (changed files) | ✅ 0 errors, 0 warnings |

## Missing Work and Deviations

### Undocumented Files

The changes log does not list these two files that were created as part of the implementation:

* `prompt-babbler-service/src/Infrastructure/Services/ITranscriptionClientWrapper.cs` — test-enabling wrapper interface
* `prompt-babbler-service/src/Infrastructure/Services/TranscriptionClientWrapper.cs` — sealed wrapper delegating to SDK

### Plan Deviation: docs/API.md Not Updated

The AGENTS.md checklist states: "Update `docs/API.md` if the public API surface changed." A new `POST /api/babbles/upload` endpoint was added but `docs/API.md` was not updated.

## Follow-Up Recommendations

### Deferred from Scope

1. Update `docs/API.md` with the new `POST /api/babbles/upload` endpoint documentation (per AGENTS.md checklist)

### Discovered During Review

1. **(Major) IV-001**: Add file-extension validation to `UploadAudio` endpoint — cross-check `file.FileName` extension against allowed list (`.mp3`, `.wav`, `.webm`, `.ogg`)
2. **(Major) IV-008**: Add try/catch around `TranscribeAsync` in `BabbleController.UploadAudio` returning 502 on Azure API failure — matches existing pattern in `GeneratePrompt`/`GenerateTitle`
3. **(Minor) IV-002**: Validate `language` parameter length/format at controller boundary
4. **(Minor) IV-003**: Add `[Consumes("multipart/form-data")]` to `UploadAudio`
5. **(Minor) IV-006**: Add unit test for `TranscribeAsync` throwing exception scenario
6. **(Minor)**: Update changes log to include `ITranscriptionClientWrapper.cs` and `TranscriptionClientWrapper.cs`

## Overall Status

**⚠️ Needs Rework** — Two Major findings require fixes before production deployment:

1. Content-type validation bypass (IV-001) — security hardening
2. Missing error handling for Azure API failures (IV-008) — reliability

Both are straightforward 5–10 line fixes. No architectural rework needed.
