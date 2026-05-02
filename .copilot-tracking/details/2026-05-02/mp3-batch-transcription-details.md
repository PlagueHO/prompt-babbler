<!-- markdownlint-disable-file -->
# Implementation Details: MP3 File Upload Batch Transcription

## Context Reference

Sources: .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md, .copilot-tracking/research/subagents/2026-05-02/mp3-upload-codebase-verification.md

## Implementation Phase 1: Backend Domain and Infrastructure

<!-- parallelizable: true -->

### Step 1.1: Add Azure.AI.Speech.Transcription package to Directory.Packages.props

Add the package version entry to the Azure / AI section of Directory.Packages.props.

Files:
* prompt-babbler-service/Directory.Packages.props - Add PackageVersion entry in the Azure/AI ItemGroup

Insert after the existing `Azure.AI.OpenAI` entry:

```xml
<PackageVersion Include="Azure.AI.Speech.Transcription" Version="1.0.0-beta.2" />
```

Also add the package reference to the Infrastructure project:

Files:
* prompt-babbler-service/src/Infrastructure/PromptBabbler.Infrastructure.csproj - Add PackageReference

```xml
<PackageReference Include="Azure.AI.Speech.Transcription" />
```

Success criteria:
* `dotnet restore PromptBabbler.slnx` succeeds
* Package appears in the project dependency graph

Context references:
* prompt-babbler-service/Directory.Packages.props (Lines 1-47) - Existing package structure
* .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md (Lines 278-280) - Package version specification

Dependencies:
* None — first step

### Step 1.2: Create IFileTranscriptionService interface in Domain/Interfaces/

Create a new interface file for file-based (batch) transcription, separate from the existing `IRealtimeTranscriptionService`.

Files:
* prompt-babbler-service/src/Domain/Interfaces/IFileTranscriptionService.cs - NEW

```csharp
namespace PromptBabbler.Domain.Interfaces;

/// <summary>
/// Transcribes audio files to text using batch processing.
/// </summary>
public interface IFileTranscriptionService
{
    /// <summary>
    /// Transcribes the audio content from the provided stream.
    /// </summary>
    /// <param name="audioStream">The audio data stream (MP3, WAV, WebM, OGG).</param>
    /// <param name="language">Optional BCP-47 locale (defaults to "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcribed text.</returns>
    Task<string> TranscribeAsync(
        Stream audioStream,
        string? language = null,
        CancellationToken cancellationToken = default);
}
```

Success criteria:
* Interface follows project conventions (in Domain/Interfaces/, no infrastructure dependencies)
* Method signature supports the Fast Transcription API usage pattern

Context references:
* prompt-babbler-service/src/Domain/Interfaces/ITranscriptionService.cs - Pattern reference for existing transcription interface
* .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md (Lines 153-160) - Interface design

Dependencies:
* None — pure domain definition

### Step 1.3: Create AzureFastTranscriptionService in Infrastructure/Services/

Implement the file transcription service using `Azure.AI.Speech.Transcription.TranscriptionClient`.

Files:
* prompt-babbler-service/src/Infrastructure/Services/AzureFastTranscriptionService.cs - NEW

```csharp
using Azure.AI.Speech.Transcription;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using System.ClientModel;

namespace PromptBabbler.Infrastructure.Services;

/// <summary>
/// Transcribes audio files using Azure Fast Transcription API.
/// </summary>
public sealed class AzureFastTranscriptionService(
    TranscriptionClient client,
    ILogger<AzureFastTranscriptionService> logger) : IFileTranscriptionService
{
    public async Task<string> TranscribeAsync(
        Stream audioStream,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var locale = language ?? "en-US";
        var options = new TranscriptionOptions(audioStream);
        options.Locales.Add(locale);

        logger.LogInformation("Starting fast transcription for locale {Locale}", locale);

        ClientResult<TranscriptionResult> response = await client.TranscribeAsync(options, cancellationToken);
        TranscriptionResult result = response.Value;

        var transcribedText = result.CombinedPhrases.FirstOrDefault()?.Text ?? string.Empty;

        logger.LogInformation("Fast transcription completed: {CharCount} characters", transcribedText.Length);

        return transcribedText;
    }
}
```

