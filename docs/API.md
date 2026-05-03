# Prompt Babbler API Reference

The Prompt Babbler service exposes a REST (and WebSocket) API for babble management, generated prompt storage, prompt generation, prompt template management, real-time audio transcription, and health status.

Base path: `/api`

## Authentication

All API endpoints except `GET /api/status` require authentication via **JWT Bearer tokens** issued by Microsoft Entra ID.

**Setup:**

- The API validates tokens against the `AzureAd` configuration section (`Instance`, `TenantId`, `ClientId`, `Audience`)
- Required scope: `access_as_user` (from `api://prompt-babbler-api/access_as_user`)
- The SPA acquires tokens via MSAL.js (`@azure/msal-browser`) using Authorization Code with PKCE
- WebSocket endpoints accept tokens via the `?access_token={token}` query parameter

**Headers:**

```http
Authorization: Bearer {access_token}
```

**Error Responses:**

- `401 Unauthorized` — missing or invalid token
- `403 Forbidden` — valid token but insufficient scope

## Endpoints

### Status

#### `GET /api/status`

Returns the current health status of the API.

**Response** `200 OK`

```json
{
  "status": "ok"
}
```

---

---

### Prompt Templates

Templates define reusable system prompts. Built-in templates cannot be modified or deleted.

#### `GET /api/templates`

Returns all templates for the current user (including built-in templates).

**Query Parameters**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `forceRefresh` | `boolean` | `false` | Bypass the template cache. |

**Response** `200 OK`

```json
[
  {
    "id": "abc-123",
    "name": "Code Review",
    "description": "Generates code review prompts.",
    "systemPrompt": "You are a senior code reviewer...",
    "isBuiltIn": true,
    "createdAt": "2025-01-01T00:00:00.0000000+00:00",
    "updatedAt": "2025-01-01T00:00:00.0000000+00:00"
  }
]
```

#### `GET /api/templates/{id}`

Returns a single template by ID.

**Response** `200 OK` — template object (same shape as above).

| Status | Condition |
|---|---|
| `404 Not Found` | Template does not exist. |

#### `POST /api/templates`

Creates a new user template.

**Request Body**

| Field | Type | Required | Constraints |
|---|---|---|---|
| `name` | `string` | Yes | 1–100 characters |
| `description` | `string` | Yes | 1–500 characters |
| `systemPrompt` | `string` | Yes | 1–10 000 characters |

```json
{
  "name": "Bug Report",
  "description": "Generates bug report prompts.",
  "systemPrompt": "You are a QA engineer..."
}
```

**Response** `201 Created` — the created template object with a `Location` header.

| Status | Condition |
|---|---|
| `400 Bad Request` | Validation failure (see constraints above). |

#### `PUT /api/templates/{id}`

Updates an existing user template. Built-in templates cannot be updated.

**Request Body** — same schema as `POST /api/templates`.

**Response** `200 OK` — the updated template object.

| Status | Condition |
|---|---|
| `400 Bad Request` | Validation failure. |
| `403 Forbidden` | Attempted to modify a built-in template. |
| `404 Not Found` | Template does not exist. |

#### `DELETE /api/templates/{id}`

Deletes a user template. Built-in templates cannot be deleted.

**Response** `204 No Content`

| Status | Condition |
|---|---|
| `403 Forbidden` | Attempted to delete a built-in template. |
| `404 Not Found` | Template does not exist. |

---

### Babbles

Babbles store transcribed speech text. Each babble belongs to the authenticated user and can have one or more generated prompts.

All babble endpoints require authentication. Responses are scoped to the current user — a user can only access their own babbles.

#### `GET /api/babbles`

Returns a paginated list of babbles for the current user, ordered by creation date (newest first).

**Query Parameters**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `continuationToken` | `string` | `null` | Opaque token for fetching the next page. |
| `pageSize` | `integer` | `20` | Number of items per page (1–100). |

**Response** `200 OK`

```json
{
  "items": [
    {
      "id": "abc-123",
      "title": "Sort function request",
      "text": "I want a function that sorts a list of numbers...",
      "createdAt": "2025-01-15T10:30:00.0000000+00:00",
      "updatedAt": "2025-01-15T10:30:00.0000000+00:00"
    }
  ],
  "continuationToken": "eyJjb250..."
}
```

When `continuationToken` is `null`, there are no more pages.

#### `GET /api/babbles/{id}`

Returns a single babble by ID.

**Response** `200 OK` — babble object (same shape as items above).

