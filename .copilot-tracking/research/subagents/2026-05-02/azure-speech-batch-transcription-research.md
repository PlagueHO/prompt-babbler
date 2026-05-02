# Azure Speech-to-Text Batch Transcription Research

## Research Topics

1. Azure Speech SDK options for file-based transcription (MP3)
1. Batch Transcription REST API
1. Speech SDK RecognizeOnceAsync vs ContinuousRecognition
1. Audio format support (MP3 confirmation)
1. Storage requirements for batch transcription
1. C# code samples for speech-to-text from file
1. Comparison of transcription approaches

## Key Discoveries

### 1. Azure Speech SDK Options for File-Based Transcription

There are **four main approaches** for transcribing audio files in Azure:

#### Option A: Speech SDK `RecognizeOnceAsync` (Legacy SDK)

- **Package**: `Microsoft.CognitiveServices.Speech`
- Single-shot recognition; returns after the first utterance
- Maximum ~30 seconds of audio processed per call
- Best for commands/queries, not for longer audio

#### Option B: Speech SDK Continuous Recognition (Legacy SDK)

- **Package**: `Microsoft.CognitiveServices.Speech`
- Uses `StartContinuousRecognitionAsync()` / `StopContinuousRecognitionAsync()`
- Event-driven: subscribe to `Recognizing`, `Recognized`, `Canceled`, `SessionStopped`
- Handles longer audio files in-process
- Requires GStreamer for MP3/compressed audio on Linux/Windows

#### Option C: Batch Transcription REST API

- **REST API**: Speech to text REST API (`/speechtotext/v3.2/transcriptions`)
- Asynchronous; files stored in Azure Blob Storage
- Handles very large volumes and files up to 1 GB
- Best for offline/batch processing of large audio archives

#### Option D: Fast Transcription API (NEW - Recommended)

- **Package**: `Azure.AI.Speech.Transcription` (NuGet, beta)
- Synchronous API, returns results faster than real-time
- Supports local file streams and URLs directly
- **Max file size**: < 500 MB
- **Max audio length**: < 5 hours per file (< 2 hours with diarization)
- Supports MP3 natively
- No Blob Storage required - stream files directly
- Supports Enhanced Mode (LLM-powered) for highest accuracy

**Source**: <https://learn.microsoft.com/azure/ai-services/speech-service/fast-transcription-create>

### 2. Batch Transcription REST API

#### How It Works

1. **Upload audio** to Azure Blob Storage (or provide public URIs)
1. **Submit transcription job** via REST API with audio file locations
1. **Poll for status** or use webhooks for completion notifications
1. **Retrieve results** from storage container

#### Prerequisites

- Microsoft Foundry resource for Speech (Standard S0 tier)
- Azure Blob Storage account (for file storage)
- Audio files accessible via SAS URI, public URI, or Trusted Azure Services mechanism

#### Typical Flow

```text
Upload files to Blob → Create transcription request → Service processes async →
Poll status or receive webhook → Download transcription results
```

#### Key Characteristics

- Processes files concurrently for faster turnaround
- Results stored in Microsoft-managed container or your own Blob container
- Supports webhooks for status notifications
- Can process entire Blob containers at once
- Scheduling: best-effort basis; up to 30 min to start at peak, up to 24 hours to complete

#### Access Methods for Audio

1. **Trusted Azure services** - System assigned managed identity with Storage Blob Data Reader role
1. **SAS URI** - Shared access signature with Read + List permissions
1. **Public URI** - Publicly accessible URL (no auth required)

**Source**: <https://learn.microsoft.com/azure/ai-services/speech-service/batch-transcription>

### 3. RecognizeOnceAsync vs ContinuousRecognition

| Feature | RecognizeOnceAsync | StartContinuousRecognitionAsync |
|---------|-------------------|--------------------------------|
| Duration | Single utterance (~15-30 sec max) | Unlimited (until stopped) |
| Use case | Commands, queries | Long-running audio, files |
| Return pattern | Returns single result | Event-driven (Recognized events) |
| Control | Automatic stop | Manual start/stop |
| Complexity | Simple | More complex (event handling) |

