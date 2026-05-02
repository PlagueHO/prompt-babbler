# Fast Transcription API — Detailed Research

## Research Topics

1. Package details for `Azure.AI.Speech.Transcription`
1. Regional availability for fast transcription
1. Authentication patterns (DefaultAzureCredential vs API keys)
1. Error handling and retry patterns
1. File size limits and constraints
1. ASP.NET Core implementation patterns
1. Comparison with Azure OpenAI Whisper

---

## 1. Package Details: Azure.AI.Speech.Transcription

### NuGet Package

| Property | Value |
|---|---|
| Package name | `Azure.AI.Speech.Transcription` |
| Current version | **1.0.0-beta.2** |
| Status | **Prerelease / Beta** |
| NuGet URL | <https://www.nuget.org/packages/Azure.AI.Speech.Transcription> |
| Source code | <https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/transcription/Azure.AI.Speech.Transcription> |
| API reference | <https://learn.microsoft.com/dotnet/api/azure.ai.speech.transcription> |

### Native Dependencies

**Pure managed .NET** — no native dependencies. This package is part of the Azure SDK for .NET (`Azure.*` family), built on `System.ClientModel` and `Azure.Core`. It does NOT use the `Microsoft.CognitiveServices.Speech` native SDK (which has native Linux/Windows dependencies). The `TranscriptionClient` communicates via REST API (multipart/form-data POST requests).

> **Key insight for prompt-babbler**: Unlike the current `AzureSpeechTranscriptionService` that uses `Microsoft.CognitiveServices.Speech` (which requires glibc, libssl, libasound on Linux), this package has **zero native dependencies** and works with chiseled Docker images.

### Target Frameworks

- **.NET 8.0 SDK or later** (documented prerequisite)
- Standard Azure SDK for .NET package (likely targets `netstandard2.0` or `net8.0`+)

### Dependencies

- `Azure.Core` (HTTP pipeline, retry policies, diagnostics)
- `System.ClientModel` (client model infrastructure)
- `Azure.Identity` (optional, for Entra ID auth)

### Installation

```dotnetcli
dotnet add package Azure.AI.Speech.Transcription --prerelease
dotnet add package Azure.Identity
```

---

## 2. Regional Availability

Fast transcription is supported in the following Azure regions:

| Region | Fast Transcription |
|---|---|
| `australiaeast` | ✅ |
| `brazilsouth` | ✅ |
| `canadacentral` | ✅ |
| `centralindia` | ✅ |
| `eastus` | ✅ |
| `eastus2` | ✅ |
| `francecentral` | ✅ |
| `germanywestcentral` | ✅ |
| `italynorth` | ✅ |
| `japaneast` | ✅ |
| `japanwest` | ✅ |
| `koreacentral` | ✅ |
| `northcentralus` | ✅ |
| `northeurope` | ✅ |
| `southcentralus` | ✅ |
| `southeastasia` | ✅ |
| `swedencentral` | ✅ |
| `uksouth` | ✅ |
| `westeurope` | ✅ |
| `westus` | ✅ |
| `westus2` | ✅ |
| `westus3` | ✅ |

**Regions that do NOT support fast transcription** (only real-time/batch): `canadaeast`, `centralus`, `eastasia`, `norwayeast`, `qatarcentral`, `southafricanorth`, `switzerlandnorth`, `switzerlandwest`, `uaenorth`, `ukwest`, `westcentralus`.

**Source:** <https://learn.microsoft.com/azure/ai-services/speech-service/regions#regions>

---

## 3. Authentication Patterns

### Option 1: Entra ID / DefaultAzureCredential (Recommended)

```csharp
using Azure.Identity;
using Azure.AI.Speech.Transcription;

DefaultAzureCredential credential = new DefaultAzureCredential();
Uri endpoint = new Uri("https://<your-region>.api.cognitive.microsoft.com");
TranscriptionClient client = new TranscriptionClient(endpoint, credential);
```

**Requirements:**

- Assign `Cognitive Services User` role to the identity
- Ensure Speech resource has Entra ID authentication enabled
- Works with managed identities, service principals, Azure CLI, Visual Studio credentials

### Option 2: API Key (Subscription Key)

```csharp
using System.ClientModel;
using Azure.AI.Speech.Transcription;

Uri endpoint = new Uri("https://<your-region>.api.cognitive.microsoft.com/");
ApiKeyCredential credential = new ApiKeyCredential("<your-api-key>");
TranscriptionClient client = new TranscriptionClient(endpoint, credential);
```