Key implementation notes:
* Primary constructor pattern matches project conventions
* `TranscriptionClient` is thread-safe (singleton DI)
* No native dependencies — pure managed .NET (unlike Microsoft.CognitiveServices.Speech)
* Logging follows existing patterns (structured, no PII in log content)

Success criteria:
* Class is sealed (project convention)
* No native dependencies (works with chiseled Docker image if needed)
* Handles the happy path: stream in → text out

Context references:
* prompt-babbler-service/src/Infrastructure/Services/AzureSpeechTranscriptionService.cs - Pattern reference for service structure
* .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md (Lines 162-192) - Full implementation example

Dependencies:
* Step 1.1 (package reference)
* Step 1.2 (interface definition)

### Step 1.4: Register TranscriptionClient and service in DependencyInjection.cs

Register the `TranscriptionClient` as a singleton and bind it to the interface.

Files:
* prompt-babbler-service/src/Infrastructure/DependencyInjection.cs - MODIFIED: add registrations after existing transcription service (after line ~29)

Insert after the `IRealtimeTranscriptionService` registration block:

```csharp
// File transcription (Azure Fast Transcription API)
services.AddSingleton(sp =>
{
    var endpoint = new Uri(aiServicesEndpoint);
    var credential = sp.GetRequiredService<TokenCredential>();
    return new TranscriptionClient(endpoint, credential);
});

services.AddSingleton<IFileTranscriptionService, AzureFastTranscriptionService>();
```

Also add the required `using` statements at the top:

```csharp
using Azure.AI.Speech.Transcription;
using Azure.Core;
```

Note: `TokenCredential` is already injected via `Azure.Identity` (DefaultAzureCredential from the Aspire host). Verify that `TokenCredential` is available in the DI container or add it to the `AddInfrastructure` method parameters if needed.

Success criteria:
* `TranscriptionClient` registered as singleton with endpoint and credential
* `IFileTranscriptionService` resolves to `AzureFastTranscriptionService`
* Existing service registrations remain unchanged

Context references:
* prompt-babbler-service/src/Infrastructure/DependencyInjection.cs (Lines 24-29) - Existing transcription registration pattern
* .copilot-tracking/research/subagents/2026-05-02/mp3-upload-codebase-verification.md (Lines 101-120) - Full DI registration listing

Dependencies:
* Step 1.2 (interface)
* Step 1.3 (implementation)

### Step 1.5: Add upload endpoint to BabbleController

Add the `POST /api/babbles/upload` endpoint accepting multipart/form-data with file validation.

Files:
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs - MODIFIED: add constructor parameter and endpoint

**Constructor change:** Add `IFileTranscriptionService fileTranscriptionService` parameter to the primary constructor.

**New endpoint** — insert after the GenerateTitle endpoint (after line ~330):

```csharp
[HttpPost("upload")]
[RequestSizeLimit(500 * 1024 * 1024)] // 500 MB
public async Task<IActionResult> UploadAudio(
    IFormFile file,
    [FromForm] string? language,
    CancellationToken cancellationToken)
{
    if (file is null || file.Length == 0)
    {
        return BadRequest("No audio file provided.");
    }

    string[] allowedTypes = ["audio/mpeg", "audio/mp3", "audio/wav", "audio/webm", "audio/ogg"];
    if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
    {
        return BadRequest("Unsupported audio format. Supported: MP3, WAV, WebM, OGG.");
    }

    var userId = User.GetUserId();
    await using var stream = file.OpenReadStream();

    var transcribedText = await _fileTranscriptionService.TranscribeAsync(stream, language, cancellationToken);

    if (string.IsNullOrWhiteSpace(transcribedText))
    {
        return BadRequest("Could not transcribe audio. The file may be empty or contain no speech.");
    }

    var babble = new Babble
    {
        Id = Guid.NewGuid().ToString(),
        UserId = userId,
        Title = GenerateTitleFromText(transcribedText),
        Text = transcribedText,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    var created = await _babbleService.CreateAsync(babble, cancellationToken);
    return CreatedAtAction(nameof(GetBabble), new { id = created.Id }, created);
}
```

**Important:** The `GenerateTitle` method at line 296 is an HTTP endpoint (POST {id}/generate-title), NOT a helper. The upload endpoint needs a private helper to generate a title from text:

```csharp
private static string GenerateTitleFromText(string text)
{
    const int maxLength = 50;
    var title = text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "...";
    return title.Replace('\n', ' ').Replace('\r', ' ');
}
```

Update the `UploadAudio` method to call `GenerateTitleFromText(transcribedText)` instead.

Success criteria:
* Endpoint requires authentication (inherits class-level `[Authorize]` + `[RequiredScope]`)
* File validation rejects null/empty files and unsupported MIME types
* 500 MB request size limit is explicitly set
* Returns 201 Created with Location header pointing to GetBabble
* Returns 400 Bad Request for invalid input or empty transcription

Context references:
* prompt-babbler-service/src/Api/Controllers/BabbleController.cs (Lines 91-119) - CreateBabble endpoint pattern
* .copilot-tracking/research/subagents/2026-05-02/mp3-upload-codebase-verification.md (Lines 36-65) - Controller structure and endpoints
* .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md (Lines 194-236) - Upload endpoint example

Dependencies:
* Step 1.2 (interface for constructor injection)
* Step 1.4 (DI registration so service resolves)

### Step 1.6: Validate backend builds and formats correctly

Run build and format verification for the backend.

Validation commands:
* `dotnet build PromptBabbler.slnx` - Full solution build
* `dotnet format PromptBabbler.slnx --verify-no-changes --severity error` - Format verification

## Implementation Phase 2: Frontend Upload Feature

<!-- parallelizable: true -->

### Step 2.1: Add uploadAudioFile function to api-client.ts

Add a new exported function for uploading audio files via multipart/form-data.

Files:
* prompt-babbler-app/src/services/api-client.ts - MODIFIED: add uploadAudioFile export

Add at the end of the file (before any closing comments):

```typescript
export async function uploadAudioFile(
  file: File,
  accessToken?: string,
): Promise<Babble> {
  const base = getApiBaseUrl();
  const formData = new FormData();
  formData.append('file', file);

  const headers: Record<string, string> = {};
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }
  if (currentAccessCode) {
    headers['X-Access-Code'] = currentAccessCode;
  }
  // Do NOT set Content-Type — browser auto-sets multipart boundary

  const res = await fetch(`${base}/api/babbles/upload`, {
    method: 'POST',
    headers,
    body: formData,
  });

  if (!res.ok) {
    const contentType = res.headers.get('content-type') ?? '';
    if (contentType.includes('text/html')) {
      throw new Error('Backend service is unavailable. Please try again later.');
    }
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`Upload failed (${res.status}): ${text}`);
  }

  return res.json() as Promise<Babble>;
}
```

Key implementation notes:
* Does NOT use `fetchJson` because that hardcodes `Content-Type: application/json`
* Uses raw `fetch` with `FormData` — browser auto-sets multipart boundary
* Auth pattern matches existing: Bearer token + X-Access-Code
* Error handling mirrors `fetchJson` pattern (HTML detection for unavailable backend)
* Uses `buildHeaders` would work for auth but NOT for content-type — use inline headers

Success criteria:
* Function exports correctly (named export)
* No explicit Content-Type header (FormData handles this)
* Auth headers included when available
* Error handling matches existing patterns

Context references:
* prompt-babbler-app/src/services/api-client.ts (Lines 53-75) - fetchJson pattern reference
* .copilot-tracking/research/subagents/2026-05-02/mp3-upload-codebase-verification.md (Lines 160-200) - API client analysis
* .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md (Lines 238-266) - Frontend API example

Dependencies:
* None on frontend side — backend endpoint can be developed in parallel

### Step 2.2: Create useFileUpload hook

Create a custom hook for file upload state management.

Files:
* prompt-babbler-app/src/hooks/useFileUpload.ts - NEW

```typescript
import { useCallback, useRef, useState } from 'react';
import { uploadAudioFile } from '@/services/api-client';
import { useAuthToken } from '@/hooks/useAuthToken';
import type { Babble } from '@/types';

export function useFileUpload() {
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const getAuthToken = useAuthToken();
  const getAuthTokenRef = useRef(getAuthToken);
  getAuthTokenRef.current = getAuthToken;

  const upload = useCallback(async (file: File): Promise<Babble> => {
    setIsUploading(true);
    setError(null);
    try {
      const authToken = await getAuthTokenRef.current();
      const babble = await uploadAudioFile(file, authToken);
      return babble;
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Upload failed';
      setError(msg);
      throw err;
    } finally {
      setIsUploading(false);
    }
  }, []);

  return { upload, isUploading, error };
}
```

