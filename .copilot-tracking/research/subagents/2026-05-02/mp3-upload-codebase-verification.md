# MP3 Upload Batch Transcription — Codebase Verification

## Research Topics

1. BabbleController.cs — endpoints, dependencies, GenerateTitle method, namespace
1. DependencyInjection.cs — registered services, method signature
1. HomePage.tsx — "New Babble" button structure, imports, hooks/state
1. api-client.ts — exports, fetchJson pattern, access code pattern
1. Directory.Packages.props — package versions, XML structure
1. Kestrel / request size configuration in the Api project
1. Domain/Interfaces/ — transcription-related interfaces
1. types/index.ts — Babble type definition

---

## 1. BabbleController.cs

**File:** `prompt-babbler-service/src/Api/Controllers/BabbleController.cs`

### Namespace and Class Declaration (Lines 12–18)

```csharp
namespace PromptBabbler.Api.Controllers;

[ApiController]
[Authorize]
[RequiredScope("access_as_user")]
[Route("api/babbles")]
public sealed class BabbleController : ControllerBase
```

### Injected Dependencies (Lines 20–36)

| Parameter | Type |
|-----------|------|
| `babbleService` | `IBabbleService` |
| `promptGenerationService` | `IPromptGenerationService` |
| `templateService` | `IPromptTemplateService` |
| `generatedPromptService` | `IGeneratedPromptService` |
| `logger` | `ILogger<BabbleController>` |

### Endpoints (by line number)

| Line | HTTP Method | Route | Method Name |
|------|-------------|-------|-------------|
| 40 | `[HttpGet]` | `api/babbles` | `GetBabbles` |
| 79 | `[HttpGet("{id}")]` | `api/babbles/{id}` | `GetBabble` |
| 91 | `[HttpPost]` | `api/babbles` | `CreateBabble` |
| 121 | `[HttpPut("{id}")]` | `api/babbles/{id}` | `UpdateBabble` |
| 149 | `[HttpPatch("{id}/pin")]` | `api/babbles/{id}/pin` | `PinBabble` |
| 168 | `[HttpDelete("{id}")]` | `api/babbles/{id}` | `DeleteBabble` |
| 182 | `[HttpPost("{id}/generate")]` | `api/babbles/{id}/generate` | `GeneratePrompt` (streaming SSE, void return) |
| 296 | `[HttpPost("{id}/generate-title")]` | `api/babbles/{id}/generate-title` | `GenerateTitle` |

### GenerateTitle Method — YES, it exists (Line 296)

```csharp
[HttpPost("{id}/generate-title")]
public async Task<IActionResult> GenerateTitle(
    string id,
    CancellationToken cancellationToken = default)
```

It calls `_promptGenerationService.GenerateTitleAsync(babble.Text, cancellationToken)` and updates the babble title.

### File Length

The file ends at approximately line 415 (after `ToResponse` helper and closing brace).

### Key Insertion Point

A new endpoint (e.g., `POST api/babbles/batch-upload`) would best be inserted **after the `GenerateTitle` method (line ~330)** and before the private validation helpers (line ~335).

---

## 2. DependencyInjection.cs

**File:** `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs`  
**Total lines:** 72

### Method Signature (Lines 12–16)

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    string speechRegion,
    string aiServicesEndpoint)
```

### Registered Services

| Line | Interface | Implementation | Lifetime |
|------|-----------|----------------|----------|
| 18 | `IPromptBuilder` | `PromptBuilder` | Singleton |
| 19 | `IPromptGenerationService` | `AzureOpenAiPromptGenerationService` | Transient |
| 20 | `ITemplateValidationService` | `TemplateValidationService` | Transient |
| 24–29 | `IRealtimeTranscriptionService` | `AzureSpeechTranscriptionService` | Singleton (factory) |
| 33–38 | `IPromptTemplateRepository` | `CosmosPromptTemplateRepository` | Singleton (factory) |
| 39 | `IPromptTemplateService` | `PromptTemplateService` | Singleton |
| 40 | Hosted service | `BuiltInTemplateSeedingService` | HostedService |
| 43–48 | `IBabbleRepository` | `CosmosBabbleRepository` | Singleton (factory) |
| 49 | `IBabbleService` | `BabbleService` | Singleton |
| 52–57 | `IGeneratedPromptRepository` | `CosmosGeneratedPromptRepository` | Singleton (factory) |
| 58 | `IGeneratedPromptService` | `GeneratedPromptService` | Singleton |
| 61–66 | `IUserRepository` | `CosmosUserRepository` | Singleton (factory) |
| 67 | `IUserService` | `UserService` | Singleton |

### Key Finding

No batch transcription service is currently registered. The existing transcription interface is `IRealtimeTranscriptionService` (streaming real-time only). A new `IBatchTranscriptionService` would need to be registered here.

---

## 3. HomePage.tsx

**File:** `prompt-babbler-app/src/pages/HomePage.tsx`  
**Total lines:** 103

### Imports (Lines 1–8)

```tsx
import { Link } from 'react-router';
import { Plus, Mic, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ErrorBanner } from '@/components/ui/error-banner';
import { BabbleBubbles } from '@/components/babbles/BabbleBubbles';
import { BabbleListSection } from '@/components/babbles/BabbleListSection';
import { AuthGuard } from '@/components/layout/AuthGuard';
import { useBabbles } from '@/hooks/useBabbles';
```

### Hooks / State (Lines 11–30)

Uses the `useBabbles()` hook exclusively. Destructures: `bubbleBabbles`, `bubblesLoading`, `listBabbles`, `listLoading`, `loadingMore`, `loadMore`, `search`, `setSearch`, `sortBy`, `setSortBy`, `sortDirection`, `setSortDirection`, `loading`, `error`, `totalBabbles`, `togglePin`, `refresh`.

Derived state: `const showEmpty = !loading && totalBabbles === 0 && !error;`

### "New Babble" Button (Lines 42–50)

```tsx
<div className="flex gap-2">
  <Button asChild>
    <Link to="/record">
      <Mic className="size-4" />
      New Babble
    </Link>
  </Button>
