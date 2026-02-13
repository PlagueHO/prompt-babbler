# Data Model: Prompt Babbler — 001-babble-web-app

**Date**: 2026-02-12 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Overview

V1 uses a split-storage model:

- **Browser localStorage**: Babbles, Prompt Templates, Generated Prompts (last per babble)
- **Backend config file** (`~/.prompt-babbler/settings.json`): LLM Settings

All localStorage entities use JSON serialization. Entity IDs are UUID v4, generated client-side using `crypto.randomUUID()`. Audio is captured as `audio/webm;codecs=opus` (max 25 MB per chunk). Interim transcription data is persisted to localStorage after every successfully transcribed chunk (~5 seconds). The app warns when localStorage usage reaches 80% of quota.

## Entities

### Babble

A captured stream-of-consciousness speech transcription.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | `string` (UUID) | Yes | Unique identifier, generated client-side |
| `title` | `string` | Yes | Display name. Auto-generated from first ~50 chars of text, user-editable |
| `text` | `string` | Yes | Full transcribed text content |
| `createdAt` | `string` (ISO 8601) | Yes | Creation timestamp |
| `updatedAt` | `string` (ISO 8601) | Yes | Last modification timestamp |
| `lastGeneratedPrompt` | `GeneratedPrompt \| null` | No | Most recently generated prompt for this babble (V1 stores only one) |

**Validation rules**:

- `title`: 1–200 characters, trimmed
- `text`: 1–500,000 characters (supports 30+ min transcriptions at ~150 wpm)
- `createdAt`, `updatedAt`: Valid ISO 8601 datetime strings

**State transitions**:

- `recording` → User clicks Record, interim text is captured
- `draft` → Recording stopped, text saved. Default state.
- `generating` → Prompt generation in progress (transient, not persisted)

**localStorage key**: `prompt-babbler:babbles` → `Babble[]`

---

### PromptTemplate

A reusable template defining how babble text is transformed into a structured prompt.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | `string` (UUID) | Yes | Unique identifier |
| `name` | `string` | Yes | Display name (e.g., "GitHub Copilot Prompt") |
| `description` | `string` | Yes | Brief description of the template's purpose |
| `systemPrompt` | `string` | Yes | System prompt text sent to the LLM alongside babble text |
| `isBuiltIn` | `boolean` | Yes | `true` for default templates (cannot be deleted), `false` for custom |
| `createdAt` | `string` (ISO 8601) | Yes | Creation timestamp |
| `updatedAt` | `string` (ISO 8601) | Yes | Last modification timestamp |

**Validation rules**:

- `name`: 1–100 characters, trimmed, unique across all templates
- `description`: 0–500 characters
- `systemPrompt`: 1–10,000 characters
- Built-in templates: `isBuiltIn = true`, deletion blocked in UI

**localStorage key**: `prompt-babbler:templates` → `PromptTemplate[]`

**Built-in templates** (seeded on first load if key is empty):

1. **GitHub Copilot Prompt**
   - `name`: "GitHub Copilot Prompt"
   - `description`: "Transform your thoughts into a well-structured prompt for GitHub Copilot Chat"
   - `systemPrompt`: Instructions for converting stream-of-consciousness into a clear, actionable GitHub Copilot prompt with context, task description, and constraints.

1. **General Assistant Prompt**
   - `name`: "General Assistant Prompt"
   - `description`: "Transform your thoughts into a clear prompt for a general AI assistant"
   - `systemPrompt`: Instructions for converting stream-of-consciousness into a structured prompt with background, request, and desired output format.

---

### GeneratedPrompt

The output of combining a babble with a template via the LLM.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | `string` (UUID) | Yes | Unique identifier |
| `babbleId` | `string` (UUID) | Yes | Reference to source Babble |
| `templateId` | `string` (UUID) | Yes | Reference to PromptTemplate used |
| `templateName` | `string` | Yes | Snapshot of template name at generation time |
| `promptText` | `string` | Yes | The generated prompt text |
| `generatedAt` | `string` (ISO 8601) | Yes | Generation timestamp |

**Validation rules**:

- `promptText`: 1–100,000 characters
- `babbleId`: Must reference an existing Babble
- `templateId`: Must reference an existing PromptTemplate

**Storage**: Embedded in the parent Babble's `lastGeneratedPrompt` field. V1 stores only the most recent prompt per babble. In V2 (prompt history), this becomes a separate collection.

---

### LlmSettings

The user's Azure OpenAI configuration. Stored server-side only. Covers both LLM (prompt generation) and STT (Whisper transcription) since both use the same Azure OpenAI resource.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `endpoint` | `string` (URL) | Yes | Azure OpenAI or Azure AI Foundry endpoint URL |
| `apiKey` | `string` | Yes | API key for authentication. Never sent to frontend in full. |
| `deploymentName` | `string` | Yes | Model deployment name for LLM/prompt generation (e.g., "gpt-4o-mini") |
| `whisperDeploymentName` | `string` | Yes | Model deployment name for Whisper STT (e.g., "whisper") |

**Validation rules**:

- `endpoint`: Valid HTTPS URL matching `https://*.openai.azure.com/*` or `https://*.services.ai.azure.com/*` or custom endpoint
- `apiKey`: Non-empty string, 32+ characters
- `deploymentName`: 1–64 characters, alphanumeric + hyphens
- `whisperDeploymentName`: 1–64 characters, alphanumeric + hyphens

**Storage**: Backend config file at `~/.prompt-babbler/settings.json`

**File format**:

```json
{
  "endpoint": "https://my-resource.openai.azure.com/",
  "apiKey": "sk-...",
  "deploymentName": "gpt-4o-mini",
  "whisperDeploymentName": "whisper"
}
```

**Frontend representation** (returned by GET /api/settings):

```json
{
  "endpoint": "https://my-resource.openai.azure.com/",
  "apiKeyHint": "...a1b2",
  "deploymentName": "gpt-4o-mini",
  "whisperDeploymentName": "whisper",
  "isConfigured": true
}
```

The `apiKeyHint` field shows only the last 4 characters. The full `apiKey` is never included in GET responses.

---

## Relationships

```text
┌─────────────┐       ┌──────────────────┐
│   Babble    │       │ PromptTemplate   │
│             │       │                  │
│  id         │       │  id              │
│  title      │       │  name            │
│  text       │       │  description     │
│  createdAt  │       │  systemPrompt    │
│  updatedAt  │       │  isBuiltIn       │
│             │       │  createdAt       │
│  lastGenerated      │  updatedAt       │
│  Prompt ────┼──┐    │                  │
└─────────────┘  │    └──────────────────┘
                 │           ▲
                 ▼           │
         ┌───────────────────┤
         │ GeneratedPrompt   │
         │                   │
         │  id               │
         │  babbleId ────────┼──► Babble
         │  templateId ──────┘
         │  templateName
         │  promptText
         │  generatedAt
         └───────────────────┘

┌──────────────────────────┐
│ LlmSettings              │  (Server-side only)
│                          │
│  endpoint                │
│  apiKey                  │
│  deploymentName          │
│  whisperDeploymentName   │
└──────────────────────────┘
```

- **Babble 1:0..1 GeneratedPrompt** (V1: stores only the last generated prompt per babble)
- **GeneratedPrompt N:1 PromptTemplate** (each prompt references the template used)
- **LlmSettings** is a singleton — one configuration for the whole application

---

## localStorage Schema

**Keys**:

| Key | Type | Description |
|-----|------|-------------|
| `prompt-babbler:babbles` | `Babble[]` | All saved babbles |
| `prompt-babbler:templates` | `PromptTemplate[]` | All templates (built-in + custom) |
| `prompt-babbler:settings:speechLang` | `string` | Whisper transcription language code (ISO-639-1, e.g., "en"). Empty = auto-detect. |

**Size management**: Estimated storage per babble ≈ 5-50 KB (mostly text). localStorage limit is typically 5-10 MB. At 50 KB average, ~100-200 babbles fit comfortably. The app warns when usage exceeds 80% of the quota (FR-029). At 100% capacity, the app refuses to create new babbles and displays guidance suggesting the user delete old babbles.

---

## TypeScript Type Definitions

```typescript
// -- Babble --
interface Babble {
  id: string;
  title: string;
  text: string;
  createdAt: string;
  updatedAt: string;
  lastGeneratedPrompt: GeneratedPrompt | null;
}

// -- PromptTemplate --
interface PromptTemplate {
  id: string;
  name: string;
  description: string;
  systemPrompt: string;
  isBuiltIn: boolean;
  createdAt: string;
  updatedAt: string;
}

// -- GeneratedPrompt --
interface GeneratedPrompt {
  id: string;
  babbleId: string;
  templateId: string;
  templateName: string;
  promptText: string;
  generatedAt: string;
}

// -- LLM Settings (frontend view — from GET /api/settings) --
interface LlmSettingsView {
  endpoint: string;
  apiKeyHint: string;       // Last 4 chars only
  deploymentName: string;
  whisperDeploymentName: string;
  isConfigured: boolean;
}

// -- LLM Settings (save request — for PUT /api/settings) --
interface LlmSettingsSaveRequest {
  endpoint: string;
  apiKey: string;
  deploymentName: string;
  whisperDeploymentName: string;
}
```

---

## C# Models (Backend Domain)

```csharp
// Domain/Models/LlmSettings.cs
namespace PromptBabbler.Domain.Models;

public sealed record LlmSettings
{
    public required string Endpoint { get; init; }
    public required string ApiKey { get; init; }
    public required string DeploymentName { get; init; }
    public required string WhisperDeploymentName { get; init; }
}
```

```csharp
// Api/Models/Requests/GeneratePromptRequest.cs
namespace PromptBabbler.Api.Models.Requests;

public sealed record GeneratePromptRequest
{
    public required string BabbleText { get; init; }
    public required string SystemPrompt { get; init; }
}
```

```csharp
// Api/Models/Responses/LlmSettingsResponse.cs
namespace PromptBabbler.Api.Models.Responses;

public sealed record LlmSettingsResponse
{
    public required string Endpoint { get; init; }
    public required string ApiKeyHint { get; init; }
    public required string DeploymentName { get; init; }
    public required string WhisperDeploymentName { get; init; }
    public required bool IsConfigured { get; init; }
}
```