Key implementation notes:
* Follows existing hook pattern: `useCallback` + loading/error state
* `useRef` for `getAuthToken` avoids stale closure (same pattern as `useTranscription`)
* Returns the created `Babble` object for navigation
* Re-throws error after setting state (caller can handle navigation logic)

Success criteria:
* Named export (project convention)
* Uses `@/` path alias for imports
* Matches existing hook state management patterns
* TypeScript types are correct

Context references:
* prompt-babbler-app/src/hooks/useTranscription.ts - Pattern reference for hook structure
* .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md (Lines 208-236) - Hook example

Dependencies:
* Step 2.1 (uploadAudioFile function)

### Step 2.3: Add upload button and file input to HomePage.tsx

Modify HomePage to include the upload button, hidden file input, and upload handling logic.

Files:
* prompt-babbler-app/src/pages/HomePage.tsx - MODIFIED

**Add imports** (update line 2 and add new imports):

```tsx
// Update lucide-react import to include Upload and Loader2 (keep Plus — used in empty state)
import { Plus, Mic, Upload, Loader2 } from 'lucide-react';

// Add new imports
import { useRef } from 'react';
import { useNavigate } from 'react-router';
import { toast } from 'sonner';
import { useFileUpload } from '@/hooks/useFileUpload';
```

**Add hook usage** inside the component function (after existing hooks):

```tsx
const navigate = useNavigate();
const fileInputRef = useRef<HTMLInputElement>(null);
const { upload, isUploading, error: uploadError } = useFileUpload();

const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
  const file = e.target.files?.[0];
  if (!file) return;

  // Reset the input so the same file can be re-selected
  e.target.value = '';

  try {
    const babble = await upload(file);
    toast.success('Audio transcribed successfully!');
    navigate(`/babble/${babble.id}`);
  } catch {
    // Error is already set in the hook state
    toast.error('Failed to transcribe audio file.');
  }
};
```

**Add upload button** inside the `<div className="flex gap-2">` block (after the existing New Babble button, before the closing `</div>`):

```tsx
<Button
  variant="outline"
  size="default"
  disabled={isUploading}
  onClick={() => fileInputRef.current?.click()}
>
  {isUploading ? (
    <Loader2 className="size-4 animate-spin" />
  ) : (
    <Upload className="size-4" />
  )}
  Upload
</Button>
<input
  ref={fileInputRef}
  type="file"
  accept="audio/mpeg,audio/mp3,audio/wav,audio/webm,audio/ogg"
  className="hidden"
  onChange={handleFileSelect}
/>
```

**Add upload error display** — if `uploadError` is truthy, show an ErrorBanner below the header (same pattern as existing `error` state).

Success criteria:
* Upload button renders alongside "New Babble" button
* File input accepts audio MIME types only
* Loading spinner shows during upload
* Success navigates to babble detail page with toast
* Error shows toast and can display in ErrorBanner
* Hidden file input is not visible

Context references:
* prompt-babbler-app/src/pages/HomePage.tsx (Lines 42-50) - Button container location
* prompt-babbler-app/src/pages/RecordPage.tsx - Navigation + toast pattern reference
* .copilot-tracking/research/2026-05-02/mp3-batch-transcription-research.md (Lines 268-292) - Button example

Dependencies:
* Step 2.2 (useFileUpload hook)

### Step 2.4: Validate frontend builds and lints correctly

Validation commands:
* `pnpm lint` - ESLint check
* `pnpm run build` - TypeScript + Vite build

## Implementation Phase 3: Unit Tests

<!-- parallelizable: false -->

### Step 3.1: Create AzureFastTranscriptionServiceTests

Create unit tests for the transcription service.

Files:
* prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/AzureFastTranscriptionServiceTests.cs - NEW

Test cases:
* `TranscribeAsync_ValidAudioStream_ReturnsTranscribedText` — happy path with mocked TranscriptionClient
* `TranscribeAsync_EmptyResult_ReturnsEmptyString` — when CombinedPhrases is empty
* `TranscribeAsync_NullLanguage_DefaultsToEnUS` — verifies locale default
* `TranscribeAsync_CustomLanguage_UsesProvidedLocale` — verifies locale passthrough

