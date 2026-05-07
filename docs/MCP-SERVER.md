---
title: MCP Server Reference
description: Reference guide for the Prompt Babbler MCP server — tools, resources, prompts, authentication, and client configuration for VS Code, Claude Code, and GitHub Copilot CLI.
---

## Overview

Prompt Babbler exposes its full API surface to AI agents via the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/). The MCP server lets GitHub Copilot, Claude, and any MCP-compatible client search your babbles, generate prompts, and manage templates without leaving the AI chat interface.

The server uses **Streamable HTTP** transport in stateless mode, making it compatible with all modern MCP clients. When running locally via Aspire or directly, the endpoint is:

```text
http://localhost:5242
```

Find the live URL from the Aspire dashboard when running via `aspire run`.

## Authentication

The MCP server uses the same three authentication modes as the main API. The active mode is determined at startup by configuration.

| Mode | Trigger | Header required |
|------|---------|-----------------|
| **Anonymous** | No access code, no Entra ID | None |
| **Access code** | `AccessControl:AccessCode` is set | `Authorization: Bearer <access-code>` |
| **Entra ID** | `AzureAd:ClientId` is set | OAuth 2.0 via OBO flow (handled automatically by clients that support OAuth resource metadata) |

> [!NOTE]
> When running locally with `aspire run` and no authentication configured, no header is needed. Add an access code in `appsettings.Development.json` or via user secrets for shared local environments.

## Client Configuration

### VS Code (GitHub Copilot Agent Mode)

Create or update `.vscode/mcp.json` in your workspace root:

**Anonymous / no auth:**

```json
{
  "servers": {
    "prompt-babbler": {
      "type": "http",
      "url": "http://localhost:5242"
    }
  }
}
```

**With access code** (VS Code prompts once and caches the value):

```json
{
  "servers": {
    "prompt-babbler": {
      "type": "http",
      "url": "http://localhost:5242",
      "headers": {
        "Authorization": "Bearer ${input:promptBabblerCode}"
      },
      "inputs": [
        {
          "id": "promptBabblerCode",
          "type": "promptString",
          "description": "Prompt Babbler access code",
          "password": true
        }
      ]
    }
  }
}
```

After saving, open the **GitHub Copilot Chat** panel, switch to **Agent Mode**, and the `prompt-babbler` server appears in the tools list.

### Claude Code

Add the server to Claude Code using the `claude mcp add` command:

**Anonymous / no auth:**

```bash
claude mcp add --transport http prompt-babbler http://localhost:5242
```

**With access code:**

```bash
claude mcp add --transport http prompt-babbler http://localhost:5242 \
  --header "Authorization: Bearer <your-access-code>"
```

Verify the server is registered:

```bash
claude mcp list
```

### GitHub Copilot CLI

Add the server to the GitHub Copilot CLI MCP configuration:

**Anonymous / no auth:**

```bash
gh copilot mcp add prompt-babbler --transport http http://localhost:5242
```

**With access code:**

```bash
gh copilot mcp add prompt-babbler --transport http http://localhost:5242 \
  --header "Authorization: Bearer <your-access-code>"
```

> [!TIP]
> Verify the server is active with `gh copilot mcp list`. For persistent configuration, check `~/.config/gh/copilot/mcp.json` for the stored settings.

## Tools

The MCP server exposes nine tools across three categories.

### Babble Tools

| Tool | Read-only | Description |
|------|-----------|-------------|
| `search_babbles` | Yes | Semantic/vector search across babbles ranked by relevance |
| `list_babbles` | Yes | Paginated list of all babbles |
| `get_babble` | Yes | Retrieve a single babble by ID |
| `generate_prompt` | No | Generate an AI prompt from a babble using a template |

#### `search_babbles`

Runs a vector similarity search across all babble transcriptions and returns results ranked by relevance to the query.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `query` | string | Yes | The search query text |
| `topK` | integer | No | Maximum results to return (1–50, default 10) |

#### `list_babbles`

Returns a paginated list of babbles. Pass the `continuationToken` from a previous response to fetch the next page.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `continuationToken` | string | No | Pagination token from a previous response |
| `pageSize` | integer | No | Results per page (1–100, default 20) |

#### `get_babble`

Retrieves a single babble by its unique ID. Returns `null` if not found.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | The babble ID |

#### `generate_prompt`

Generates a structured AI prompt from a babble's transcription text using the specified template. Streams the response from the Foundry Models model and returns the complete text.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `babbleId` | string | Yes | The babble ID to generate from |
| `templateId` | string | Yes | The prompt template ID to use |
| `promptFormat` | string | No | Output format: `text` or `markdown` |
| `allowEmojis` | boolean | No | Whether to allow emojis in the output |