| Status | Condition |
|---|---|
| `404 Not Found` | Babble does not exist or does not belong to the current user. |

#### `POST /api/babbles`

Creates a new babble.

**Request Body**

| Field | Type | Required | Constraints |
|---|---|---|---|
| `title` | `string` | Yes | 1–200 characters |
| `text` | `string` | Yes | 1–50 000 characters |

```json
{
  "title": "Sort function request",
  "text": "I want a function that sorts a list of numbers..."
}
```

**Response** `201 Created` — the created babble object with a `Location` header.

| Status | Condition |
|---|---|
| `400 Bad Request` | Validation failure (see constraints above). |

#### `PUT /api/babbles/{id}`

Updates an existing babble.

**Request Body** — same schema as `POST /api/babbles`.

**Response** `200 OK` — the updated babble object.

| Status | Condition |
|---|---|
| `400 Bad Request` | Validation failure. |
| `404 Not Found` | Babble does not exist or does not belong to the current user. |

#### `DELETE /api/babbles/{id}`

Deletes a babble and all of its generated prompts (cascade delete).

**Response** `204 No Content`

| Status | Condition |
|---|---|
| `404 Not Found` | Babble does not exist or does not belong to the current user. |

#### `POST /api/babbles/upload`

Uploads an audio file for batch transcription via Azure Fast Transcription API. Creates a babble from the transcribed text with an auto-generated title.

**Content-Type**: `multipart/form-data`

**Form Fields**

| Field | Type | Required | Constraints |
|---|---|---|---|
| `file` | `file` | Yes | Audio file (`.mp3`, `.wav`, `.webm`, `.ogg`, `.m4a`). Max 500 MB. |
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

#### `POST /api/babbles/{id}/generate`

Generates a prompt from a babble's text using Azure OpenAI. The response is streamed as **Server-Sent Events** (SSE) with incremental text chunks. The generated prompt is automatically persisted as a child resource of the babble.

**Request Body**

| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `templateId` | `string` | Yes | | The ID of the prompt template to use for generation. |
| `promptFormat` | `string` | No | `"text"` | Desired output format (e.g., `"text"`, `"markdown"`). |
| `allowEmojis` | `boolean` | No | `false` | Whether emojis are permitted in the generated prompt. |

```json
{
  "templateId": "builtin-general-assistant",
  "promptFormat": "text",
  "allowEmojis": false
}
```

**Response** — `200 OK` (`text/event-stream`)

Text is streamed as incremental SSE `data:` chunks. After generation completes, the prompt is auto-persisted and its ID is sent.

```text
data: {"text":"Write a Python"}

data: {"text":" function that takes"}

data: {"text":" a list of numbers..."}

data: {"promptId":"def-456"}

data: [DONE]
```

**Error Responses**

| Status | Condition |
|---|---|
| `400 Bad Request` | `templateId` is missing or empty. |
| `404 Not Found` | Babble or template not found. |
| `502 Bad Gateway` | Azure OpenAI communication failure. |

#### `GET /api/babbles/search`

Performs a vector (semantic) similarity search across the current user's babbles using Azure OpenAI embeddings.

**Query Parameters**

| Parameter | Type | Default | Constraints | Description |
|---|---|---|---|---|
| `query` | `string` | — | Required, 1–200 characters | Natural-language search query. |
| `topK` | `integer` | `10` | Clamped to 1–50 | Maximum number of results to return. |

**Response** `200 OK`

```json
{
  "results": [
    {
      "id": "abc-123",
      "title": "Sort function request",
      "snippet": "I want a function that sorts a list of numbers...",
      "tags": ["code", "python"],
      "createdAt": "2025-01-15T10:30:00.0000000+00:00",
      "isPinned": false,
      "score": 0.95
    }
  ]
}
```

Results are ordered by descending similarity `score` (0–1). The `snippet` field is truncated to 200 characters.

**Error Responses**

| Status | Condition |
|---|---|
| `400 Bad Request` | `query` is missing, empty, or exceeds 200 characters. |
| `502 Bad Gateway` | Azure OpenAI embedding service failure. |

#### `POST /api/babbles/{id}/generate-title`

Generates a short descriptive title for a babble using Azure OpenAI and auto-saves it.

**Request Body** — none.

**Response** `200 OK` — the updated babble object.