Note: `TranscriptionClient` is from `Azure.AI.Speech.Transcription`. It may need to be wrapped in an abstraction or tested via integration tests if it cannot be mocked with NSubstitute. Check if the class is virtual/interface-based. If not, consider:
* Creating a thin wrapper interface
* Or testing at the integration level with a test double

Success criteria:
* Tests have `[TestCategory("Unit")]` attribute
* Test class is sealed
* Uses MSTest + FluentAssertions + NSubstitute
* Covers happy path and edge cases

Context references:
* prompt-babbler-service/tests/unit/ - Existing test project structure
* .github/copilot-instructions.md - Test naming conventions

Dependencies:
* Phase 1 completion (service implementation exists)

### Step 3.2: Create BabbleController upload endpoint tests

Add unit tests for the upload endpoint validation logic.

Files:
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerUploadTests.cs - NEW (or extend existing BabbleControllerTests)

Test cases:
* `UploadAudio_NullFile_ReturnsBadRequest`
* `UploadAudio_EmptyFile_ReturnsBadRequest`
* `UploadAudio_UnsupportedContentType_ReturnsBadRequest`
* `UploadAudio_ValidMp3_ReturnsCreated`
* `UploadAudio_EmptyTranscription_ReturnsBadRequest`
* `UploadAudio_ValidFile_CreatessBabbleWithGeneratedTitle`

Success criteria:
* Tests have `[TestCategory("Unit")]` attribute
* Test class is sealed
* Validates all input validation paths
* Mocks `IFileTranscriptionService` and `IBabbleService`

Context references:
* prompt-babbler-service/tests/unit/Api.UnitTests/ - Existing controller test patterns
* .github/copilot-instructions.md - Test naming: MethodName_Condition_ExpectedResult

Dependencies:
* Phase 1 completion (controller endpoint exists)

### Step 3.3: Create useFileUpload hook tests

Create frontend tests for the upload hook.

Files:
* prompt-babbler-app/tests/hooks/useFileUpload.test.ts - NEW

Test cases:
* `it('returns initial state with isUploading false and no error')`
* `it('sets isUploading true during upload')`
* `it('returns babble on successful upload')`
* `it('sets error message on upload failure')`
* `it('resets error on new upload attempt')`

Success criteria:
* Uses Vitest + Testing Library
* Mocks `@/services/api-client` module
* Tests observable behavior, not implementation details

Context references:
* prompt-babbler-app/tests/hooks/ - Existing hook test patterns
* .github/copilot-instructions.md - Frontend test naming conventions

Dependencies:
* Phase 2 completion (hook implementation exists)

## Implementation Phase 4: Validation

<!-- parallelizable: false -->

### Step 4.1: Run full project validation

Execute all validation commands for the project:
* `dotnet build PromptBabbler.slnx` (in prompt-babbler-service/)
* `dotnet format PromptBabbler.slnx --verify-no-changes --severity error` (in prompt-babbler-service/)
* `dotnet test --solution PromptBabbler.slnx --filter TestCategory=Unit --configuration Release` (in prompt-babbler-service/)
* `pnpm lint` (in prompt-babbler-app/)
* `pnpm test` (in prompt-babbler-app/)
* `pnpm run build` (in prompt-babbler-app/)

### Step 4.2: Fix minor validation issues

Iterate on lint errors, build warnings, and test failures. Apply fixes directly when corrections are straightforward and isolated.

### Step 4.3: Report blocking issues

When validation failures require changes beyond minor fixes:
* Document the issues and affected files.
* Provide the user with next steps.
* Recommend additional research and planning rather than inline fixes.
* Avoid large-scale refactoring within this phase.

## Dependencies

* Azure.AI.Speech.Transcription v1.0.0-beta.2 NuGet package
* .NET 10 SDK
* pnpm (frontend package manager)
* Existing Azure Cognitive Services endpoint

## Success Criteria

* Backend: `dotnet build` succeeds, `dotnet format --verify-no-changes` passes, all unit tests pass
* Frontend: `pnpm lint` passes, `pnpm run build` succeeds, `pnpm test` passes
* Upload endpoint accessible at POST /api/babbles/upload with proper auth
* File transcription produces text from audio input
* UI renders upload button and handles full upload lifecycle