### Endpoint URL Format

```text
https://<region>.api.cognitive.microsoft.com
```

Examples:

- `https://eastus.api.cognitive.microsoft.com`
- `https://westeurope.api.cognitive.microsoft.com`

The REST API endpoint for the fast transcription operation:

```text
POST https://<region>.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2025-10-15
```

---

## 4. Error Handling

### Errors from TranscribeAsync

The `TranscribeAsync` method may throw:

| Error Type | HTTP Status | Retryable? | Description |
|---|---|---|---|
| `ClientResultException` (rate limit) | 429 | ✅ | Too many requests per minute |
| `ClientResultException` (server error) | 500, 502, 503, 504 | ✅ | Transient server-side failures |
| `ClientResultException` (bad request) | 400 | ❌ | Invalid audio format, missing parameters |
| `ClientResultException` (unauthorized) | 401 | ❌ | Invalid credentials or role |
| `ClientResultException` (unprocessable) | 422 | ❌ | Unsupported locale, invalid options |

### Recommended Retry Configuration

1. Retry up to **5 times** on transient errors
1. Use **exponential backoff**: 2s, 4s, 8s, 16s, 32s
1. Total backoff time: **62 seconds**

### Retry Logic Categories

**DO retry on:**

- HTTP 429 (rate limit)
- HTTP 500, 502, 503, 504 (server errors)
- Network errors (`ServiceRequestError`, `ServiceResponseError`)
- `ConnectionError`, `TimeoutError`, `OSError`

**Do NOT retry on:**

- HTTP 400 (bad request — fix the input)
- HTTP 401 (unauthorized — fix credentials)
- HTTP 422 (unprocessable entity — fix parameters)
- Other 4xx client errors

### Implementation Notes

- Reset/rewind the audio stream (`stream.Position = 0`) before each retry attempt
- Under heavy rate limiting, the default HTTP read timeout (300s) may be exceeded
- The API may accept a request but time out generating the response — this appears as a network error, not an HTTP error

### Azure.Core Built-in Retry

Since this package uses `Azure.Core`, it has built-in retry policies by default. Configure via `TranscriptionClientOptions`:

```csharp
var options = new TranscriptionClientOptions();
options.Retry.MaxRetries = 5;
options.Retry.Delay = TimeSpan.FromSeconds(2);
options.Retry.MaxDelay = TimeSpan.FromSeconds(32);
options.Retry.Mode = RetryMode.Exponential;
```

---

## 5. File Size Limits and Constraints

### Fast Transcription Quotas (Standard S0)

| Constraint | Limit |
|---|---|
| Maximum audio input file size | **< 500 MB** |
| Maximum audio length | **< 5 hours** per file |
| Maximum audio length (with diarization) | **< 2 hours** per file |
| Maximum requests per minute | **600** (adjustable via support request) |
| Free tier (F0) | **Not available** (S0 only) |

### Supported Audio Formats

WAV, **MP3**, OPUS/OGG, FLAC, WMA, AAC, ALAW in WAV container, MULAW in WAV container, AMR, WebM, SPEEX.

### Supported Locales (Explicit)

de-DE, en-GB, en-IN, en-US, es-ES, es-MX, fr-FR, hi-IN, it-IT, ja-JP, ko-KR, pt-BR, zh-CN.

**Multi-lingual model** (leave locales empty): de-DE, en-AU, en-CA, en-GB, en-IN, en-US, es-ES, es-MX, fr-CA, fr-FR, it-IT, ja-JP, ko-KR, pt-BR, zh-CN.

---

## 6. Implementation Pattern for ASP.NET Core

### Controller Receiving MP3 File Upload (IFormFile)

```csharp
[HttpPost("transcribe")]
[Authorize]
[RequiredScope("access_as_user")]
public async Task<IActionResult> TranscribeAudio(
    IFormFile audioFile,
    CancellationToken cancellationToken)
{
    if (audioFile is null || audioFile.Length == 0)
        return BadRequest("No audio file provided.");

    if (audioFile.Length > 500 * 1024 * 1024) // 500 MB
        return BadRequest("File exceeds maximum size of 500 MB.");

    await using var stream = audioFile.OpenReadStream();
    var result = await _transcriptionService.TranscribeAsync(stream, cancellationToken);

    return Ok(result);
}
```