---

### Prompt Template Tools

| Tool | Read-only | Destructive | Description |
|------|-----------|-------------|-------------|
| `list_templates` | Yes | No | List all templates (built-in and user-created) |
| `get_template` | Yes | No | Retrieve a single template by ID |
| `create_template` | No | No | Create a new user-defined template |
| `update_template` | No | No | Update an existing user-defined template |
| `delete_template` | No | Yes | Permanently delete a user-defined template |

#### `list_templates`

Returns all available prompt templates, including built-in templates shipped with Prompt Babbler and any user-created templates. No parameters required.

#### `get_template`

Retrieves a single template by its unique ID. Returns `null` if not found.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | The template ID |

#### `create_template`

Creates a new user-defined prompt template. Built-in templates cannot be modified.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Display name for the template |
| `description` | string | Yes | What this template does |
| `instructions` | string | Yes | System instructions for the AI |
| `outputDescription` | string | No | Description of the expected output format |
| `defaultOutputFormat` | string | No | Default format: `text` or `markdown` |
| `defaultAllowEmojis` | boolean | No | Default emoji setting |
| `tags` | string[] | No | Tags for categorisation |

#### `update_template`

Updates an existing user-defined template. Accepts the same parameters as `create_template` plus the required `id`. Built-in templates return an error if update is attempted.

#### `delete_template`

> [!CAUTION]
> This action is irreversible. Built-in templates cannot be deleted.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | The template ID to delete |

---

### Generated Prompt Tools

| Tool | Read-only | Description |
|------|-----------|-------------|
| `list_generated_prompts` | Yes | Paginated list of generated prompts for a babble |
| `get_generated_prompt` | Yes | Retrieve a single generated prompt by ID |

#### `list_generated_prompts`

Returns all generated prompts produced from a specific babble, with pagination support.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `babbleId` | string | Yes | The babble ID to list prompts for |
| `continuationToken` | string | No | Pagination token from a previous response |
| `pageSize` | integer | No | Results per page (1–100, default 20) |

#### `get_generated_prompt`

Retrieves a single generated prompt by its ID.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `babbleId` | string | Yes | The babble ID the prompt belongs to |
| `id` | string | Yes | The generated prompt ID |

---

## Resources

MCP resources are read-only data sources that clients can subscribe to and browse. Prompt Babbler exposes the template catalog as MCP resources.

| URI | MIME type | Description |
|-----|-----------|-------------|
| `babbler://templates` | `application/json` | All available prompt templates |
| `babbler://templates/{id}` | `application/json` | A single template by ID |

Resources are useful in clients that support the MCP resource browser — the client can read the template catalog directly without invoking a tool.

---

## Prompts

MCP prompts are predefined conversation seeds that the user can invoke like slash commands. They return a structured message that starts a focused conversation.

### `review_template`

Seeds a conversation to review and improve a prompt template. Provides structured guidance across four evaluation areas: clarity, completeness, effectiveness, and conciseness.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `instructions` | string | Yes | The template instructions text to review |
| `description` | string | Yes | The template description for context |

The prompt returns a single `User` message containing the template content and a four-point review framework, ready for the AI to respond with specific, actionable suggestions.

---

## Local Development

When running the MCP server standalone (outside Aspire):

```bash
cd prompt-babbler-service/src/McpServer
dotnet run
```

The server starts on `http://localhost:5242`. The MCP endpoint is at `http://localhost:5242/mcp`.

When running via Aspire (`aspire run`), the server is registered as the `mcp-server` resource and the port is allocated dynamically. Check the **Aspire dashboard** for the current URL.

To configure an access code for local testing, add to `appsettings.Development.json`:

```json
{
  "AccessControl": {
    "AccessCode": "your-local-access-code"
  }
}
```

Or use .NET user secrets:

```bash
cd prompt-babbler-service/src/McpServer
dotnet user-secrets set "AccessControl:AccessCode" "your-local-access-code"
```

---

## Configuration Reference

| Key | Description | Required |
|-----|-------------|----------|
| `AccessControl:AccessCode` | Static access code for access-code auth mode | No |
| `AzureAd:ClientId` | Entra ID application (client) ID — enables JWT auth mode | No |
| `AzureAd:TenantId` | Entra ID tenant ID | When `ClientId` is set |
| `AzureAd:Instance` | Entra ID authority base URL (default: `https://login.microsoftonline.com/`) | No |
| `AzureAd:ApiScope` | Scope used when acquiring OBO tokens for the downstream API | When `ClientId` is set |

When neither `AccessControl:AccessCode` nor `AzureAd:ClientId` is set, the server runs in **anonymous mode** with no authentication.
