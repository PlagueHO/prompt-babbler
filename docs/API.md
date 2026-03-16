# Prompt Babbler API Reference

The Prompt Babbler service exposes a REST (and WebSocket) API for prompt generation, prompt template management, real-time audio transcription, and health status.

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

### Prompt Generation

#### `POST /api/prompts/generate`

Generates a prompt from transcribed babble text using Azure OpenAI. The response is streamed as **Server-Sent Events** (SSE).

The API looks up the system prompt and template name from the prompt template repository using the provided `templateId`.

**Request Body**

| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `babbleText` | `string` | Yes | | The transcribed text to generate a prompt from. |
| `templateId` | `string` | Yes | | The ID of the prompt template to use for generation. |
| `promptFormat` | `string` | No | `"text"` | Desired output format (e.g., `"text"`, `"markdown"`). |
| `allowEmojis` | `boolean` | No | `false` | Whether emojis are permitted in the generated prompt. |

```json
{
  "babbleText": "I want a function that sorts a list",
  "templateId": "builtin-general-assistant",
  "promptFormat": "text",
  "allowEmojis": false
}
```

**Response** — `200 OK` (`text/event-stream`)

Each chunk is sent as an SSE `data:` line containing a JSON object.

A `name` event is sent first, followed by the full prompt text.

```text
data: {"name":"My Template"}

data: {"text":"Generated prompt content..."}

data: [DONE]
```

**Error Responses**

| Status | Condition |
|---|---|
| `400 Bad Request` | `babbleText` or `templateId` is missing or empty. |
| `404 Not Found` | No template found with the given `templateId`. |
| `502 Bad Gateway` | Azure OpenAI communication failure. |

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
