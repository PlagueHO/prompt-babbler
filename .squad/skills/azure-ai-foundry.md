# Skill: Azure AI Foundry Integration

## Confidence: medium

## Overview

prompt-babbler uses Azure AI Foundry for two AI capabilities: LLM chat completion (prompt generation) and real-time speech-to-text transcription. Both are provisioned through a single AI Foundry resource and accessed via the Aspire hosting integration.

## Aspire Hosting Integration

The AppHost provisions AI Foundry resources:

```csharp
var foundry = builder.AddAzureAIFoundry("ai-foundry");
var chatDeployment = foundry.AddDeployment(
    name: "chat",
    modelName: builder.Configuration["MicrosoftFoundry:chatModelName"] ?? "gpt-5.3-chat",
    modelVersion: builder.Configuration["MicrosoftFoundry:chatModelVersion"] ?? "2026-03-03",
    tier: "OpenAI")
    .WithProperties(deployment => {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });
```

The API service references both the foundry and deployment:

```csharp
builder.AddProject<Projects.PromptBabbler_Api>("api")
    .WithReference(foundry)
    .WithReference(chatDeployment)
    .WaitFor(chatDeployment);
```

**NuGet packages:**

- `Aspire.Hosting.Azure.AIFoundry` (13.1.2-preview) — AppHost integration
- `Aspire.Azure.AI.OpenAI` (13.1.2-preview) — Client integration
- `Azure.AI.OpenAI` (2.1.0) — Azure OpenAI SDK

## IChatClient Pattern

LLM interactions use `IChatClient` from Microsoft.Extensions.AI:

```csharp
// Registration in Program.cs
builder.AddAzureOpenAIClient("ai-foundry", configureSettings: settings => { ... });
builder.Services.AddSingleton<IChatClient>(sp => {
    var openAiClient = sp.GetRequiredService<AzureOpenAIClient>();
    return openAiClient.GetChatClient("chat").AsIChatClient();
});
```

Key points:

- Use `AsIChatClient()` — NOT `AsChatClient()`
- Streaming: `chatClient.GetStreamingResponseAsync(messages)` returns `ChatResponseUpdate` with `.Text`
- Structured output: Use `ResponseFormat.Json` option for JSON responses
- System prompt building: concatenate user prompt + format instruction + emoji instruction

## Azure Speech SDK (Real-time Transcription)

### Token Exchange (Critical)

`SpeechConfig.FromAuthorizationToken()` does NOT accept raw AAD tokens. You must exchange:

1. Obtain AAD token via `DefaultAzureCredential` with scope `https://cognitiveservices.azure.com/.default`
1. POST to `{aiServicesEndpoint}/sts/v1.0/issueToken` with `Authorization: Bearer {aadToken}`
1. Returned plain-text token is valid for 10 minutes
1. Cache with 1-minute safety margin, refresh with `SemaphoreSlim` thread safety

### Audio Configuration

- Format: PCM 16kHz, 16-bit, mono
- Input: `PushAudioInputStream` for real-time streaming
- Silence timeouts: 3s segmentation, 30s end-of-speech

### Session Lifecycle

1. Create `SpeechConfig` with STS token + region
1. Create `AudioConfig` from `PushAudioInputStream`
1. Start continuous recognition
1. Events: `Recognizing` (partial), `Recognized` (final), `Canceled`, `SessionStarted/Stopped`
1. Results written to unbounded `Channel<TranscriptionEvent>`
1. `CompleteAsync()` triggers `StopContinuousRecognitionAsync()`

### Required RBAC Roles

- `Cognitive Services OpenAI User` — for model calls (Container App managed identity)
- `Cognitive Services OpenAI Contributor` — for deployment management (deploying principal)
- `Cognitive Services Speech User` — for speech recognition

## Model Deployment Configuration

- Model name and version are configurable via `MicrosoftFoundry:chatModelName` and `MicrosoftFoundry:chatModelVersion`
- Default (production): `gpt-5.3-chat` version `2026-03-03`
- Local dev (launchSettings): `gpt-4.1` version `2025-04-14`
- SKU: `GlobalStandard` with capacity 50
- Speech region: from `Speech:Region` or `Azure:Location` environment variable
