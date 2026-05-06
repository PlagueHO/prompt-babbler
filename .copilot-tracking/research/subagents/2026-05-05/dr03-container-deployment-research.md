# DR03 — Container Deployment Patterns Research

**Date:** 2026-05-05
**Status:** Complete
**Scope:** Existing Docker/Container configuration patterns for Azure Container Apps deployment

---

## Research Topics

1. Dockerfile(s) in the repository
2. Azure Container Apps Bicep configuration in `infra/`
3. `azure.yaml` — how `azd` maps services to Container Apps
4. `AppHost.cs` — Aspire local orchestration and container references
5. `.dockerignore` files
6. Container Apps environment configuration: scale rules, CPU/memory, ingress, env vars

---

## Findings

### 1. Dockerfile

**File:** `prompt-babbler-service/src/Api/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS base          # line 1

# Install native dependencies required by Speech SDK
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libasound2t64 \
        libssl3t64 \
    && rm -rf /var/lib/apt/lists/*                               # lines 3-9

WORKDIR /app
EXPOSE 8080                                                       # lines 11-12

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build                  # line 14
WORKDIR /src                                                      # line 15

COPY Directory.Build.props Directory.Packages.props ./            # line 17
COPY src/Api/PromptBabbler.Api.csproj src/Api/                   # line 18
COPY src/Domain/PromptBabbler.Domain.csproj src/Domain/          # line 19
COPY src/Infrastructure/PromptBabbler.Infrastructure.csproj src/Infrastructure/   # line 20
COPY src/Orchestration/ServiceDefaults/PromptBabbler.ServiceDefaults.csproj src/Orchestration/ServiceDefaults/  # line 21
RUN dotnet restore src/Api/PromptBabbler.Api.csproj              # line 22

COPY src/ src/                                                    # line 24
WORKDIR /src/src/Api                                              # line 25
RUN dotnet publish -c Release -o /app/publish --no-restore       # line 26

FROM base AS final                                                # line 28
WORKDIR /app                                                      # line 29
COPY --from=build /app/publish .                                 # line 30
ENTRYPOINT ["dotnet", "PromptBabbler.Api.dll"]                   # line 31
```

**Key patterns:**

- 3-stage multi-stage build: `base` → `build` → `final`
- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0-noble` (Ubuntu Noble variant)
- SDK image: `mcr.microsoft.com/dotnet/sdk:10.0`
- HTTP port: `8080` (matches ACA ingress target port)
- Native dependency installation in base stage (Speech SDK: `libasound2t64`, `libssl3t64`)
- `apt-get` cleanup pattern: `rm -rf /var/lib/apt/lists/*`
- Copies `Directory.Build.props` and `Directory.Packages.props` before project files (enables NuGet restore layer caching)
- Copies only `.csproj` files before `dotnet restore` (Docker layer cache optimization)
- Copies `src/` source after restore
- Publish is Release, `--no-restore`, outputs to `/app/publish`
- NOT self-contained; targets framework `net10.0` implicitly
- Entrypoint: `dotnet PromptBabbler.Api.dll`

**Docker build context (from CI workflow):**
- Context: `./prompt-babbler-service` (the service folder)
- Dockerfile: `./prompt-babbler-service/src/Api/Dockerfile`
- The Dockerfile uses paths relative to the context root (`prompt-babbler-service/`)

---

### 2. `.dockerignore`

**File:** `prompt-babbler-service/.dockerignore`

```
**/bin/
**/obj/
**/TestResults/
**/.vs/
```

Located at `prompt-babbler-service/` (matches the Docker build context root).
Excludes build artifacts, VS metadata, and test results.

---

### 3. `azure.yaml` — `azd` Service Mapping

**File:** `azure.yaml`

```yaml
services:
  api:
    host: containerapp
    language: dotnet
    project: ./prompt-babbler-service/src/Api
    docker:
      path: ./Dockerfile           # relative to project dir
      context: ../../              # relative to project dir = prompt-babbler-service/
  frontend:
    project: ./prompt-babbler-app
    language: js
    host: staticwebapp
    dist: dist
```

- Service name `api` maps to the Container App with tag `azd-service-name: api`
- `docker.path` is relative to the `project:` directory → resolves to `prompt-babbler-service/src/Api/Dockerfile`
- `docker.context` is `../../` relative to the project dir → resolves to `prompt-babbler-service/`
- `azd` uses the tag `azd-service-name` on the Container App to identify deployment target

---

### 4. Container App Bicep Configuration

**File:** `infra/main.bicep` (lines ~508–600)

**AVM module used:** `br/public:avm/res/app/container-app:0.22.1`

#### Resource naming pattern

```bicep
var containerAppName = '${abbrs.appContainerApps}${environmentName}-api'
// abbrs.appContainerApps = 'ca-'
// Result: 'ca-{environmentName}-api'
// Example: 'ca-myenv-api'