</div>
```

### Key Insertion Point

An "Upload MP3" button should be added inside the `<div className="flex gap-2">` block (line 42), as a sibling to the existing "New Babble" button. Would need a new `Upload` icon import from lucide-react.

---

## 4. api-client.ts

**File:** `prompt-babbler-app/src/services/api-client.ts`  
**Total lines:** ~370

### Module-Level Pattern

- `__API_BASE_URL__` — Vite-injected global for service discovery
- `currentAccessCode` — module-scoped `let` variable, set via `setAccessCode()` / `getAccessCode()`
- `buildHeaders(contentType?, accessToken?)` — constructs `Authorization: Bearer`, `Content-Type`, and `X-Access-Code` headers

### fetchJson Pattern (Lines 53–75)

```typescript
async function fetchJson<T>(
  path: string,
  init?: RequestInit,
  accessToken?: string,
): Promise<T> {
  const base = getApiBaseUrl();
  const res = await fetch(`${base}${path}`, {
    ...init,
    headers: {
      ...buildHeaders('application/json', accessToken),
      ...init?.headers,
    },
  });
  // Error handling: HTML detection → backend unavailable
  // Non-ok status → throw with status + text
  return res.json() as Promise<T>;
}
```

### Exported Functions Summary

| Export | Type | Route |
|--------|------|-------|
| `setAccessCode` | Function | — |
| `getAccessCode` | Function | — |
| `getStatus` | Async | `GET /api/status` |
| `getAccessStatus` | Async | `GET /api/config/access-status` |
| `getTemplates` | Async | `GET /api/templates` |
| `getTemplate` | Async | `GET /api/templates/{id}` |
| `createTemplate` | Async | `POST /api/templates` |
| `updateTemplate` | Async | `PUT /api/templates/{id}` |
| `deleteTemplate` | Async | `DELETE /api/templates/{id}` |
| `generatePrompt` | Async (stream) | `POST /api/babbles/{id}/generate` |
| `generateTitle` | Async | `POST /api/babbles/{id}/generate-title` |
| `getBabbles` | Async | `GET /api/babbles` |
| `getBabble` | Async | `GET /api/babbles/{id}` |
| `createBabble` | Async | `POST /api/babbles` |
| `updateBabble` | Async | `PUT /api/babbles/{id}` |
| `pinBabble` | Async | `PATCH /api/babbles/{id}/pin` |
| `deleteBabble` | Async | `DELETE /api/babbles/{id}` |
| `getGeneratedPrompts` | Async | `GET /api/babbles/{id}/prompts` |
| `createGeneratedPrompt` | Async | `POST /api/babbles/{id}/prompts` |
| `deleteGeneratedPrompt` | Async | `DELETE /api/babbles/{id}/prompts/{id}` |
| `getUserProfile` | Async | `GET /api/user` |
| `updateUserSettings` | Async | `PUT /api/user/settings` |
| `searchBabbles` | Async | `GET /api/babbles/search` |

### Key Finding for File Upload

`fetchJson` always sets `Content-Type: application/json`. A file upload function would need to use `fetch` directly (like `deleteTemplate` / `deleteBabble` do for non-JSON operations) with `multipart/form-data` and **no explicit Content-Type** (let browser set boundary). The `buildHeaders` function can still be used for auth, but `contentType` should be passed as `undefined`.

---

## 5. Directory.Packages.props

**File:** `prompt-babbler-service/Directory.Packages.props`  
**Total lines:** 47

### Structure

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <!-- Aspire -->
    <!-- Azure / AI -->
    <!-- Testing -->
  </ItemGroup>
</Project>
```