**Official guidance** (from API docs):

> "Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single shot recognition like command or query. For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead."

**Recommendation**: For MP3 file transcription of any meaningful length, use either:

- Continuous recognition (in-process) for real-time streaming needs
- Fast Transcription API (recommended) for simplest file-based transcription

### 4. Audio Format Support

#### MP3 Support Confirmed

**Yes, MP3 is supported** across all Azure Speech transcription approaches:

| API | MP3 Support | Notes |
|-----|-------------|-------|
| Speech SDK (real-time) | Yes | Requires GStreamer on Linux/Windows |
| Fast Transcription API | Yes | Native support, no GStreamer needed |
| Batch Transcription API | Yes | Native support |
| Azure.AI.Speech.Transcription | Yes | Streams MP3 files directly |

#### Full List of Supported Formats (Batch/Fast Transcription)

1. WAV
1. MP3
1. OPUS/OGG
1. FLAC
1. WMA
1. AAC
1. ALAW in WAV container
1. MULAW in WAV container
1. AMR
1. WebM
1. SPEEX

#### Speech SDK Compressed Audio (via GStreamer)

1. MP3
1. OPUS/OGG
1. FLAC
1. ALAW in WAV container
1. MULAW in WAV container
1. ANY for MP4 container or unknown media format

**Source**: <https://learn.microsoft.com/azure/ai-services/speech-service/batch-transcription-audio-data>

#### Azure.AI.Speech.Transcription Client Library

- Supports WAV, MP3, OGG, and more
- Audio must be < 2 hours duration and < 250 MB in size (for the client library)

**Source**: <https://learn.microsoft.com/dotnet/api/overview/azure/ai.speech.transcription-readme?view=azure-dotnet-preview>

### 5. Storage Requirements

#### Batch Transcription REST API

**Yes, requires Azure Blob Storage** (or public URIs):

- Files must be in Azure Blob Storage with SAS URI or Trusted Azure Services access
- Alternatively, files can be at any publicly accessible URL
- Cannot stream local files directly

#### Fast Transcription API (`Azure.AI.Speech.Transcription`)

**No Blob Storage required**:

- Accepts local `FileStream` directly
- Also accepts public URLs via `TranscriptionOptions(Uri audioUrl)`
- Files streamed directly to the service

#### Speech SDK (real-time/continuous)

**No Blob Storage required**:

- Reads from local file or audio stream
- `AudioConfig.FromWavFileInput()` for WAV files
- `AudioInputStream.CreatePullStream()` with compressed format for MP3

### 6. Code Samples

#### Fast Transcription API - Transcribe Local MP3 File (Recommended)

```csharp
using System;
using System.ClientModel;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.Speech.Transcription;
using Azure.Identity;

Uri endpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_SPEECH_ENDPOINT")
    ?? throw new InvalidOperationException("Set the AZURE_SPEECH_ENDPOINT environment variable."));

var credential = new DefaultAzureCredential();
TranscriptionClient client = new TranscriptionClient(endpoint, credential);

string audioFilePath = "path/to/audio.mp3";
using FileStream audioStream = File.OpenRead(audioFilePath);

TranscriptionOptions options = new TranscriptionOptions(audioStream);
options.Locales.Add("en-US");

ClientResult<TranscriptionResult> response = await client.TranscribeAsync(options);

var channelPhrases = response.Value.PhrasesByChannel.First();
Console.WriteLine(channelPhrases.Text);
```

**Package**: `Azure.AI.Speech.Transcription` (prerelease), `Azure.Identity`
**Source**: <https://learn.microsoft.com/azure/ai-services/speech-service/fast-transcription-create>

#### Fast Transcription API - Enhanced Mode (LLM-Powered)