var containerAppsEnvironmentName = '${abbrs.appManagedEnvironments}${environmentName}'
// abbrs.appManagedEnvironments = 'cae-'
// Result: 'cae-{environmentName}'
```

**For a new MCP server Container App, the pattern would be:**
```bicep
var mcpServerContainerAppName = '${abbrs.appContainerApps}${environmentName}-mcp-server'
// Result: 'ca-{environmentName}-mcp-server'
```

#### Container App parameters

```bicep
module containerApp 'br/public:avm/res/app/container-app:0.22.1' = {
  params: {
    name: containerAppName                                // ca-{env}-api
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    location: location
    tags: union(tags, {
      'azd-service-name': 'api'                          // azd discovery tag
    })
    managedIdentities: {
      systemAssigned: true                               // system-assigned MI
    }
    containers: [
      {
        name: 'api'                                      // internal container name
        image: containerImage                            // param; default: ghcr.io/plagueho/prompt-babbler-api:latest
        resources: {
          cpu: json('0.5')
          memory: '1Gi'
        }
        env: [ ... ]                                     // see env vars section
      }
    ]
    ingressExternal: true                                // publicly accessible
    ingressTargetPort: 8080                              // matches EXPOSE 8080
    ingressTransport: 'auto'
    scaleSettings: {
      minReplicas: 0                                     // scales to zero
      maxReplicas: 3
    }
  }
}
```

#### Environment variables injected at deployment

| Variable | Value source | Conditional |
|---|---|---|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights module output | No |
| `AZURE_AI_FOUNDRY_ENDPOINT` | Foundry module output | No |
| `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` | Foundry computed endpoint | No |
| `ConnectionStrings__cosmos` | `AccountEndpoint=${cosmosDb.endpoint}` | No |
| `ConnectionStrings__foundry` | `Endpoint=${foundry.endpoint}` | No |
| `ConnectionStrings__ai-foundry` | `Endpoint=${foundryProjectEndpoint}` | No |
| `AzureAd__ClientId` | Bicep param `apiClientId` | Only if `apiClientId` non-empty |
| `AzureAd__TenantId` | `tenant().tenantId` | Only if `apiClientId` non-empty |
| `AzureAd__Instance` | `environment().authentication.loginEndpoint` | Only if `apiClientId` non-empty |
| `CORS__AllowedOrigins` | `https://${staticWebApp.defaultHostname}` | No |
| `ACCESS_CODE` | Bicep secure param `accessCode` | Only if `accessCode` non-empty |

#### RBAC assigned to Container App managed identity

- `Cognitive Services OpenAI User` on Foundry resource
- `Cognitive Services Speech User` on Foundry resource
- Cosmos DB `Built-in Data Contributor` (`00000000-0000-0000-0000-000000000002`) on Cosmos DB account

---

### 5. Container App Bicep input parameter

```bicep
@sys.description('Container image to deploy for the backend API Container App.')
param containerImage string = 'ghcr.io/plagueho/prompt-babbler-api:latest'
```

The image is a Bicep parameter. The CI/CD pipeline passes the versioned tag on production deploys. This decouples the infra from the image tag.

---

### 6. AppHost.cs — Aspire Orchestration (local dev only)

**File:** `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`

Key findings:

- Uses `builder.AddProject<Projects.PromptBabbler_Api>("api")` — **no `.WithDockerfile()`**
- Aspire handles local connectivity via service discovery, NOT Docker containerization
- Local dev runs the API project directly (not in a container)
- References: `foundry`, `foundryProject`, `chatDeployment`, `embeddingDeployment`, `cosmos`, containers
- Injects local env vars: `Azure__TenantId`, `AZURE_TENANT_ID`, `Speech__Region`, `AzureAd__*`

**Conclusion:** Aspire is used **only for local development orchestration**. The Dockerfile and Bicep handle production containerization and deployment independently.

---

### 7. CI/CD — Container Image Build and Push

**File:** `.github/workflows/continuous-delivery.yml` (job `build-and-push-api-container`)

```yaml
- name: Build and push API container image
  uses: docker/build-push-action@v7
  with:
    context: ./prompt-babbler-service
    file: ./prompt-babbler-service/src/Api/Dockerfile
    push: true
    tags: |
      ghcr.io/plagueho/prompt-babbler-api:latest     # only on main branch
      ghcr.io/plagueho/prompt-babbler-api:sha-{sha}  # always
      ghcr.io/plagueho/prompt-babbler-api:{tag}       # on v* tags
```