### Key Package Versions

| Package | Version |
|---------|---------|
| Aspire.AppHost.Sdk | 13.2.2 |
| Aspire.Hosting.Foundry | 13.2.4-preview.1.26224.4 |
| Azure.AI.OpenAI | 2.1.0 |
| Azure.Identity | 1.21.0 |
| Microsoft.Azure.Cosmos | 3.59.0 |
| Microsoft.CognitiveServices.Speech | 1.49.1 |
| Microsoft.Extensions.AI.OpenAI | 10.5.0 |
| Microsoft.Identity.Web | 4.8.0 |
| MSTest.TestFramework | 4.2.1 |
| FluentAssertions | 8.9.0 |
| NSubstitute | 5.3.0 |

### Key Finding

No `Azure.Storage.Blobs` or any blob/file storage package exists. If MP3 upload stores files temporarily in Azure Blob Storage, a new package reference is needed. However, if files are only processed in-memory and passed directly to Azure Speech batch transcription, no storage package may be needed.

---

## 6. Kestrel / Request Size Configuration

**No Kestrel configuration found anywhere in the Api project:**

- `Program.cs` — no `ConfigureKestrel`, no `RequestSizeLimit`, no `MaxRequestBodySize`
- `appsettings.json` — no Kestrel section
- No `[RequestSizeLimit]` attributes on any controllers

### Key Finding

ASP.NET Core default max request body size is **~28.6 MB** (30,000,000 bytes). For MP3 batch upload (likely 10–50 MB per file, possibly multiple files), the default may need to be increased via:

- `[RequestSizeLimit(bytes)]` attribute on the upload endpoint
- Or `[DisableRequestSizeLimit]` with custom validation
- Or Kestrel `MaxRequestBodySize` in Program.cs

---

## 7. Domain Interfaces — Transcription

**Directory:** `prompt-babbler-service/src/Domain/Interfaces/`

### Files

- `IBabbleRepository.cs`, `IBabbleService.cs`
- `IEmbeddingService.cs`
- `IGeneratedPromptRepository.cs`, `IGeneratedPromptService.cs`
- `IPromptBuilder.cs`, `IPromptGenerationService.cs`
- `IPromptTemplateRepository.cs`, `IPromptTemplateService.cs`
- `ITemplateValidationService.cs`
- **`ITranscriptionService.cs`** — The transcription interface file
- `IUserRepository.cs`, `IUserService.cs`

### ITranscriptionService.cs Contents

**Interface:** `IRealtimeTranscriptionService` — real-time streaming only.

```csharp
public interface IRealtimeTranscriptionService
{
    Task<TranscriptionSession> StartSessionAsync(
        string? language = null,
        CancellationToken cancellationToken = default);
}
```

Supporting types:

- `TranscriptionSession` — sealed class with `WriteAudioAsync`, `CompleteAsync`, `DisposeAsync`, and `Results` (ChannelReader)
- `TranscriptionEvent` — sealed record with `Text`, `IsFinal`, `Offset?`, `Duration?`

### Key Finding

There is **no batch transcription interface**. The existing interface (`IRealtimeTranscriptionService`) is designed for real-time WebSocket streaming with raw PCM audio. A new `IBatchTranscriptionService` interface is needed for file-based transcription (accepts MP3 audio data, returns full transcription text). The `TranscriptionEvent` record could potentially be reused, but a simpler return type (just the full text) is likely more appropriate for batch.

---

## 8. types/index.ts — Babble Type

**File:** `prompt-babbler-app/src/types/index.ts`

```typescript
export interface Babble {
  id: string;
  title: string;
  text: string;
  tags?: string[];
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
}
```

### Key Finding

The `Babble` type has no `source` or `audioUrl` field. If the feature needs to indicate whether a babble was created from voice recording vs. MP3 upload, a field like `source?: 'recording' | 'upload'` could be added — but this may not be necessary if the upload simply creates a standard babble with the transcribed text.

---

## Summary of Key Findings for Feature Planning

1. **GenerateTitle already exists** — line 296 in BabbleController.cs
1. **No Kestrel size limits configured** — default ~28.6 MB applies; explicit limits needed for large MP3s
1. **No batch transcription interface** — only `IRealtimeTranscriptionService` exists for streaming PCM
1. **No blob storage package** — `Azure.Storage.Blobs` not in Directory.Packages.props
1. **fetchJson always sets JSON content type** — file upload needs raw `fetch` with FormData
1. **"New Babble" button is in a `flex gap-2` div** — easy to add an "Upload" sibling button
1. **The Babble type has no source indicator** — upload-created babbles look identical to recorded ones
1. **DependencyInjection.cs takes `speechRegion` and `aiServicesEndpoint`** — batch transcription service would use the same credential/endpoint pattern

---

## Clarifying Questions

None — all research questions are fully answered from the codebase.
