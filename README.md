# Prompt Babbler

[![CI][ci-shield]][ci-url]
[![CD][cd-shield]][cd-url]
[![License: MIT][license-shield]][license-url]
[![.NET 10][dotnet-shield]][dotnet-url]
[![Node.js 22+][node-shield]][node-url]
[![TypeScript][ts-shield]][ts-url]
[![React 19][react-shield]][react-url]
[![Azure][azure-shield]][azure-url]
[![GitHub Issues][issues-shield]][issues-url]
[![PRs Welcome][prs-shield]][prs-url]
[![GitHub Stars][stars-shield]][stars-url]
[![Docs][docs-shield]][docs-url]

💬 Prompt Babbler is a speech-to-prompt web application that captures stream-of-consciousness speech, transcribes it using Microsoft Foundry, and generates structured prompts for target systems like GitHub Copilot. It can run locally with a Cosmos DB emulator or be deployed to Azure with a fully managed Cosmos DB instance and Microsoft Foundry resources. The app is built with a React frontend and a .NET backend, orchestrated by .NET Aspire for seamless local and cloud development.

## What It Does

Prompt Babbler turns rough speech into polished, structured prompts ready to use with AI tools. The typical workflow is:

1. **Record** — Click record and speak your thoughts out loud, stream-of-consciousness style.
1. **Transcribe** — Azure AI Speech Service converts your audio to text in real time.
1. **Generate** — The app sends your transcription through a configurable prompt template and calls a Foundry Models model to produce a structured, ready-to-use prompt.
1. **Use** — Copy the generated prompt into GitHub Copilot, an AI assistant, an image generator, or any other AI tool.

### Key Features

- **Real-time speech transcription** via Azure AI Speech Service — see your words appear as you speak.
- **Prompt templates** — Create and manage reusable templates that shape how your transcription is turned into a prompt. Templates support structured instructions, output format, guardrails, examples, and tags.
- **Prompt history** — Every generated prompt is saved alongside its source transcription (called a *babble*) so you can review and reuse past outputs.
- **Multi-target support** — Included built-in templates for GitHub Copilot, general AI assistants, and image generators.
- **Single-user and multi-user modes** — Run privately without authentication, or enable Microsoft Entra ID for multi-user access.
- **Fully cloud-native** — Deploys to Azure Static Web Apps + Azure Container Apps with Cosmos DB and Microsoft Foundry, all provisioned via Bicep.
- **MCP server** — Exposes babbles, templates, and prompt generation to GitHub Copilot, Claude, and any MCP-compatible AI client via the Model Context Protocol.

## Quick Start

For full setup instructions including prerequisite installation, see [docs/QUICKSTART-LOCAL.md](docs/QUICKSTART-LOCAL.md). To deploy to Azure, see [docs/QUICKSTART-AZURE.md](docs/QUICKSTART-AZURE.md).

### Prerequisites to run Locally

