# Prompt Babbler

[![CI][ci-shield]][ci-url]
[![CD][cd-shield]][cd-url]
[![License][license-shield]][license-url]
[![Azure][azure-shield]][azure-url]
[![IaC][iac-shield]][iac-url]

💬 Prompt Babbler is a speech-to-prompt web application that captures stream-of-consciousness speech, transcribes it using Azure AI Foundry, and generates structured prompts for target systems like GitHub Copilot. It can run locally with a Cosmos DB emulator or be deployed to Azure with a fully managed Cosmos DB instance and Azure AI Foundry resources. The app is built with a React frontend and a .NET backend, orchestrated by .NET Aspire for seamless local and cloud development.

## Quick Start

For full setup instructions including prerequisite installation, see [docs/QUICKSTART-LOCAL.md](docs/QUICKSTART-LOCAL.md). To deploy to Azure, see [docs/QUICKSTART-AZURE.md](docs/QUICKSTART-AZURE.md).

### Prerequisites to run Locally

- An [Azure Account](https://azure.microsoft.com/free/) with **Contributor** access to allow Aspire to provision Azure AI Foundry resources for the app
- [Aspire CLI](https://aspire.dev/get-started/install-cli/) to orchestrate the components
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) to authenticate and allow Aspire to provision Azure AI Foundry resources in your subscription
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

Aspire handles all dependency installation, builds, and orchestration automatically. On first run it provisions Azure AI Foundry resources and starts a Cosmos DB emulator in Docker — this takes several minutes. Subsequent runs start quickly.

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/ARCHITECTURE.md) | Tech stack, project structure, API endpoints, data model, infrastructure |
| [Local Development](docs/QUICKSTART-LOCAL.md) | Run locally with .NET Aspire |
| [Deploy to Azure](docs/QUICKSTART-AZURE.md) | Deploy to Azure with Azure Developer CLI |
| [API Reference](docs/API.md) | Full API reference with request/response schemas |
| [CI/CD Setup](docs/CICD.md) | GitHub Actions pipeline configuration |
| [Infrastructure](infra/README.md) | Azure Bicep infrastructure details |

## License

MIT

<!-- Badge reference links -->
[ci-shield]: https://img.shields.io/github/actions/workflow/status/PlagueHO/prompt-babbler/continuous-integration.yml?branch=main&label=CI
[ci-url]: https://github.com/PlagueHO/prompt-babbler/actions/workflows/continuous-integration.yml
[cd-shield]: https://img.shields.io/github/actions/workflow/status/PlagueHO/prompt-babbler/continuous-delivery.yml?branch=main&label=CD
[cd-url]: https://github.com/PlagueHO/prompt-babbler/actions/workflows/continuous-delivery.yml
[license-shield]: https://img.shields.io/github/license/PlagueHO/prompt-babbler
[license-url]: https://github.com/PlagueHO/prompt-babbler/blob/main/LICENSE
[azure-shield]: https://img.shields.io/badge/Azure-Solution%20Accelerator-0078D4?logo=microsoftazure&logoColor=white
[azure-url]: https://azure.microsoft.com/
[iac-shield]: https://img.shields.io/badge/Infrastructure%20as%20Code-Bicep-5C2D91?logo=azurepipelines&logoColor=white
[iac-url]: https://learn.microsoft.com/azure/azure-resource-manager/bicep/overview
