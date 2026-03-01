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

        MediaRecorder[MediaRecorder API]
        LocalStorage[(localStorage)]
    end

    subgraph AspireAppHost["Aspire AppHost — Orchestration"]
        subgraph Backend["prompt-babbler-service — .NET 10 Clean Architecture"]
            subgraph ApiLayer["Api Layer — ASP.NET Core"]
                TC[TranscriptionController\nPOST /api/transcribe]
                PC[PromptController\nPOST /api/prompts/generate]
                SC[SettingsController\nGET/PUT /api/settings\nPOST /api/settings/test]
                HC[Health Checks\n/health  /alive]
            end

            subgraph DomainLayer["Domain Layer"]
                ITS[ITranscriptionService]
                IPGS[IPromptGenerationService]
                ISS[ISettingsService]
                LLM[LlmSettings Model]
            end

            subgraph InfraLayer["Infrastructure Layer"]
                AOTS[AzureOpenAi\nTranscriptionService]
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
        STT[Speech-to-Text\nModel]
        LLMGen[GPT-4o / GPT-4o-mini\nPrompt Generation]
    end

    ConfigFile[("~/.prompt-babbler/\nsettings.json")]

    %% Browser internal flows
    Pages --> Hooks
    Hooks --> Services
    useAR --> MediaRecorder
    useLS --> LocalStorage
    useB --> useLS
    useT --> useLS
    LS --> LocalStorage
    API -->|HTTP| ApiLayer

    %% API to Domain
    TC --> ITS
    PC --> IPGS
    SC --> ISS

    %% Domain to Infrastructure
    ITS -.->|implements| AOTS
    IPGS -.->|implements| AOPGS
    ISS -.->|implements| FSS

    %% Infrastructure to External
    AOTS -->|Audio API| STT
    AOPGS -->|Chat Completions\nSSE Streaming| LLMGen
    STT --> AOAI
    LLMGen --> AOAI
    FSS -->|Read/Write JSON| ConfigFile

    %% Aspire orchestration
    ServiceDefaults -.->|configures| Backend

    %% Styling
    classDef external fill:#f9e2af,stroke:#f5c542,color:#000
    classDef storage fill:#a6e3a1,stroke:#40a02b,color:#000
    classDef browser fill:#89b4fa,stroke:#1e66f5,color:#000
    classDef backend fill:#cba6f7,stroke:#8839ef,color:#000

    class AOAI,STT,LLMGen external
    class LocalStorage,ConfigFile storage
    class MediaRecorder browser
```

## Data Flow Diagram

```mermaid
sequenceDiagram
    participant U as User
    participant B as Browser
    participant MR as MediaRecorder API
    participant LS as localStorage
    participant API as .NET API
    participant AOAI as Azure OpenAI

    Note over U,AOAI: Recording & Transcription Flow
    U->>B: Click Record
    B->>MR: Start capture (webm/opus)
    loop Every ~5 seconds
        MR-->>B: Audio chunk (≤25 MB)
        B->>API: POST /api/transcribe
        API->>AOAI: Audio → STT Model
        AOAI-->>API: Transcribed text
        API-->>B: TranscriptionResponse
        B->>LS: Persist interim babble
    end
    U->>B: Click Stop
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