### Streaming File to TranscriptionClient Without Full Buffering

The `TranscriptionOptions` constructor accepts a `Stream`:

```csharp
public sealed class FastTranscriptionService : IFileTranscriptionService
{
    private readonly TranscriptionClient _client;

    public FastTranscriptionService(TranscriptionClient client)
    {
        _client = client;
    }

    public async Task<string> TranscribeAsync(Stream audioStream, CancellationToken cancellationToken)
    {
        // TranscriptionOptions accepts a Stream directly — no need to buffer to byte[]
        var options = new TranscriptionOptions(audioStream);
        options.Locales.Add("en-US");

        ClientResult<TranscriptionResult> response = await _client.TranscribeAsync(options, cancellationToken);
        TranscriptionResult result = response.Value;

        return result.CombinedPhrases.FirstOrDefault()?.Text ?? string.Empty;
    }
}
```

**Note:** `IFormFile.OpenReadStream()` returns a `Stream` that can be passed directly to `TranscriptionOptions`. The SDK handles the multipart/form-data upload internally. However, ASP.NET Core may buffer `IFormFile` in memory or to disk depending on size. For very large files, consider using streaming request body binding with `[DisableRequestSizeLimit]` and reading from `Request.Body`.

### Service Registration: Singleton

The `TranscriptionClient` is **thread-safe** (documented guarantee) — all instance methods are thread-safe and independent. The recommendation is to **reuse client instances** across threads.

```csharp
// In DI registration:
services.AddSingleton(sp =>
{
    var endpoint = new Uri(configuration["AzureSpeech:Endpoint"]!);
    var credential = new DefaultAzureCredential();
    return new TranscriptionClient(endpoint, credential);
});

services.AddSingleton<IFileTranscriptionService, FastTranscriptionService>();
```

---

## 7. Alternative: Azure OpenAI Whisper

### Package Details

| Property | Azure OpenAI Whisper |
|---|---|
| Package name | `Azure.AI.OpenAI` |
| Current version | **2.x** (GA) |
| Status | **Generally Available** |
| Client class | `AzureOpenAIClient` → `AudioClient` |
| Method | `TranscribeAudioAsync(filePath, options)` |

### Implementation

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;

AzureOpenAIClient openAIClient = new(new Uri(endpoint), new DefaultAzureCredential());
AudioClient audioClient = openAIClient.GetAudioClient("whisper"); // deployment name

