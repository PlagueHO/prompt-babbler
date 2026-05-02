<!-- markdownlint-disable-file -->
# Implementation Details: MP3 Batch Transcription — Review Rework

## Context Reference

Sources: .copilot-tracking/reviews/2026-05-02/mp3-batch-transcription-plan-review.md

## Implementation Phase 1: Controller Security and Error Handling

### Step 1.1: Add file-extension validation (IV-001)

Add file-extension validation alongside the existing content-type check in `UploadAudio`. The content-type header is client-provided and trivially spoofable; validating the file extension provides defense-in-depth.

Files:
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Modify `UploadAudio` method

Add after the existing content-type validation block (after line ~349):

```csharp
string[] allowedExtensions = [".mp3", ".wav", ".webm", ".ogg"];
var extension = Path.GetExtension(file.FileName);
if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
{
    return BadRequest("Unsupported file extension. Supported: .mp3, .wav, .webm, .ogg.");
}
```

Success criteria:
* Files with mismatched extension (e.g., `.exe` with `audio/mpeg` content-type) are rejected
* Files with valid extensions continue to be accepted

### Step 1.2: Add language parameter validation (IV-002)

Validate the `language` form parameter at the controller boundary. The parameter is passed to an external Azure API — reject excessively long or malformed values early.

Files:
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Modify `UploadAudio` method

Add after the file-extension validation, before `var userId = ...`:

```csharp
if (language is not null && (language.Length > 20 || !System.Text.RegularExpressions.Regex.IsMatch(language, @"^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{1,8})*$")))
{
    return BadRequest("Invalid language code. Provide a valid BCP-47 language tag (e.g., 'en-US').");
}
```

Note: Use a compiled/generated regex if the project has a pattern for it, otherwise inline is acceptable for a single-use validation.

Success criteria:
* `null` language passes (optional parameter)
* Valid BCP-47 codes like `en-US`, `fr`, `zh-Hans` pass
* Strings longer than 20 characters or with invalid characters are rejected

### Step 1.3: Add Consumes attribute (IV-003)

Add `[Consumes("multipart/form-data")]` attribute to the `UploadAudio` method to explicitly restrict content negotiation.

Files:
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Add attribute to `UploadAudio` method

Add the attribute before the existing `[HttpPost("upload")]`:

```csharp
[Consumes("multipart/form-data")]
```

Success criteria:
* Requests with non-multipart content types are rejected by the framework before reaching the action

### Step 1.4: Add try/catch around TranscribeAsync (IV-008)

Wrap the `_fileTranscriptionService.TranscribeAsync()` call in a try/catch that returns 502 on Azure API failure. This matches the existing error handling pattern in `GeneratePrompt` and `GenerateTitle`.

Files:
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs — Modify `UploadAudio` method

Wrap the transcription call:

```csharp
string transcribedText;
try
{
    transcribedText = await _fileTranscriptionService.TranscribeAsync(stream, language, cancellationToken);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    _logger.LogError(ex, "File transcription failed for uploaded audio");
    return StatusCode(502, new ProblemDetails
    {
        Title = "Transcription Service Error",
        Status = 502,
        Detail = "An error occurred during transcription. Please try again.",
    });
}
```

Pattern reference: See `GenerateTitle` method at line ~321 for the identical pattern.

Success criteria:
* Azure API failures return 502 with ProblemDetails instead of raw 500
* `OperationCanceledException` is not caught (propagates for proper cancellation handling)
* Error is logged with context

### Step 1.5: Validate backend builds and formats

Run build and format verification.

Commands:
* `dotnet build PromptBabbler.slnx` in prompt-babbler-service/
* `dotnet format PromptBabbler.slnx --verify-no-changes` in prompt-babbler-service/

## Implementation Phase 2: Unit Tests

### Step 2.1: Add test for TranscribeAsync throwing exception (IV-006)

Add a unit test verifying that when `TranscribeAsync` throws, the controller returns 502.