```json
{
  "id": "abc-123",
  "title": "Sort Function Request",
  "text": "I want a function that sorts a list of numbers...",
  "createdAt": "2025-01-15T10:30:00.0000000+00:00",
  "updatedAt": "2025-01-15T10:35:00.0000000+00:00"
}
```

**Error Responses**

| Status | Condition |
|---|---|
| `404 Not Found` | Babble does not exist or does not belong to the current user. |
| `502 Bad Gateway` | Azure OpenAI communication failure. |

---

### Generated Prompts

Generated prompts are child resources of babbles. Each prompt stores the output of running a babble through a template. Generated prompts are **immutable** — they cannot be updated after creation.

All generated prompt endpoints require authentication. The parent babble must belong to the current user.

#### `GET /api/babbles/{babbleId}/prompts`

Returns a paginated list of generated prompts for a babble, ordered by generation date (newest first).

**Query Parameters**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `continuationToken` | `string` | `null` | Opaque token for fetching the next page. |
| `pageSize` | `integer` | `20` | Number of items per page (1–100). |

**Response** `200 OK`

```json
{
  "items": [
    {
      "id": "def-456",
      "babbleId": "abc-123",
      "templateId": "builtin-general-assistant",
      "templateName": "General Assistant",
      "promptText": "Write a Python function that takes a list of numbers...",
      "generatedAt": "2025-01-15T10:35:00.0000000+00:00"
    }
  ],
  "continuationToken": null
}
```

| Status | Condition |
|---|---|
| `404 Not Found` | Parent babble does not exist or does not belong to the current user. |

#### `GET /api/babbles/{babbleId}/prompts/{id}`

Returns a single generated prompt by ID.

**Response** `200 OK` — generated prompt object (same shape as items above).

| Status | Condition |
|---|---|
| `404 Not Found` | Prompt or parent babble does not exist, or babble does not belong to the current user. |

#### `POST /api/babbles/{babbleId}/prompts`

Creates a new generated prompt for a babble.

**Request Body**

| Field | Type | Required | Constraints |
|---|---|---|---|
| `templateId` | `string` | Yes | Non-empty |
| `templateName` | `string` | Yes | 1–100 characters |
| `promptText` | `string` | Yes | 1–50 000 characters |

```json
{
  "templateId": "builtin-general-assistant",
  "templateName": "General Assistant",
  "promptText": "Write a Python function that takes a list of numbers..."
}
```

**Response** `201 Created` — the created generated prompt object with a `Location` header.

| Status | Condition |
|---|---|
| `400 Bad Request` | Validation failure (see constraints above). |
| `404 Not Found` | Parent babble does not exist or does not belong to the current user. |

#### `DELETE /api/babbles/{babbleId}/prompts/{id}`

Deletes a generated prompt.

**Response** `204 No Content`

| Status | Condition |
|---|---|
| `404 Not Found` | Prompt or parent babble does not exist, or babble does not belong to the current user. |

---

### Transcription (WebSocket)

#### `GET /api/transcribe/stream`

Upgrades to a WebSocket connection for real-time audio transcription using Azure AI Speech Service.

**Query Parameters**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `language` | `string` | No | BCP-47 language code (e.g., `en-US`). |

**Protocol**

1. Client initiates a WebSocket upgrade request.
1. Client sends **binary** frames containing raw PCM audio (16 kHz, 16-bit, mono).
1. Server sends **text** frames with JSON transcription events:

```json
{
  "text": "hello world",
  "isFinal": false
}
```

| Field | Type | Description |
|---|---|---|
| `text` | `string` | The recognized text. |
| `isFinal` | `boolean` | `true` for final (confirmed) results, `false` for interim (partial) results. |

1. When the client finishes sending audio, it closes the WebSocket. The server completes any remaining recognition and closes with `NormalClosure`.

**Error Responses**

| Status | Condition |
|---|---|
| `400 Bad Request` | Request is not a WebSocket upgrade. |

---

## Configuration

The API reads the following configuration values:

| Key | Description |
|---|---|
| `Speech:Region` | Azure Speech Service region. |
| `ConnectionStrings:ai-foundry` | Azure OpenAI / AI Foundry connection string. |
| `ConnectionStrings:cosmos` | Azure Cosmos DB connection string. |
| `Azure:TenantId` | *(Optional)* Scopes `DefaultAzureCredential` to a specific tenant for local development. |
| `PromptTemplates:CacheDurationMinutes` | Template cache TTL in minutes (default: `5`). |

## CORS

In development the API allows requests from `localhost` and `127.0.0.1` origins with any header, method, and credentials.
