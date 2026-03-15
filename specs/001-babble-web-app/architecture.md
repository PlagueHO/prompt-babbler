# Logical Architecture: Prompt Babbler

**Date**: 2026-02-13 | **Plan**: [plan.md](plan.md)

## Component Diagram

```mermaid
graph TB
    subgraph Browser["Browser (Client)"]
        subgraph ReactApp["prompt-babbler-app — React 19 + TypeScript + Vite"]
            subgraph Pages["Pages"]
                HP[HomePage]
                RP[RecordPage]
                BP[BabblePage]
                TP[TemplatesPage]
                SP[SettingsPage]
            end

            subgraph Hooks["Custom Hooks"]
                useAR[useAudioRecording]
                useTR[useTranscription]
                usePG[usePromptGeneration]
                useB[useBabbles]
                useT[useTemplates]
                useS[useSettings]
                useLS[useLocalStorage]
            end

            subgraph Services["Services"]
                API[api-client.ts]
                TS[transcription-stream.ts]
                LS[local-storage.ts]
                DT[default-templates.ts]
            end

            subgraph UIComponents["UI Components"]
                Recording[recording/]
                Babbles[babbles/]
                Prompts[prompts/]
                Templates[templates/]
                Settings[settings/]
                Layout[layout/]
            end
        end

        MediaRecorder[AudioWorklet\nPCM Capture]
        LocalStorage[(localStorage)]
    end

    subgraph AspireAppHost["Aspire AppHost — Orchestration"]
        subgraph Backend["prompt-babbler-service — .NET 10 Clean Architecture"]
            subgraph ApiLayer["Api Layer — ASP.NET Core"]
                TC[TranscriptionWebSocketController\nWS /api/transcribe/stream]
                PC[PromptController\nPOST /api/prompts/generate]
                SC[SettingsController\nGET/PUT /api/settings\nPOST /api/settings/test]
                HC[Health Checks\n/health  /alive]
            end

            subgraph DomainLayer["Domain Layer"]
                ITS[IRealtimeTranscriptionService]
                IPGS[IPromptGenerationService]
                ISS[ISettingsService]
                LLM[LlmSettings Model]
            end

            subgraph InfraLayer["Infrastructure Layer"]
                ASTS[AzureSpeech\nTranscriptionService]
                AOPGS[AzureOpenAi\nPromptGenerationService]
                FSS[FileSettingsService]
            end
        end

        subgraph ServiceDefaults["ServiceDefaults"]
            OTel[OpenTelemetry]
            HealthChecks[Health Checks]
            ServiceDisc[Service Discovery]
        end
    end

    subgraph ExternalServices["External Services"]
        AOAI[Azure OpenAI]
        SPEECH[Azure AI\nSpeech Service]
        LLMGen[GPT-4o / GPT-4o-mini\nPrompt Generation]
    end

    ConfigFile[("~/.prompt-babbler/\nsettings.json")]

    %% Browser internal flows
    Pages --> Hooks
    Hooks --> Services
    useAR --> MediaRecorder
    useTR --> TS
    useLS --> LocalStorage
    useB --> useLS
    useT --> useLS
    LS --> LocalStorage
    API -->|HTTP| ApiLayer
    TS -->|WebSocket| TC

    %% API to Domain
    TC --> ITS
    PC --> IPGS
    SC --> ISS

    %% Domain to Infrastructure
    ITS -.->|implements| ASTS
    IPGS -.->|implements| AOPGS
    ISS -.->|implements| FSS

    %% Infrastructure to External
    ASTS -->|Speech SDK\nWebSocket| SPEECH
    AOPGS -->|Chat Completions\nSSE Streaming| LLMGen
    LLMGen --> AOAI
    FSS -->|Read/Write JSON| ConfigFile

    %% Aspire orchestration
    ServiceDefaults -.->|configures| Backend

    %% Styling
    classDef external fill:#f9e2af,stroke:#f5c542,color:#000
    classDef storage fill:#a6e3a1,stroke:#40a02b,color:#000
    classDef browser fill:#89b4fa,stroke:#1e66f5,color:#000
    classDef backend fill:#cba6f7,stroke:#8839ef,color:#000

    class AOAI,SPEECH,LLMGen external
    class LocalStorage,ConfigFile storage
    class MediaRecorder browser
```

## Data Flow Diagram

```mermaid
sequenceDiagram
    participant U as User
    participant B as Browser
    participant AW as AudioWorklet
    participant LS as localStorage
    participant API as .NET API (WebSocket)
    participant SPEECH as Azure AI Speech Service
    participant AOAI as Azure OpenAI

    Note over U,AOAI: Recording & Transcription Flow (Real-time WebSocket)
    U->>B: Click Record
    B->>AW: Start AudioWorklet (16kHz/16-bit/mono PCM)
    B->>API: Open WebSocket /api/transcribe/stream
    API->>SPEECH: Start SpeechRecognizer session
    loop Continuous audio frames
        AW-->>B: PCM Int16 buffer
        B->>API: Binary WebSocket frame (PCM)
        API->>SPEECH: PushAudioInputStream.Write()
        SPEECH-->>API: Recognizing (partial) event
        API-->>B: JSON {"text":"...", "isFinal": false}
        B->>B: Update partial transcript preview
    end
    SPEECH-->>API: Recognized (final) event
    API-->>B: JSON {"text":"...", "isFinal": true}
    B->>B: Append to final transcript
    U->>B: Click Stop
    B->>API: Close WebSocket
    B->>LS: Save final Babble

    Note over U,AOAI: Prompt Generation Flow
    U->>B: Select template + Generate
    B->>API: POST /api/prompts/generate (SSE)
    API->>AOAI: Chat completion (streaming)
    loop SSE chunks
        AOAI-->>API: Token chunk
        API-->>B: data: {text: "..."}
    end
    API-->>B: data: [DONE]
    B->>LS: Save GeneratedPrompt in Babble
    U->>B: Copy to clipboard
```

## Layer Dependency Rules

```mermaid
graph LR
    A[Api Layer] --> D[Domain Layer]
    A --> I[Infrastructure Layer]
    I --> D
    O[Orchestration\nAppHost] --> A
    O --> SD[ServiceDefaults]
    A --> SD

    style D fill:#a6e3a1,stroke:#40a02b,color:#000
    style A fill:#89b4fa,stroke:#1e66f5,color:#000
    style I fill:#cba6f7,stroke:#8839ef,color:#000
    style O fill:#f9e2af,stroke:#f5c542,color:#000
    style SD fill:#f5c2e7,stroke:#ea76cb,color:#000
```

- **Domain** has zero external dependencies (pure business models + interfaces)
- **Infrastructure** implements Domain interfaces, depends on external SDKs
- **Api** depends on Domain (contracts) and registers Infrastructure (DI)
- **Orchestration** wires everything together via Aspire AppHost
