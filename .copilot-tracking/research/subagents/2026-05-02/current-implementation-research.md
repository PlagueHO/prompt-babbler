# Current Transcription Implementation Research

## Status: Complete

## Research Topics

1. Current streaming transcription service implementation
1. Domain interfaces for transcription
1. Domain models (Babble, TranscriptionEvent)
1. API Controllers handling transcription/babble creation
1. Frontend recording components and hooks
1. Frontend home page layout
1. Infrastructure/DI registration
1. Existing storage/upload patterns

---

## 1. Current Streaming Transcription Service

### File: `prompt-babbler-service/src/Infrastructure/Services/AzureSpeechTranscriptionService.cs`

**SDK:** `Microsoft.CognitiveServices.Speech` (Azure Speech SDK / Cognitive Services Speech)

**Class signature:**

```csharp
public sealed class AzureSpeechTranscriptionService(
    string region,
    string aiServicesEndpoint,
    TokenCredential credential,
    ILogger<AzureSpeechTranscriptionService> logger) : IRealtimeTranscriptionService
```

**How it works:**

- Implements `IRealtimeTranscriptionService` (single method: `StartSessionAsync`)
- Uses Azure Speech SDK's **continuous recognition** with a `PushStream` for audio input
- Audio format: **16 kHz, 16-bit, mono PCM** (`AudioStreamFormat.GetWaveFormatPCM`)
- Authentication: Obtains an AAD token via `TokenCredential`, then exchanges it for a short-lived Cognitive Services STS token (10 min validity, cached with 1 min safety margin)
- STS endpoint derived from AI Services endpoint: `{endpoint}/sts/v1.0/issueToken`
- Session flow:
  1. Creates `SpeechConfig` with STS token and region
  1. Creates `AudioInputStream.CreatePushStream(audioFormat)` for live audio
  1. Creates `SpeechRecognizer` with continuous recognition
  1. Wires `Recognizing`, `Recognized`, `Canceled`, `SessionStarted`, `SessionStopped` events
  1. Events write `TranscriptionEvent` records to an unbounded `Channel<TranscriptionEvent>`
  1. Returns a `TranscriptionSession` that exposes:
     - `WriteAudioAsync` — pushes PCM bytes into the push stream
     - `CompleteAsync` — closes the push stream, waits for session stop, stops recognition
     - `Results` — `ChannelReader<TranscriptionEvent>` for consuming events
     - `DisposeAsync` — cleanup

**Key configuration:**

- `Speech_SegmentationSilenceTimeoutMs`: 3000 ms
- `SpeechServiceConnection_EndSilenceTimeoutMs`: 30000 ms (30 seconds)
- Default language: `en-US` (overridable via parameter)

### File: `prompt-babbler-service/src/Infrastructure/Services/AzureOpenAiTranscriptionService.cs`

**Status:** Empty/deprecated stub. Contains only a comment stating it has been replaced by `AzureSpeechTranscriptionService`.

---

## 2. Domain Interfaces

### File: `prompt-babbler-service/src/Domain/Interfaces/ITranscriptionService.cs`

**Interface:**

```csharp
public interface IRealtimeTranscriptionService
{
    Task<TranscriptionSession> StartSessionAsync(
        string? language = null,
        CancellationToken cancellationToken = default);
}
```

**TranscriptionSession class** (same file):

```csharp
public sealed class TranscriptionSession : IAsyncDisposable
{
    public ChannelReader<TranscriptionEvent> Results { get; }
    public Task WriteAudioAsync(ReadOnlyMemory<byte> pcmData, CancellationToken cancellationToken = default);
    public Task CompleteAsync();
    public ValueTask DisposeAsync();
}
```

**TranscriptionEvent record** (same file):

```csharp
public sealed record TranscriptionEvent
{
    public required string Text { get; init; }
    public required bool IsFinal { get; init; }
    public TimeSpan? Offset { get; init; }
    public TimeSpan? Duration { get; init; }
}
```

### File: `prompt-babbler-service/src/Domain/Interfaces/IBabbleService.cs`

```csharp
public interface IBabbleService
{
    Task<(IReadOnlyList<Babble> Items, string? ContinuationToken)> GetByUserAsync(...);
    Task<Babble?> GetByIdAsync(string userId, string babbleId, CancellationToken cancellationToken = default);
    Task<Babble> CreateAsync(Babble babble, CancellationToken cancellationToken = default);
    Task<Babble> UpdateAsync(string userId, Babble babble, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, string babbleId, CancellationToken cancellationToken = default);
    Task<Babble> SetPinAsync(string userId, string babbleId, bool isPinned, CancellationToken cancellationToken = default);
}
```

---

## 3. Domain Models

### File: `prompt-babbler-service/src/Domain/Models/Babble.cs`

```csharp
public sealed record Babble
{
    [JsonPropertyName("id")]       public required string Id { get; init; }
    [JsonPropertyName("userId")]   public required string UserId { get; init; }
    [JsonPropertyName("title")]    public required string Title { get; init; }
    [JsonPropertyName("text")]     public required string Text { get; init; }
    [JsonPropertyName("createdAt")] public required DateTimeOffset CreatedAt { get; init; }
    [JsonPropertyName("tags")]     public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("updatedAt")] public required DateTimeOffset UpdatedAt { get; init; }
    [JsonPropertyName("isPinned")] public bool IsPinned { get; init; }
}
```