- Registry: **GitHub Container Registry (GHCR)** — `ghcr.io`
- Image name: `ghcr.io/{owner}/prompt-babbler-api`
- Production deploy uses the versioned tag `ghcr.io/plagueho/prompt-babbler-api:{version-tag}`
- `latest` tag is only updated on pushes to `main`

---

### 8. Container Apps Environment

**File:** `infra/main.bicep` (lines ~462–495)

```bicep
module containerAppsEnvironment 'br/public:avm/res/app/managed-environment:0.13.2' = {
  params: {
    name: containerAppsEnvironmentName                  // cae-{environmentName}
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsWorkspaceResourceId: ...
    }
    zoneRedundant: false
    infrastructureSubnetResourceId: virtualNetwork.outputs.subnetResourceIds[0]  // ACA subnet
    internal: false                                     // publicly accessible
    publicNetworkAccess: 'Enabled'
  }
}
```

- Shared environment for all Container Apps in the deployment
- VNET-integrated (ACA subnet: `10.0.0.0/23`)
- External / public (not internal-only)
- Logs to Log Analytics

---

## Key Patterns for New MCP Server Container App

Based on the research, a new MCP server Container App should follow these patterns:

### Naming

```bicep
var mcpServerContainerAppName = '${abbrs.appContainerApps}${environmentName}-mcp-server'
// e.g. 'ca-myenv-mcp-server'
```

### Bicep module call (template)

```bicep
module mcpServerContainerApp 'br/public:avm/res/app/container-app:0.22.1' = {
  name: 'container-app-mcp-server-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  params: {
    name: mcpServerContainerAppName
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    location: location
    tags: union(tags, {
      'azd-service-name': 'mcp-server'
    })
    managedIdentities: {
      systemAssigned: true
    }
    containers: [
      {
        name: 'mcp-server'
        image: mcpServerContainerImage
        resources: {
          cpu: json('0.5')
          memory: '1Gi'
        }
        env: [ ... ]
      }
    ]
    ingressExternal: true
    ingressTargetPort: 8080
    ingressTransport: 'auto'
    scaleSettings: {
      minReplicas: 0
      maxReplicas: 3
    }
  }
}
```

### Dockerfile (template)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/McpServer/PromptBabbler.McpServer.csproj src/McpServer/
COPY src/Domain/PromptBabbler.Domain.csproj src/Domain/
COPY src/Infrastructure/PromptBabbler.Infrastructure.csproj src/Infrastructure/
COPY src/Orchestration/ServiceDefaults/PromptBabbler.ServiceDefaults.csproj src/Orchestration/ServiceDefaults/
RUN dotnet restore src/McpServer/PromptBabbler.McpServer.csproj
COPY src/ src/
WORKDIR /src/src/McpServer
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PromptBabbler.McpServer.dll"]
```

### azure.yaml addition

```yaml
services:
  api:
    ...
  mcp-server:
    host: containerapp
    language: dotnet
    project: ./prompt-babbler-service/src/McpServer
    docker:
      path: ./Dockerfile
      context: ../../
  frontend:
    ...
```

### GHCR image name

```
ghcr.io/plagueho/prompt-babbler-mcp-server
```

---

## Gaps / Follow-on Questions

1. **Does the MCP server need Speech SDK native dependencies?** The API installs `libasound2t64` and `libssl3t64` for the Speech SDK. If the MCP server does not use Speech, these can be omitted (simpler base stage).

2. **What environment variables will the MCP server need?** The API injects Foundry, Cosmos, Entra ID, CORS, and access code vars. The MCP server's variable set depends on what services it calls.

3. **Does the MCP server need external ingress or internal-only?** The API is `ingressExternal: true`. If the MCP server is consumed only by the API (internal), `ingressExternal: false` with `ingressTargetPort: 8080` would be more secure.

4. **Will the MCP server image be published to GHCR as `prompt-babbler-mcp-server`?** The CI workflow will need a new job mirroring `build-and-push-api-container`.

5. **What RBAC roles does the MCP server managed identity need?** Depends on which Azure resources it accesses (Foundry, Cosmos, etc.).

6. **Does the MCP server run in the same Container Apps Environment?** Based on patterns it should share `containerAppsEnvironmentName`, same as the API.

---

## References

- `prompt-babbler-service/src/Api/Dockerfile`
- `prompt-babbler-service/.dockerignore`
- `azure.yaml`
- `infra/main.bicep`
- `infra/abbreviations.json`
- `prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs`
- `.github/workflows/continuous-delivery.yml`
- `.github/workflows/build-and-publish-backend-service.yml`