- An [Azure Account](https://azure.microsoft.com/free/) with **Contributor** access to allow Aspire to provision Microsoft Foundry resources for the app
- [Aspire CLI](https://aspire.dev/get-started/install-cli/) to orchestrate the components
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) to authenticate and allow Aspire to provision Microsoft Foundry resources in your subscription
- Install [Docker Desktop](https://docs.docker.com/desktop/) to host Cosmos DB emulator (Windows/Linux/Mac)

### Run Locally

```bash
git clone https://github.com/PlagueHO/prompt-babbler.git
cd prompt-babbler

# Sign in to Azure (one-time — Aspire provisions cloud AI resources)
az login --tenant <your-tenant-id>

# Start everything via Aspire
aspire run
```

Aspire handles all dependency installation, builds, and orchestration automatically. On first run it provisions Microsoft Foundry resources and starts a Cosmos DB emulator in Docker — this takes several minutes. Subsequent runs start quickly.

---

## MCP Server

Prompt Babbler includes an [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) server that exposes your babbles, prompt templates, and prompt generation to any MCP-compatible AI client. Connect GitHub Copilot, Claude, or another agent and let it search your voice notes or generate prompts on your behalf — without leaving the chat interface.

The MCP endpoint when running locally is `http://localhost:5242`. See [docs/MCP-SERVER.md](docs/MCP-SERVER.md) for the full tool reference, resource catalog, and authentication options.

### VS Code (GitHub Copilot Agent Mode)

Create `.vscode/mcp.json` in your workspace:

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

Open **GitHub Copilot Chat**, switch to **Agent Mode**, and `prompt-babbler` appears in the tools list.

### Claude Code

```bash
claude mcp add --transport http prompt-babbler http://localhost:5242
```

### GitHub Copilot CLI

```bash
gh copilot mcp add prompt-babbler --transport http http://localhost:5242
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/ARCHITECTURE.md) | Tech stack, project structure, API endpoints, data model, infrastructure |
| [Local Development](docs/QUICKSTART-LOCAL.md) | Run locally with .NET Aspire |
| [Deploy to Azure](docs/QUICKSTART-AZURE.md) | Deploy to Azure with Azure Developer CLI |
| [API Reference](docs/API.md) | Full API reference with request/response schemas |
| [MCP Server](docs/MCP-SERVER.md) | MCP tools, resources, prompts, and client configuration |
| [CI/CD Setup](docs/CICD.md) | GitHub Actions pipeline configuration |
| [Infrastructure](infra/README.md) | Azure Bicep infrastructure details |

## License

MIT

<!-- Badge reference links -->
[ci-shield]: https://img.shields.io/github/actions/workflow/status/PlagueHO/prompt-babbler/continuous-integration.yml?branch=main&label=CI
[ci-url]: https://github.com/PlagueHO/prompt-babbler/actions/workflows/continuous-integration.yml
[cd-shield]: https://img.shields.io/github/actions/workflow/status/PlagueHO/prompt-babbler/continuous-delivery.yml?branch=main&label=CD
[cd-url]: https://github.com/PlagueHO/prompt-babbler/actions/workflows/continuous-delivery.yml
[license-shield]: https://img.shields.io/badge/license-MIT-blue.svg
[license-url]: https://github.com/PlagueHO/prompt-babbler/blob/main/LICENSE
[dotnet-shield]: https://img.shields.io/badge/.NET-10-512bd4?logo=dotnet
[dotnet-url]: https://dotnet.microsoft.com/download/dotnet/10.0
[node-shield]: https://img.shields.io/badge/Node.js-22%2B-5fa04e?logo=nodedotjs
[node-url]: https://nodejs.org/
[ts-shield]: https://img.shields.io/badge/TypeScript-5-3178c6?logo=typescript&logoColor=white
[ts-url]: https://www.typescriptlang.org/
[react-shield]: https://img.shields.io/badge/React-19-61dafb?logo=react&logoColor=white
[react-url]: https://react.dev/
[azure-shield]: https://img.shields.io/badge/Azure-Deployed-0078d4?logo=microsoftazure&logoColor=white
[azure-url]: https://azure.microsoft.com/
[issues-shield]: https://img.shields.io/github/issues/PlagueHO/prompt-babbler
[issues-url]: https://github.com/PlagueHO/prompt-babbler/issues
[prs-shield]: https://img.shields.io/badge/PRs-welcome-brightgreen.svg
[prs-url]: https://github.com/PlagueHO/prompt-babbler/pulls
[stars-shield]: https://img.shields.io/github/stars/PlagueHO/prompt-babbler?style=flat
[stars-url]: https://github.com/PlagueHO/prompt-babbler/stargazers
[docs-shield]: https://img.shields.io/badge/docs-online-purple?logo=readthedocs&logoColor=white
[docs-url]: https://plagueho.github.io/prompt-babbler/