### File: `prompt-babbler-service/src/Domain/Models/BabbleSearchResult.cs`

```csharp
public sealed record BabbleSearchResult(Babble Babble, double SimilarityScore);
```

**Key observation:** The `Babble` model stores only text. There is **no audio URL, blob reference, or audio duration** field on the model. Audio is streamed in real-time and discarded after transcription — only the resulting text is persisted.

---

## 4. API Controllers

### TranscriptionWebSocketController

**File:** `prompt-babbler-service/src/Api/Controllers/TranscriptionWebSocketController.cs`

**Route:** `api/transcribe`

**Endpoint:** `GET api/transcribe/stream?language={language}`

- Requires WebSocket upgrade
- Auth: `[Authorize]` + `[RequiredScope("access_as_user")]`
- Flow:
  1. Validates WebSocket request
  1. Accepts WebSocket connection
  1. Starts a pre-buffering task (reads audio frames from WS while session starts)
  1. Calls `transcriptionService.StartSessionAsync(language, cancellationToken)`
  1. Flushes pre-buffered frames into the session
  1. Reader loop: reads binary WS frames → `session.WriteAudioAsync()`
  1. Writer task: reads `session.Results` channel → sends JSON `{text, isFinal}` to WS client
  1. On WS close: calls `session.CompleteAsync()`, waits for writer, closes WS

**Audio format expected:** Raw PCM binary frames (16 kHz, 16-bit, mono) sent as WebSocket binary messages.

### BabbleController

**File:** `prompt-babbler-service/src/Api/Controllers/BabbleController.cs`

**Route:** `api/babbles`

