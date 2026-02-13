# Infrastructure (vNext)

This directory will contain Bicep Infrastructure-as-Code (IaC) modules for Azure deployment.

## Planned Architecture

- **Azure Container Apps** — .NET backend API
- **Azure Static Web Apps** — React frontend
- **Azure OpenAI** — LLM and Whisper STT (managed identity)
- **Azure Log Analytics** — Logging and monitoring
- **Azure Container Registry** — Docker images

## Usage (vNext)

```bash
# Provision Azure resources
azd provision

# Deploy application
azd deploy

# Or do both
azd up
```

## Status

Not yet implemented. V1 is local-only. See [plan.md](../specs/001-babble-web-app/plan.md) for details.