```csharp
using System;
using System.ClientModel;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.Speech.Transcription;
using Azure.Identity;

Uri endpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_SPEECH_ENDPOINT")
    ?? throw new InvalidOperationException("Set the AZURE_SPEECH_ENDPOINT environment variable."));

var credential = new DefaultAzureCredential();
TranscriptionClient client = new TranscriptionClient(endpoint, credential);

string audioFilePath = "<path-to-your-audio-file.wav>";
using FileStream audioStream = File.OpenRead(audioFilePath);

TranscriptionOptions options = new TranscriptionOptions(audioStream)
{
    EnhancedMode = new EnhancedModeProperties
    {
        Task = "transcribe"
    }
};

ClientResult<TranscriptionResult> response = await client.TranscribeAsync(options);

foreach (var combinedPhrase in response.Value.CombinedPhrases)
{
    Console.WriteLine($"Transcription: {combinedPhrase.Text}");
}
```

**Source**: <https://learn.microsoft.com/azure/ai-services/speech-service/llm-speech>

#### Speech SDK - RecognizeOnceAsync from WAV File

```csharp
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

async static Task FromFile(SpeechConfig speechConfig)
{
    using var audioConfig = AudioConfig.FromWavFileInput("PathToFile.wav");
    using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

    var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();
    Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
}
```

**Source**: <https://learn.microsoft.com/azure/ai-services/speech-service/how-to-recognize-speech>

#### Speech SDK - Compressed Audio (MP3) Input

```csharp
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

var speechConfig = SpeechConfig.FromSubscription("YourKey", "YourRegion");

var pullStream = AudioInputStream.CreatePullStream(
    AudioStreamFormat.GetCompressedFormat(AudioStreamContainerFormat.MP3));
var audioConfig = AudioConfig.FromStreamInput(pullStream);

using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
var result = await recognizer.RecognizeOnceAsync();
var text = result.Text;
```

**Note**: Requires GStreamer installed on Linux/Windows.
**Source**: <https://learn.microsoft.com/azure/ai-services/speech-service/how-to-use-codec-compressed-audio-input-streams>

#### Speech SDK - Continuous Recognition from File with Diarization

```csharp
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;

var filepath = "audio.wav";
var speechConfig = SpeechConfig.FromEndpoint(new Uri(endpoint), speechKey);
speechConfig.SpeechRecognitionLanguage = "en-US";

var stopRecognition = new TaskCompletionSource<int>(
    TaskCreationOptions.RunContinuationsAsynchronously);

using (var audioConfig = AudioConfig.FromWavFileInput(filepath))
using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig))
{
    conversationTranscriber.Transcribed += (s, e) =>
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text} Speaker={e.Result.SpeakerId}");
        }
    };

    conversationTranscriber.SessionStopped += (s, e) =>
    {
        stopRecognition.TrySetResult(0);
    };

    await conversationTranscriber.StartTranscribingAsync();
    Task.WaitAny(new[] { stopRecognition.Task });
    await conversationTranscriber.StopTranscribingAsync();
}
```

**Source**: <https://learn.microsoft.com/azure/ai-services/speech-service/get-started-stt-diarization>

### 7. Comparison of Approaches

| Feature | Option A: RecognizeOnceAsync | Option B: SDK Continuous | Option C: Batch REST API | Option D: Fast Transcription API |
|---------|------------------------------|--------------------------|--------------------------|----------------------------------|
| **Max duration** | ~15-30 seconds | ~240 min (4 hrs) | Unlimited (1 GB file) | < 5 hours (< 2 hrs w/diarization) |
| **Max file size** | N/A (stream) | N/A (stream) | 1 GB | 500 MB |
| **Latency** | Real-time | Real-time | Minutes to hours | Faster than real-time |
| **MP3 support** | Yes (GStreamer) | Yes (GStreamer) | Yes (native) | Yes (native) |
| **Requires Blob** | No | No | Yes | No |
| **Complexity** | Low | Medium | High | Low |
| **Processing** | Synchronous | In-process async | Server-side async | Synchronous (single call) |
| **Diarization** | No | Yes | Yes | Yes |
| **NuGet Package** | Microsoft.CognitiveServices.Speech | Microsoft.CognitiveServices.Speech | N/A (REST) | Azure.AI.Speech.Transcription |
| **Native deps** | GStreamer (Linux/Win) | GStreamer (Linux/Win) | None | None |
| **Best for** | Short commands | Real-time streaming | Large archives | File transcription |