Files:
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerUploadTests.cs — Add test method

```csharp
[TestMethod]
public async Task UploadAudio_TranscriptionServiceThrows_Returns502()
{
    var file = CreateFormFile(contentType: "audio/mpeg", fileName: "test.mp3");

    _fileTranscriptionService
        .TranscribeAsync(Arg.Any<Stream>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
        .ThrowsAsync(new InvalidOperationException("Azure service unavailable"));

    var result = await _controller.UploadAudio(file, null, CancellationToken.None);

    var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
    objectResult.StatusCode.Should().Be(502);
}
```

Success criteria:
* Test passes confirming 502 is returned on transcription failure

### Step 2.2: Add test for invalid file extension

Add a test verifying that files with valid content-type but invalid extension are rejected.

Files:
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerUploadTests.cs — Add test method

```csharp
[TestMethod]
public async Task UploadAudio_ValidContentTypeInvalidExtension_ReturnsBadRequest()
{
    var file = CreateFormFile(contentType: "audio/mpeg", fileName: "malicious.exe");

    var result = await _controller.UploadAudio(file, null, CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
}
```

Success criteria:
* Test passes confirming extension validation works independently of content-type

### Step 2.3: Add test for invalid language parameter

Add a test verifying that excessively long or malformed language parameters are rejected.

Files:
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerUploadTests.cs — Add test method

```csharp
[TestMethod]
public async Task UploadAudio_InvalidLanguageParameter_ReturnsBadRequest()
{
    var file = CreateFormFile(contentType: "audio/mpeg", fileName: "test.mp3");

    var result = await _controller.UploadAudio(file, "invalid-language-code-that-is-way-too-long", CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
}
```

Success criteria:
* Test passes confirming language validation rejects invalid values

### Step 2.4: Run all unit tests

Commands:
* `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit` in prompt-babbler-service/

## Implementation Phase 3: Documentation

### Step 3.1: Update docs/API.md with POST /api/babbles/upload endpoint

Add documentation for the upload endpoint in the Babbles section of API.md, after the `DELETE /api/babbles/{id}` section and before the `POST /api/babbles/{id}/generate` section.

Files:
* docs/API.md — Add new endpoint documentation

Insert the following after the `DELETE /api/babbles/{id}` section:

```markdown
#### `POST /api/babbles/upload`

Uploads an audio file for batch transcription via Azure Fast Transcription API. Creates a babble from the transcribed text with an auto-generated title.

**Content-Type**: `multipart/form-data`

**Form Fields**

| Field | Type | Required | Constraints |
|---|---|---|---|
| `file` | `file` | Yes | Audio file (MP3, WAV, WebM, OGG). Max 500 MB. |
| `language` | `string` | No | BCP-47 language code (e.g., `en-US`). Max 20 characters. |

**Response** `201 Created` — the created babble object with a `Location` header.

```json
{
  "id": "abc-123",
  "title": "Transcribed text from the uploaded...",
  "text": "Transcribed text from the uploaded audio file.",
  "createdAt": "2025-01-15T10:30:00.0000000+00:00",
  "updatedAt": "2025-01-15T10:30:00.0000000+00:00"
}
```

| Status | Condition |
|---|---|
| `400 Bad Request` | No file provided, unsupported format, invalid extension, invalid language code, or empty transcription. |
| `502 Bad Gateway` | Azure transcription service failure. |
```

### Step 3.2: Update changes log with missing wrapper files

Add the two missing wrapper files to the original changes log.

Files:
* .copilot-tracking/changes/2026-05-02/mp3-batch-transcription-changes.md — Add missing entries to Added section

Add these entries to the Added section:
* prompt-babbler-service/src/Infrastructure/Services/ITranscriptionClientWrapper.cs — Wrapper interface for TranscriptionClient to enable unit testing
* prompt-babbler-service/src/Infrastructure/Services/TranscriptionClientWrapper.cs — Sealed wrapper delegating to Azure SDK TranscriptionClient