**Key endpoints:**

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/babbles` | List babbles (paged, searchable, sortable) |
| GET | `/api/babbles/{id}` | Get single babble |
| POST | `/api/babbles` | Create babble (JSON body: `{title, text, tags?, isPinned?}`) |
| PUT | `/api/babbles/{id}` | Update babble |

**CreateBabbleRequest** (`prompt-babbler-service/src/Api/Models/Requests/CreateBabbleRequest.cs`):

```csharp
public sealed record CreateBabbleRequest
{
    public required string Title { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    [JsonPropertyName("isPinned")] public bool? IsPinned { get; init; }
}
```

**Key observation:** Babble creation is a simple JSON POST with `title` + `text`. There is **no file upload endpoint**. The transcription happens separately via WebSocket, then the frontend saves the resulting text as a babble.

---

## 5. Frontend Recording Components and Hooks

### useAudioRecording Hook

**File:** `prompt-babbler-app/src/hooks/useAudioRecording.ts`

- Captures microphone audio at **16 kHz, mono** using `AudioWorklet` (`pcm-processor.js`)
- Delivers raw Int16 PCM buffers via `onPcmFrame` callback
- Returns: `{ isRecording, duration, start, stop, analyserRef }`
- `start()`: Gets user media, creates AudioContext at 16 kHz, loads AudioWorklet, connects source
- `stop()`: Closes AudioContext, stops media stream tracks
- Includes waveform `AnalyserNode` for visualization

### useTranscription Hook

**File:** `prompt-babbler-app/src/hooks/useTranscription.ts`

- Manages WebSocket transcription session lifecycle
- Uses `TranscriptionStream` service class
- `connect(language?)`: Acquires auth token, opens WebSocket
- `sendAudio(pcmBuffer)`: Forwards PCM data to WebSocket
- `disconnect()`: Closes WebSocket
- `reset()`: Resets accumulated text
- State: `{ transcribedText, partialText, isConnected, error }`
- Token refresh: Reconnects with fresh token every 55 minutes
- OTEL tracing: Tracks session span, time-to-first-word, WS connect time

### TranscriptionStream Service

**File:** `prompt-babbler-app/src/services/transcription-stream.ts`

- WebSocket client to `GET /api/transcribe/stream`
- Constructs WS URL from `__API_BASE_URL__` (Vite injected) or derives from page location
- Passes `access_token` and `access_code` as query parameters
- Binary frames sent via `ws.send(pcmBuffer)` (ArrayBuffer)
- Receives JSON messages: `{ text: string, isFinal: boolean }`
- Buffers up to 320 frames (~5 seconds) while WS is connecting
- Connection timeout: 10 seconds

### RecordButton Component

**File:** `prompt-babbler-app/src/components/recording/RecordButton.tsx`

- Simple toggle button (Mic icon → Square icon when recording)
- Handles microphone permission denied state
- Props: `{ isRecording, onStart, onStop, disabled }`

### RecordPage

**File:** `prompt-babbler-app/src/pages/RecordPage.tsx`

- Combines `useAudioRecording` and `useTranscription` hooks
- `handleStart`: Calls `connect(language)` and `startRecording()` in parallel
- `handleStop`: Calls `stopRecording()` and `disconnect()`
- `onPcmFrame` callback: Forwards buffer to `sendAudio()`
- Save flow: Creates babble via `createBabble({ title, text: transcribedText })`
- Supports "append mode" (babbleId param): Loads existing babble, concatenates new transcription
- Shows: RecordButton, WaveformVisualizer, TranscriptPreview, title input, save buttons

---

## 6. Frontend Home Page

**File:** `prompt-babbler-app/src/pages/HomePage.tsx`

**Layout:**

- Header row: "Your Babbles" title + "New Babble" button (links to `/record`)
- Error banner (conditional)
- Loading state / Empty state / Content:
  - `BabbleBubbles` — pinned babbles as bubbles
  - `BabbleListSection` — searchable/sortable list with load more

**Key observation:** The header `div` with `flex items-center justify-between` contains the action buttons in a `flex gap-2` container. Currently only one button: "New Babble" (Mic icon, links to `/record`). An upload button would logically be placed alongside this button in the same `gap-2` container.

---

## 7. Infrastructure/DI Registration

**File:** `prompt-babbler-service/src/Infrastructure/DependencyInjection.cs`

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    string speechRegion,
    string aiServicesEndpoint)
```

**Transcription registration:**

```csharp
services.AddSingleton<IRealtimeTranscriptionService>(sp =>
{
    var credential = sp.GetRequiredService<TokenCredential>();
    var logger = sp.GetRequiredService<ILogger<AzureSpeechTranscriptionService>>();
    return new AzureSpeechTranscriptionService(speechRegion, aiServicesEndpoint, credential, logger);
});
```

**Called from:** `prompt-babbler-service/src/Api/Program.cs` (line ~150):

```csharp
builder.Services.AddInfrastructure(
    speechRegion: builder.Configuration["Speech:Region"] ?? string.Empty,
    aiServicesEndpoint: aiServicesEndpoint);
```

**Other services registered:**

- `IPromptBuilder` → `PromptBuilder` (Singleton)
- `IPromptGenerationService` → `AzureOpenAiPromptGenerationService` (Transient)
- `ITemplateValidationService` → `TemplateValidationService` (Transient)
- `IPromptTemplateRepository` → `CosmosPromptTemplateRepository` (Singleton)
- `IPromptTemplateService` → `PromptTemplateService` (Singleton)
- `IBabbleRepository` → `CosmosBabbleRepository` (Singleton)
- `IBabbleService` → `BabbleService` (Singleton)
- `IGeneratedPromptRepository` → `CosmosGeneratedPromptRepository` (Singleton)
- `IGeneratedPromptService` → `GeneratedPromptService` (Singleton)
- `IUserRepository` → `CosmosUserRepository` (Singleton)
- `IUserService` → `UserService` (Singleton)

---

## 8. Existing Storage Patterns

### Azure Blob Storage

**Finding: NO Azure Blob Storage integration exists.**

- No `Azure.Storage.Blobs` package reference in any `.csproj`
- No `BlobClient`, `BlobServiceClient`, or blob-related code anywhere
- No `IFormFile` or multipart form handling in any controller

### File Upload Patterns

**Finding: NO file upload patterns exist.**

- No `IFormFile` usage in any controller
- No `multipart/form-data` handling
- No upload endpoints
- The `api-client.ts` uses only `application/json` content type for all requests
- `CreateBabbleRequest` is a simple JSON body with `title` and `text` fields

### Current Data Flow

```text
[Microphone] → AudioWorklet (16kHz PCM) → WebSocket binary frames
    → TranscriptionWebSocketController → AzureSpeechTranscriptionService
    → Speech SDK continuous recognition → TranscriptionEvent channel
    → WebSocket JSON response → Frontend accumulates text
    → POST /api/babbles { title, text } → Cosmos DB
```

Audio is **never persisted**. It streams through the system and only the transcribed text is saved.

---

## Summary of Key Findings

| Aspect | Current State |
|--------|--------------|
| Transcription approach | Real-time streaming via WebSocket + Azure Speech SDK |
| Audio format | 16 kHz, 16-bit, mono PCM |
| Audio persistence | None — audio is discarded after transcription |
| Babble creation | JSON POST with title + text only |
| File upload | Not implemented anywhere |
| Blob storage | Not integrated |
| Auth pattern | Bearer token + optional access code |
| Frontend recording | AudioWorklet → WebSocket binary frames |
| DI pattern | Extension method on IServiceCollection |

---

## Follow-on Questions (for upload feature implementation)

- Will uploaded audio files need to be persisted in Blob Storage, or only transcribed and discarded?
- What audio formats should be supported for upload (WAV, MP3, M4A, WebM, OGG)?
- Should the upload use the existing real-time `IRealtimeTranscriptionService` interface, or would a new batch `ITranscriptionService` with a `TranscribeAsync(Stream audio, ...)` method be more appropriate?
- Should the Azure OpenAI Whisper API (batch transcription) be used for uploaded files instead of the Speech SDK?
- Maximum file size limit for uploads?
- Should there be a progress indicator during transcription of uploaded files?