var result = await audioClient.TranscribeAudioAsync(audioFilePath);
Console.WriteLine(result.Value.Text);
```

### Comparison Table

| Feature | Fast Transcription API | Azure OpenAI Whisper |
|---|---|---|
| **Package** | `Azure.AI.Speech.Transcription` | `Azure.AI.OpenAI` |
| **Package maturity** | Beta (1.0.0-beta.2) | GA (2.x) |
| **Native dependencies** | None (pure managed) | None (pure managed) |
| **Max file size** | 500 MB | **25 MB** |
| **Max audio duration** | 5 hours | ~limited by 25 MB size |
| **Rate limit** | 600 requests/min | Model deployment-specific |
| **Supported formats** | WAV, MP3, OGG, FLAC, WMA, AAC, AMR, WebM, SPEEX | mp3, mp4, mpeg, mpga, m4a, wav, webm |
| **Diarization** | ✅ Built-in | ❌ Not supported |
| **Language detection** | ✅ Multi-locale | ✅ Auto-detect |
| **Phrase list / custom vocab** | ✅ | ❌ |
| **Enhanced mode (LLM)** | ✅ | N/A (Whisper is the model) |
| **Streaming input** | ✅ (accepts Stream) | File path or BinaryData |
| **API simplicity** | Medium (options object) | Simple (file path + options) |
| **Authentication** | Cognitive Services endpoint | Azure OpenAI endpoint |
| **Pricing** | Speech service pricing (~$1/audio hour STT) | Azure OpenAI pricing (~$0.006/min = $0.36/hr) |
| **Requires model deployment** | No (service-managed) | Yes (deploy Whisper model) |

### Pricing Comparison

| Service | Approximate Cost |
|---|---|
| **Speech Service (STT, Standard)** | ~$1.00 per audio hour |
| **Azure OpenAI Whisper** | ~$0.006 per minute = ~$0.36 per audio hour |
| **Fast Transcription (if same as STT)** | ~$1.00 per audio hour (estimated, check pricing page) |

> **Note:** Azure Speech pricing page lists real-time and batch STT rates. Fast transcription pricing may be the same as standard STT. Check <https://azure.microsoft.com/pricing/details/cognitive-services/speech-services/> for current rates.

### Whisper Models Available

| Model ID | Description | Max File Size |
|---|---|---|
| `whisper` | General-purpose STT | 25 MB |
| `gpt-4o-transcribe` | GPT-4o powered STT | 25 MB |
| `gpt-4o-mini-transcribe` | GPT-4o mini powered STT | 25 MB |

---

## Key Discoveries

1. **`Azure.AI.Speech.Transcription` is pure managed .NET** — no native dependencies, unlike `Microsoft.CognitiveServices.Speech`. This eliminates the Docker chiseled image constraint documented in the repo memory.

1. **Beta status (1.0.0-beta.2)** — The API is in preview. Breaking changes possible before GA. Install with `--prerelease` flag.

1. **MP3 is explicitly supported** — The code samples show `.mp3` files being transcribed directly.

1. **TranscriptionClient is thread-safe** — Register as singleton in DI.

1. **Stream-based API** — `TranscriptionOptions` accepts a `Stream`, so `IFormFile.OpenReadStream()` can be passed directly without buffering the entire file to `byte[]`.

1. **500 MB / 5 hours** — Vastly exceeds the 25 MB Whisper limit. For typical voice recordings (a few minutes), both approaches work, but fast transcription is more future-proof.

1. **600 requests/minute rate limit** — Generous for most apps; adjustable via support request.

1. **Fast transcription is S0-only** — Not available on the free tier.

1. **Azure OpenAI Whisper is GA but has a hard 25 MB limit** — For short recordings only.

1. **The existing project already has `AzureSpeechTranscriptionService` using the native Speech SDK** — The new `Azure.AI.Speech.Transcription` package would be a simpler, dependency-free alternative for file upload scenarios.

---

## Recommendation for prompt-babbler

For the MP3 file upload transcription feature, **Azure Fast Transcription API (`Azure.AI.Speech.Transcription`)** is the recommended choice because:

1. **No native dependencies** — Eliminates Docker image constraints
1. **500 MB file limit** — Handles any reasonable voice recording
1. **Stream-based API** — Clean integration with ASP.NET Core `IFormFile`
1. **Thread-safe singleton** — Simple DI registration
1. **Diarization support** — Future-proof for multi-speaker scenarios
1. **Same Cognitive Services endpoint** — Consistent with existing infrastructure

**Trade-off:** The package is still in beta. If stability is paramount and files are always < 25 MB, Azure OpenAI Whisper (`Azure.AI.OpenAI`) is GA and has simpler code. However, the 25 MB limit is restrictive for longer recordings.

---

## Clarifying Questions

1. **What is the expected maximum audio file size for babble recordings?** If always < 25 MB (roughly < 25 minutes of MP3 at 128kbps), Whisper is a viable simpler option.
1. **Is the project already deploying a Whisper model in Azure OpenAI?** If so, using Whisper avoids provisioning a separate Speech resource.
1. **Is beta package acceptable for the project?** `1.0.0-beta.2` may have breaking changes before GA.

---

## References

- Fast Transcription API quickstart: <https://learn.microsoft.com/azure/ai-services/speech-service/fast-transcription-create>
- SDK README: <https://learn.microsoft.com/dotnet/api/overview/azure/ai.speech.transcription-readme?view=azure-dotnet-preview>
- NuGet package: <https://www.nuget.org/packages/Azure.AI.Speech.Transcription>
- GitHub source: <https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/transcription/Azure.AI.Speech.Transcription>
- Regions: <https://learn.microsoft.com/azure/ai-services/speech-service/regions#regions>
- Quotas/limits: <https://learn.microsoft.com/azure/ai-services/speech-service/speech-services-quotas-and-limits>
- Error handling: <https://learn.microsoft.com/azure/ai-services/speech-service/fast-transcription-create#transcription-error-handling>
- Azure OpenAI Whisper quickstart: <https://learn.microsoft.com/azure/foundry/openai/whisper-quickstart>
- Azure OpenAI audio models: <https://learn.microsoft.com/azure/ai-foundry/openai/concepts/models#audio-models>