### When to Use Each

| Scenario | Recommended Approach |
|----------|---------------------|
| Short voice commands (< 15 sec) | Option A: RecognizeOnceAsync |
| Transcribe a single MP3 file (< 5 hrs) | **Option D: Fast Transcription API** |
| Real-time transcription with streaming | Option B: SDK Continuous |
| Large batch of files in Blob Storage | Option C: Batch REST API |
| Need LLM-enhanced accuracy | Option D: Fast Transcription API (Enhanced Mode) |
| Need speaker diarization | Option B, C, or D |
| Avoid native dependencies (GStreamer) | Option C or D |
| Minimal code complexity | **Option D: Fast Transcription API** |

## Recommendation for Prompt Babbler

Given the project's requirements (transcribing MP3 voice recordings/"babbles"):

**Primary recommendation: Fast Transcription API (`Azure.AI.Speech.Transcription`)**

Reasons:

1. **No Blob Storage required** - stream MP3 files directly from local/memory
1. **Native MP3 support** - no GStreamer dependency (critical given the existing Dockerfile complexity with Speech SDK native libs)
1. **Simple API** - single async call, result returned synchronously
1. **High limits** - up to 5 hours, 500 MB per file
1. **Enhanced Mode** - LLM-powered transcription for best quality
1. **Modern SDK** - follows Azure SDK patterns (DefaultAzureCredential, etc.)
1. **Diarization support** - if multi-speaker babbles are needed

The current project uses `Microsoft.CognitiveServices.Speech` which requires native Linux libraries (as documented in repo memory). The new `Azure.AI.Speech.Transcription` package may reduce or eliminate the native dependency burden, though this needs verification since it's still in beta.

## References

- [Fast Transcription API](https://learn.microsoft.com/azure/ai-services/speech-service/fast-transcription-create)
- [Azure.AI.Speech.Transcription NuGet](https://www.nuget.org/packages/Azure.AI.Speech.Transcription)
- [Azure.AI.Speech.Transcription README](https://learn.microsoft.com/dotnet/api/overview/azure/ai.speech.transcription-readme?view=azure-dotnet-preview)
- [Batch Transcription Overview](https://learn.microsoft.com/azure/ai-services/speech-service/batch-transcription)
- [Create a Batch Transcription](https://learn.microsoft.com/azure/ai-services/speech-service/batch-transcription-create)
- [Locate Audio Files for Batch](https://learn.microsoft.com/azure/ai-services/speech-service/batch-transcription-audio-data)
- [Supported Audio Formats](https://learn.microsoft.com/azure/ai-services/speech-service/batch-transcription-audio-data#supported-input-formats-and-codecs)
- [Compressed Audio Input (GStreamer)](https://learn.microsoft.com/azure/ai-services/speech-service/how-to-use-codec-compressed-audio-input-streams)
- [Speech-to-Text Quotas and Limits](https://learn.microsoft.com/azure/ai-services/speech-service/speech-services-quotas-and-limits)
- [RecognizeOnceAsync API Reference](https://learn.microsoft.com/dotnet/api/microsoft.cognitiveservices.speech.speechrecognizer.recognizeonceasync)
- [LLM Speech (Enhanced Mode)](https://learn.microsoft.com/azure/ai-services/speech-service/llm-speech)
- [GitHub Samples](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/transcription/Azure.AI.Speech.Transcription/samples)
- [Azure OpenAI Whisper Quickstart](https://learn.microsoft.com/azure/foundry/openai/whisper-quickstart)

## Follow-On Questions

1. Is `Azure.AI.Speech.Transcription` package a pure managed .NET library (no native deps)? The beta package needs verification.
1. What is the pricing difference between Fast Transcription API and the legacy Speech SDK real-time transcription?
1. Does Enhanced Mode (LLM Speech) have additional costs beyond standard Speech resource pricing?
1. What regions support the Fast Transcription API?
1. Can the existing `AzureSpeechTranscriptionService` in the project be migrated to use the new `TranscriptionClient` without breaking changes?
