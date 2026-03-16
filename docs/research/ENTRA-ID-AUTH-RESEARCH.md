# Entra ID Authentication Research: Prompt Babbler

> **Date:** 2026-03-16
> **Status:** Research / Pre-planning
> **Goal:** Add Entra ID SSO authentication so users can log into the application. Unauthenticated users can see the main page, but all APIs are protected.

## Current State

- **No authentication exists** — `docs/API.md` confirms all operations use `_anonymous` identity.
- `PromptTemplateController` hardcodes `AnonymousUserId = "_anonymous"` — the Domain layer already supports `userId` throughout.
- Frontend has **no MSAL**, no auth context, no token acquisition.
- The API has **no JWT Bearer middleware**, no `[Authorize]` attributes.
- The Aspire AppHost has **no auth-related configuration**.
- Infrastructure Bicep has **no Entra ID app registrations**.

## Architecture Overview (Two App Registrations Required)

### 1. SPA App Registration (Frontend — React)

- **Type**: Public client (SPA platform)
- **Auth flow**: Authorization Code with PKCE (standard for SPAs)
- **Redirect URIs**: `http://localhost:5173` (Vite dev), production URL from Static Web App
- **No client secret** (public client)

### 2. API App Registration (Backend — ASP.NET Core)

- **Type**: Web API
- **Expose API scopes**: e.g., `api://prompt-babbler-api/access_as_user`
- **The SPA app registration requests this scope** as a delegated permission
- **Pre-authorize** the SPA client app for the API scope (avoids user consent prompt)

## Task Breakdown

### Task 1: Bicep / Infrastructure — Entra ID App Registrations

**Difficulty: HIGH | Risk: HIGH**

Microsoft Graph Bicep extension (`Microsoft.Graph/applications@v1.0`) **does support** creating app registrations declaratively. Key findings:

- The resource type `Microsoft.Graph/applications@v1.0` supports `spa.redirectUris`, `api.oauth2PermissionScopes`, `api.preAuthorizedApplications`, `signInAudience`, `requiredResourceAccess`, `appRoles`, and `optionalClaims`.
- You also need `Microsoft.Graph/servicePrincipals@v1.0` for each app registration.
- **`signInAudience: 'AzureADMyOrg'`** for internal tenant.

**Sub-tasks:**

1. Create API app registration Bicep with:
   - `displayName`, `signInAudience: 'AzureADMyOrg'`
   - `identifierUris: ['api://prompt-babbler-api']` (well-known URI — see [R4](#r4-identifieruris-self-reference))
   - `api.oauth2PermissionScopes` — define `access_as_user` scope
   - `api.requestedAccessTokenVersion: 2`
   - `optionalClaims` for `idtyp` claim
   - `api.preAuthorizedApplications` — reference the SPA's appId (avoids user consent prompt)
   - Service principal for the API app
1. Create SPA app registration Bicep with:
   - `spa.redirectUris` — localhost and production URLs
   - `requiredResourceAccess` — reference the API app's scope
   - Service principal for the SPA app
1. Output `clientId` and `tenantId` values from Bicep for both apps.

### Task 2: Backend — ASP.NET Core API Authentication

**Difficulty: MEDIUM | Risk: LOW**

**Sub-tasks:**

1. Add NuGet package `Microsoft.Identity.Web` to `PromptBabbler.Api.csproj`.
1. Configure JWT Bearer authentication in `Program.cs`:

   ```csharp
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
   ```

1. Add `AzureAd` section to `appsettings.json` with `Instance`, `TenantId`, `ClientId`, `Scopes`.
1. Add `app.UseAuthentication()` and `app.UseAuthorization()` middleware in correct order (before `app.MapControllers()`).
1. Add `[Authorize]` attribute to protected controllers:
   - `PromptTemplateController`
   - `PromptController`
   - `TranscriptionWebSocketController`
1. Keep `StatusController` as `[AllowAnonymous]` (health check).
1. Replace `AnonymousUserId` with real user identity extracted from JWT claims (`User.GetObjectId()` from `Microsoft.Identity.Web`).
1. Add scope validation with `[RequiredScope]` or `[RequiredScopeOrAppPermission]`.

### Task 3: Frontend — MSAL React Integration

**Difficulty: MEDIUM | Risk: MEDIUM**

**Sub-tasks:**

1. Install packages: `@azure/msal-browser`, `@azure/msal-react`.
1. Create `authConfig.ts` with MSAL configuration:
   - `clientId` (SPA app registration client ID)
   - `authority: 'https://login.microsoftonline.com/{tenantId}'`
   - `redirectUri` based on environment
   - `cacheLocation: 'sessionStorage'`
1. Initialize `PublicClientApplication` instance in `main.tsx`.
1. Wrap `<App />` in `<MsalProvider>` in `main.tsx`.
1. Add sign-in / sign-out UI to navigation (layout components).
1. Use `AuthenticatedTemplate` / `UnauthenticatedTemplate` for conditional rendering.
1. Update `api-client.ts` to acquire access tokens and attach as `Authorization: Bearer {token}` header on all API calls using `acquireTokenSilent`.
1. Update `transcription-stream.ts` to pass the access token when connecting to the WebSocket (likely as a query parameter).
1. Define `loginRequest` scopes: `['api://prompt-babbler-api/access_as_user']`.

### Task 4: Aspire AppHost Integration

**Difficulty: LOW-MEDIUM | Risk: MEDIUM**

**Sub-tasks:**

1. Pass `AzureAd:ClientId`, `AzureAd:TenantId`, `AzureAd:Instance` configuration to the API project in `AppHost.cs`.
1. Pass SPA client ID and tenant ID as environment variables to the Vite frontend app for MSAL config.
1. Handle local dev vs deployed scenarios — redirect URIs differ.

### Task 5: Testing Updates

**Difficulty: MEDIUM | Risk: LOW**

**Sub-tasks:**

1. Update integration tests to handle authenticated requests — `WebApplicationFactory` in `Api.IntegrationTests` will need to either mock auth or use a test token.
1. Update frontend tests to mock MSAL context.
1. Add unit tests for auth-related utilities (token extraction, etc.).

### Task 6: Documentation Updates

**Difficulty: LOW | Risk: LOW**

- Update `docs/API.md` (currently says auth not implemented).
- Update `docs/QUICKSTART.md` with app registration setup steps.
- Update `README.md`.

## Difficulty & Risk Summary

### Original Ratings

| Task | Difficulty | Risk | Notes |
|------|-----------|------|-------|
| 1. Bicep App Registrations | **HIGH** | **HIGH** | Graph Bicep ext is newer, circular references, deployment permissions |
| 2. Backend JWT Auth | **MEDIUM** | **LOW** | Well-documented `Microsoft.Identity.Web` pattern |
| 3. Frontend MSAL | **MEDIUM** | **MEDIUM** | WebSocket auth and config injection are complex |
| 4. Aspire Integration | **LOW-MEDIUM** | **MEDIUM** | No built-in Aspire support for Entra auth |
| 5. Testing | **MEDIUM** | **LOW** | Standard test infrastructure updates |
| 6. Documentation | **LOW** | **LOW** | Straightforward updates |

### Revised Ratings (Post-Research)

| Task | Original Difficulty | Revised Difficulty | Revised Risk | Notes |
|------|--------------------|--------------------|--------------|-------|
| 1. Bicep App Registrations | HIGH | **MEDIUM-HIGH** | **MEDIUM** | Graph Bicep ext works; main risk is preview status |
| 2. Backend JWT Auth | MEDIUM | **MEDIUM** | **LOW** | All patterns confirmed; WebSocket solved |
| 3. Frontend MSAL | MEDIUM | **MEDIUM** | **LOW** | Config injection pattern matches existing codebase |
| 4. Aspire Integration | LOW-MEDIUM | **LOW** | **LOW** | Simple env var passing; no surprises |
| 5. Testing | MEDIUM | **LOW-MEDIUM** | **LOW** | Standard patterns confirmed with code examples |
| 6. Documentation | LOW | **LOW** | **LOW** | Straightforward updates |

---

## Research Findings

### Research Summary

26 research items identified across 5 areas. All 26 complete.

| ID | Area | Research Item | Status | Blocks |
|----|------|---------------|--------|--------|
| [R1](#r1-graph-bicep-extension-deployment-permissions) | Infrastructure | Graph Bicep Extension Deployment Permissions | ✅ Complete | Task 1 |
| [R2](#r2-circular-reference-between-spa-and-api-app-registrations) | Infrastructure | Circular Reference Between SPA and API App Registrations | ✅ Complete | Task 1 |
| [R3](#r3-graph-bicep-extension-maturity--compatibility) | Infrastructure | Graph Bicep Extension Maturity & Compatibility | ✅ Complete | Task 1 |
| [R4](#r4-identifieruris-self-reference) | Infrastructure | `identifierUris` Self-Reference | ✅ Complete | Task 1 |
| [R5](#r5-service-principal-creation) | Infrastructure | Service Principal Creation | ✅ Complete | Task 1 |
| [R6](#r6-production-deployment-config-flow) | Infrastructure | Production Deployment Config Flow | ✅ Complete | Task 1, 4 |
| [R7](#r7-websocket-authentication) | Backend | WebSocket Authentication | ✅ Complete | Task 2 |
| [R8](#r8-cors-with-credentials) | Backend | CORS with Credentials | ✅ Complete | Task 2 |
| [R9](#r9-microsoftidentityweb-version-compatibility) | Backend | Microsoft.Identity.Web Version Compatibility | ✅ Complete | Task 2 |
| [R10](#r10-appsettingsjson-azuread-schema) | Backend | `appsettings.json` AzureAd Schema | ✅ Complete | Task 2 |
| [R11](#r11-middleware-ordering-relative-to-websockets) | Backend | Middleware Ordering Relative to WebSockets | ✅ Complete | Task 2 |
| [R12](#r12-apistatus-endpoint-auth-decision) | Backend | `/api/status` Endpoint Auth Decision | ✅ Complete | Task 2, 3 |
| [R13](#r13-cors-production-origin-allowlist) | Backend | CORS Production Origin Allowlist | ✅ Complete | Task 2 |
| [R14](#r14-filesettingsservice--settings-endpoints) | Backend | `FileSettingsService` / Settings Endpoints | ✅ Complete | — |
| [R15](#r15-configuration-injection-for-spa-client-ids) | Frontend | Configuration Injection for SPA Client IDs | ✅ Complete | Task 3 |
| [R16](#r16-ux-for-unauthenticated-users) | Frontend | UX for Unauthenticated Users | ✅ Complete | Task 3 |
| [R17](#r17-token-refresh-and-error-handling) | Frontend | Token Refresh and Error Handling | ✅ Complete | Task 3 |
| [R18](#r18-msal-package-version-compatibility) | Frontend | MSAL Package Version Compatibility | ✅ Complete | Task 3 |
| [R19](#r19-api-clientts-token-injection-architecture) | Frontend | `api-client.ts` Token Injection Architecture | ✅ Complete | Task 3 |
| [R20](#r20-transcriptionstream-token-lifecycle) | Frontend | `TranscriptionStream` Token Lifecycle | ✅ Complete | Task 3 |
| [R21](#r21-net-aspire-entra-id-integration-support) | Aspire | .NET Aspire Entra ID Integration Support | ✅ Complete | Task 4 |
| [R22](#r22-local-dev-credential-flow) | Aspire | Local Dev Credential Flow | ✅ Complete | Task 4 |
| [R23](#r23-integration-test-auth-mocking-pattern) | Testing | Integration Test Auth Mocking Pattern | ✅ Complete | Task 5 |
| [R24](#r24-frontend-msal-mocking-for-vitest) | Testing | Frontend MSAL Mocking for Vitest | ✅ Complete | Task 5 |
| [R25](#r25-existing-unit-test-updates-for-httpcontextuser) | Testing | Existing Unit Test Updates for `HttpContext.User` | ✅ Complete | Task 5 |
| [R26](#r26-integration-test-infrastructure-from-scratch) | Testing | Integration Test Infrastructure From Scratch | ✅ Complete | Task 5 |

---

### Infrastructure / Bicep

#### R1: Graph Bicep Extension Deployment Permissions

**Status:** ✅ Complete | **Blocks:** Task 1

**Background:** The Bicep deployment uses the Microsoft Graph extension to create Entra ID app registrations. We need to know what permissions the deploying principal (developer or CI/CD service principal) must have.

**Finding:** `azd up` does **NOT** automatically grant `Application.ReadWrite.All`. The deploying principal must already have this Microsoft Graph API permission pre-granted via the Entra admin center.

**Recommended approach:**

- For **developer local deployments**: The developer's own Entra identity needs `Application.ReadWrite.All` (delegated). This is a privileged permission — requires an Entra admin to consent.
- For **CI/CD pipelines**: The service principal used by `azd` needs `Application.ReadWrite.OwnedBy` (application permission, least privilege) or `Application.ReadWrite.All`. Must be granted admin consent.
- **Pre-deployment step**: Add to `QUICKSTART.md` — run `az ad app permission add` and `az ad app permission admin-consent` before first `azd up`.

**Risk mitigation:** Document this as a one-time setup step. If the deploying identity lacks permissions, the Bicep deployment will fail with a clear 403 error on the `Microsoft.Graph/applications` resource.

---

#### R2: Circular Reference Between SPA and API App Registrations

**Status:** ✅ Complete | **Blocks:** Task 1

**Background:** The SPA app registration needs to reference the API app's `appId` (for `requiredResourceAccess`), and the API app registration needs the SPA's `appId` (for `preAuthorizedApplications`). This creates a potential circular dependency in Bicep.

**Finding:** You **can** reference the API app's `appId` from the SPA app within the same Bicep template. The `appId` is a read-only output property on `Microsoft.Graph/applications@v1.0` that is available after the resource is created. Bicep handles the dependency ordering automatically.

**Recommended approach:**

```bicep
// 1. Create API app first
resource apiApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: 'prompt-babbler-api'
  displayName: '${environmentName}-prompt-babbler-api'
  signInAudience: 'AzureADMyOrg'
  api: {
    requestedAccessTokenVersion: 2
    oauth2PermissionScopes: [
      {
        id: accessAsUserScopeId  // Use a Bicep variable: var accessAsUserScopeId = guid('prompt-babbler-access-as-user')
        value: 'access_as_user'
        type: 'User'
        adminConsentDisplayName: 'Access Prompt Babbler API'
        adminConsentDescription: 'Allow the app to access Prompt Babbler API on behalf of the signed-in user.'
        userConsentDisplayName: 'Access Prompt Babbler API'
        userConsentDescription: 'Allow the app to access Prompt Babbler API on your behalf.'
        isEnabled: true
      }
    ]
  }
}

// 2. Create SPA app — references apiApp.appId (Bicep resolves dependency)
resource spaApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: 'prompt-babbler-spa'
  displayName: '${environmentName}-prompt-babbler-spa'
  signInAudience: 'AzureADMyOrg'
  spa: {
    redirectUris: [
      'http://localhost:5173'
      spaRedirectUri  // Production URL
    ]
  }
  requiredResourceAccess: [
    {
      resourceAppId: apiApp.appId  // ← Reference works within same template
      resourceAccess: [
        {
          id: accessAsUserScopeId  // Must match the scope GUID defined on the API app
          type: 'Scope'
        }
      ]
    }
  ]
}

// 3. Set preAuthorizedApplications on the API — references spaApp.appId
// NOTE: This may require a separate resource or update, to be tested
```

**Remaining risk:** The `preAuthorizedApplications` on the API app referencing the SPA's `appId` in the same template needs practical validation. If it doesn't work in a single pass, use a Bicep module with `dependsOn` or a post-deployment script.

---

#### R3: Graph Bicep Extension Maturity & Compatibility

**Status:** ✅ Complete | **Blocks:** Task 1

**Background:** The Microsoft Graph Bicep extension is relatively new. We need to confirm it is available in our project and understand its maturity level.

**Finding:** The existing `infra/bicepconfig.json` **already enables** the Microsoft Graph extension with key `microsoftGraphV1` at version `0.2.0-preview`. No `bicepconfig.json` changes are required.

**Current configuration in `infra/bicepconfig.json`:**

```json
{
  "experimentalFeaturesEnabled": {},
  "extensions": {
    "microsoftGraphV1": "br:mcr.microsoft.com/bicep/extensions/microsoftgraph/v1.0:0.2.0-preview"
  }
}
```

> **Note:** The `experimentalFeaturesEnabled.extensibility` flag is **not** required for provider-based extensions in current Bicep CLI versions. The `extensions` key is sufficient.

**Key considerations:**

- The extension is in **preview** (`0.2.0-preview` as of March 2026) — expect potential breaking changes
- Works with `azd` — the framework passes through Bicep extensions
- The `-preview` suffix means GA may change the API surface
- The extension key is `microsoftGraphV1` (not `microsoftGraphV1_0`)

---

#### R4: `identifierUris` Self-Reference

**Status:** ✅ Complete | **Blocks:** Task 1

**Background:** App registrations conventionally use `api://{appId}` as the identifier URI, but the `appId` is assigned at creation time. This creates a self-reference problem in Bicep where you cannot use the `appId` in the same resource definition.

**Finding:** The `appId` property is **read-only** and only available after creation. You **cannot** self-reference `appId` in `identifierUris` within the same resource definition.

**Recommended approach — three options:**

1. **Option A (Recommended): Use `uniqueName` as the identifier URI prefix** — Instead of `api://{appId}`, use a well-known URI like `api://prompt-babbler-api`. This is fully supported and avoids the self-reference problem.
1. **Option B: Two-pass deployment** — Create the app registration without `identifierUris`, then use a second Bicep resource or script to update it with `api://{appId}`. More complex but follows the traditional convention.
1. **Option C: Parameter-based** — Accept the API app's client ID as a Bicep parameter (set after first deployment). Requires `azd` parameter state or manual input.

**Recommendation:** Use **Option A** — a well-known URI like `api://prompt-babbler-api` is simpler, avoids circular dependencies, and is fully supported by Entra ID. The `api://{appId}` convention is common but not required.

---

#### R5: Service Principal Creation

**Status:** ✅ Complete | **Blocks:** Task 1

**Background:** App registrations and service principals are separate objects in Entra ID. We need to confirm whether creating the application automatically creates a service principal.

**Finding:** Creating a `Microsoft.Graph/applications@v1.0` resource does **NOT** automatically create a service principal. You **must** explicitly create a `Microsoft.Graph/servicePrincipals@v1.0` resource for each application.

**Recommended approach:**

```bicep
resource apiServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: apiApp.appId
}

resource spaServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: spaApp.appId
}
```

Both service principals must be created to enable token issuance and consent flows.

---

#### R6: Production Deployment Config Flow

**Status:** ✅ Complete | **Blocks:** Task 1, Task 4

**Background:** The Prompt Babbler project uses `azd up` to orchestrate deployments with Bicep infrastructure. Unauthenticated local dev works via Aspire's `WithEnvironment()`, but production requires auth configuration to reach deployed Container App (API) and Static Web App (frontend) resources. This research resolves: (1) how Entra ID app registration outputs wire to the deployed API, (2) how the SPA gets MSAL client configuration at build time, (3) the Static Web App redirect URI chicken-and-egg problem, and (4) feasibility of single-pass `azd up` orchestration.

**Q1: Bicep Output → Container App Environment Variable Wiring**

The current `infra/main.bicep` already demonstrates the env var wiring pattern. The Container App module receives `env[]` array with hardcoded values from other resource outputs (e.g., `applicationInsights.outputs.connectionString`, `foundryService.outputs.endpoint`).

**Recommended approach for auth config:** After creating the API and SPA app registrations via Microsoft.Graph resources, wire outputs directly into the Container App env vars:

```bicep
module containerApp 'br/public:avm/res/app/container-app:0.12.0' = {
  params: {
    containers: [
      {
        env: [
          // ... existing env vars ...
          { name: 'AzureAd__ClientId', value: apiApp.appId }
          { name: 'AzureAd__TenantId', value: tenant().tenantId }
          { name: 'AzureAd__Instance', value: 'https://login.microsoftonline.com/' }
        ]
      }
    ]
  }
}
```

Add Bicep outputs for downstream consumption:

```bicep
output AZURE_AD_API_CLIENT_ID string = apiApp.appId
output AZURE_AD_SPA_CLIENT_ID string = spaApp.appId
output AZURE_AD_TENANT_ID string = tenant().tenantId
```

**Risk:** **LOW** — Same pattern as existing `ApplicationInsights` and `Foundry` config injection. No new Bicep features required.

**Q2: Static Web App Gets MSAL Config at Build Time**

The current `vite.config.ts` uses Aspire's service discovery pattern with `process.env.services__api__https__0` to inject `__API_BASE_URL__` at build time. The same pattern works for MSAL configuration.

**Recommended approach:**

1. **Extend `vite.config.ts`** with MSAL env vars:

```typescript
export default defineConfig({
  define: {
    __API_BASE_URL__: JSON.stringify(apiBaseUrl),
    __MSAL_CLIENT_ID__: JSON.stringify(process.env.MSAL_CLIENT_ID ?? ''),
    __MSAL_TENANT_ID__: JSON.stringify(process.env.MSAL_TENANT_ID ?? ''),
  },
})
```

1. **Extend `AppHost.cs`** (local dev):

```csharp
builder.AddViteApp("frontend", "../../../../prompt-babbler-app", "dev")
    .WithEnvironment("MSAL_CLIENT_ID", builder.Configuration["EntraAuth:SpaClientId"])
    .WithEnvironment("MSAL_TENANT_ID", builder.Configuration["Azure:TenantId"])
    // ... existing config
```

1. **For production (`azd up`):** Use `azd` hooks (see Q4 below) to inject Bicep outputs as env vars before the frontend build.

**Risk:** **LOW** — Build-time injection is standard. Client ID and tenant ID are public values for SPAs.

**Q3: SPA Redirect URI Chicken-and-Egg Problem**

Static Web App's default hostname (`*.azurestaticapps.net`) is only allocated after the resource is deployed. However, the SPA app registration's `spa.redirectUris` must be defined during the Bicep deployment.

**Recommended approach — deterministic hostname computation (Solution B):**

Since Azure Static Web App hostnames follow a deterministic pattern based on the resource name, and `staticWebAppName = '${abbrs.webStaticSites}${environmentName}'` is known at Bicep compile time, compute the redirect URI in Bicep before the app registration:

```bicep
var spaProductionRedirectUri = 'https://${staticWebAppName}.azurestaticapps.net'

resource spaApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: 'prompt-babbler-spa'
  spa: {
    redirectUris: [
      'http://localhost:5173'        // Local dev
      spaProductionRedirectUri       // Production (computed)
    ]
  }
}
```

> **Note:** Azure Static Web App hostnames may include a random hash suffix (e.g., `swa-abc123.azurestaticapps.net`). If deterministic naming proves unreliable, fall back to a **post-deployment script** via `azd` hooks:
>
> ```bash
> spaDomain=$(az staticwebapp show --name $AZURE_STATIC_WEB_APP_NAME --query "defaultHostname" -o tsv)
> az ad app update --id $SPA_CLIENT_ID --spa-redirect-uris "https://$spaDomain" "http://localhost:5173"
> ```

**Risk:** **MEDIUM** — Depends on Azure's naming stability. Mitigation: provide the post-deployment script as a fallback, and document that custom domains eliminate this concern entirely.

**Q4: Full `azd up` Sequence Feasibility**

Yes, **single-pass `azd up` is feasible** with `azd` hooks for the frontend build step.

**Extended deployment sequence:**

| Phase | Step | Tool | Output |
|-------|------|------|--------|
| **Provision** | Deploy Bicep (app regs + infra) | `azd provision` | `AZURE_AD_SPA_CLIENT_ID`, `AZURE_AD_API_CLIENT_ID`, `AZURE_AD_TENANT_ID`, `AZURE_CONTAINER_APP_FQDN` |
| **Post-provision** | Inject auth config into frontend build | `azd` hook | `MSAL_CLIENT_ID`, `MSAL_TENANT_ID` env vars |
| **Deploy** | Push container image + SWA artifacts | `azd deploy` | Live resources |

**`azd` hook implementation (`.azure/hooks/postprovision.ps1`):**

```powershell
# After Bicep deployment, inject auth config env vars for frontend build
$env:MSAL_CLIENT_ID = (azd env get-value AZURE_AD_SPA_CLIENT_ID)
$env:MSAL_TENANT_ID = (azd env get-value AZURE_AD_TENANT_ID)
$env:API_BASE_URL = "https://$(azd env get-value AZURE_CONTAINER_APP_FQDN)"

Push-Location "prompt-babbler-app"
pnpm install
pnpm build
Pop-Location
```

**Key risk:** The `azd` hooks mechanism requires creating `.azure/hooks/` directory and scripts. Platform-specific scripts (PowerShell for Windows, Bash for CI/Linux) add maintenance overhead.

**Alternative (if hooks prove fragile):** Split deployment into `azd provision` + manual `pnpm build` + `azd deploy`.

**Overall risk:** **MEDIUM** — Single-pass is achievable but requires careful hook scripting.

**Summary:**

| Question | Answer | Risk |
|----------|--------|------|
| Q1: Backend auth config wiring | Bicep outputs → Container App env vars (same existing pattern) | **LOW** |
| Q2: Frontend MSAL config at build time | Vite `define` + env vars (extends existing `__API_BASE_URL__` pattern) | **LOW** |
| Q3: SPA redirect URI sequencing | Deterministic hostname computation in Bicep + post-deploy script fallback | **MEDIUM** |
| Q4: Single-pass `azd up` feasibility | Feasible with `azd` hooks for frontend build; hooks add platform-specific complexity | **MEDIUM** |

---

### Backend / API

#### R7: WebSocket Authentication

**Status:** ✅ Complete | **Blocks:** Task 2

**Background:** JWT Bearer middleware does not automatically authenticate WebSocket upgrade requests because the browser's WebSocket API does not support custom headers. We need a mechanism to pass and validate tokens for the transcription WebSocket endpoint.

**Finding:** The recommended pattern is the **query string token approach** (`?access_token=...`), which is the same pattern used by ASP.NET Core SignalR. Use ASP.NET Core's `JwtBearerEvents.OnMessageReceived` to hook into the auth pipeline. This integrates cleanly with the existing JWT Bearer middleware — no duplicate validation logic needed.

1. Frontend passes the access token as a query parameter when opening the WebSocket: `wss://host/api/transcribe/stream?access_token={token}`
1. The `OnMessageReceived` event extracts the token from the query string before middleware validation
1. The standard `[Authorize]` attribute on the controller handles the rest — no manual token validation in the controller

**Code pattern — `Program.cs` JWT configuration:**

```csharp
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/transcribe"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
```

With this in place, `TranscriptionWebSocketController` only needs the `[Authorize]` attribute — the existing method signature (`[HttpGet("stream")]`, `[FromQuery] string? language`) does not change. User identity is available via `HttpContext.User` as usual.

---

#### R8: CORS with Credentials

**Status:** ✅ Complete | **Blocks:** Task 2

**Background:** Adding Bearer token authentication might require changes to the CORS policy if the `Authorization` header or credentials need to pass through cross-origin requests.

**Finding:** **No CORS changes needed** for the credential/header type:

- Bearer tokens sent via `Authorization` header do **not** require `AllowCredentials()` in CORS
- `AllowCredentials()` is already present and is harmless for Bearer token flows
- It remains helpful for WebSocket connections
- Keep the current CORS policy as-is

> **Note:** This finding is specific to the *credential type*. See [R13](#r13-cors-production-origin-allowlist) for the separate question of production origin allowlisting.

---

#### R9: Microsoft.Identity.Web Version Compatibility

**Status:** ✅ Complete | **Blocks:** Task 2

**Background:** We need to confirm that the latest `Microsoft.Identity.Web` package is compatible with the project's .NET 10 target framework and Aspire 13.1 packages.

**Finding:** **Microsoft.Identity.Web 4.5.0** is fully compatible with .NET 10 and Aspire 13.1.

| Package | Version | Compatibility |
|---------|---------|---------------|
| Microsoft.Identity.Web | 4.5.0 (latest) | .NET 8+ (forward-compatible with .NET 10) |
| Microsoft.Identity.Web.UI | 4.5.0 | Optional (for server-side UI — not needed for API-only) |

**No conflicts** with existing Aspire 13.1 packages, Azure SDK packages, or Microsoft.Extensions.AI packages.

**Add to `Directory.Packages.props`:**

```xml
<PackageVersion Include="Microsoft.Identity.Web" Version="4.5.0" />
```

---

#### R10: `appsettings.json` AzureAd Schema

**Status:** ✅ Complete | **Blocks:** Task 2

**Background:** Task 2 requires adding an `AzureAd` configuration section to `appsettings.json`. `AddMicrosoftIdentityWebApi` expects specific key names and values. The exact schema needs to be documented to avoid trial-and-error during implementation.

**Finding:** The required JSON schema for `Microsoft.Identity.Web`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<from-config>",
    "ClientId": "<api-app-client-id>",
    "Audience": "api://prompt-babbler-api",
    "Scopes": "access_as_user"
  }
}
```

**Key points:**

- `Audience` must match the `identifierUris` decision (`api://prompt-babbler-api` per [R4](#r4-identifieruris-self-reference))
- `Scopes` is the scope *name* (not the full URI)
- In local dev, `TenantId` and `ClientId` come from Aspire env vars which override `appsettings.json` via the `AzureAd__` prefix

---

#### R11: Middleware Ordering Relative to WebSockets

**Status:** ✅ Complete | **Blocks:** Task 2

**Background:** The current `Program.cs` pipeline is `UseExceptionHandler → UseCors → UseWebSockets → MapControllers`. Adding `UseAuthentication` and `UseAuthorization` requires careful placement relative to the existing `UseWebSockets` middleware.

**Finding:** Auth middleware should go **after** `UseWebSockets` and before `MapControllers`:

```
UseExceptionHandler → UseCors → UseWebSockets → UseAuthentication → UseAuthorization → MapControllers
```

This ensures the WebSocket middleware accepts the upgrade, then auth runs on the upgraded request via the `OnMessageReceived` event. If auth runs *before* `UseWebSockets`, the WebSocket upgrade hasn't happened yet and the `OnMessageReceived` approach may not receive the query string token correctly.

---

#### R12: `/api/status` Endpoint Auth Decision

**Status:** ✅ Complete | **Blocks:** Task 2, Task 3

**Background:** The `StatusController` serves `/api/status` which is called by the `useSettings` hook on the SettingsPage. We need to decide whether this endpoint requires authentication, which affects both the backend `[Authorize]`/`[AllowAnonymous]` attribute and the frontend page wrapping.

**Finding:** Keep `StatusController` as `[AllowAnonymous]`:

- The SettingsPage should be accessible to unauthenticated users — it shows backend connection status which is useful for debugging without login
- `useSettings` can fire regardless of auth state — no changes needed to the hook
- The `SettingsPage` should be **outside** `AuthenticatedTemplate` wrapping

This is straightforward but should be explicitly documented so the implementer doesn't accidentally wrap SettingsPage in auth guards.

---

#### R13: CORS Production Origin Allowlist

**Status:** ✅ Complete | **Blocks:** Task 2

**Background:** The current CORS policy in `Program.cs` only allows `localhost` and `127.0.0.1` origins, sufficient for local development. In production, the React SPA deployed to Azure Static Web App (e.g., `https://<name>.azurestaticapps.net`) calls the Container App API. Without updating the CORS policy, the browser will block cross-origin requests and the application will fail.

**Finding:** Use **app-level CORS (ASP.NET Core middleware)** with environment-based configuration. Azure Container Apps does not support CORS policy at the ingress level — ingress only handles routing, TLS termination, and rate limiting. Therefore, CORS must remain in the application code. The hostname injection pattern already exists in the codebase (used for other env vars like `Speech:Region` and `Azure__TenantId`).

**Recommended approach:**

**1. Update Bicep (infra/main.bicep) — pass Static Web App hostname to Container App env:**

```bicep
// In the Container App module, add the Static Web App hostname to the env array:
containers: [
  {
    name: 'api'
    image: '...'
    env: [
      // ... existing env vars ...
      {
        name: 'CORS__AllowedOrigins'
        value: 'https://${staticWebApp.outputs.defaultHostname}'
      }
    ]
  }
]
```

**2. Update Program.cs — make CORS policy environment-aware:**

```csharp
var corsAllowedOrigins = builder.Configuration["CORS:AllowedOrigins"] ?? "";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Always allow localhost for local development
        var isLocalOrigin = (string origin) =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Host is "localhost" or "127.0.0.1";

        if (string.IsNullOrEmpty(corsAllowedOrigins))
        {
            // Local dev: allow localhost only
            policy.SetIsOriginAllowed(isLocalOrigin)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Production: allow configured origins + localhost
            var allowedOrigins = corsAllowedOrigins
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .ToArray();

            policy.SetIsOriginAllowed(origin =>
                    isLocalOrigin(origin) || allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});
```

**Key decisions:**

| Question | Decision | Rationale |
|----------|----------|-----------|
| **Where to implement CORS?** | App-level (ASP.NET Core middleware) | Container Apps ingress doesn't support CORS headers; would require API Management for ingress-level CORS |
| **How to pass production origin?** | Environment variable from Bicep (`CORS__AllowedOrigins`) | Static Web App hostname is only known after deployment; env var injection is the standard pattern in this codebase |
| **Custom domain support now?** | Deferred | Not required for MVP; Static Web App provides a free `*.azurestaticapps.net` hostname. Future custom domains can be added to the env var as comma-separated list |
| **Wildcard origins?** | Not recommended | `*.azurestaticapps.net` is less secure than explicit hostname; use explicit allowlist |

**Implementation notes:**

- The pattern follows the existing `Speech:Region` and `Azure__TenantId` env var injection approach in AppHost.cs / Bicep
- `AllowCredentials()` remains active (required for WebSocket connections per [R8](#r8-cors-with-credentials))
- The Bearer token `Authorization` header works with this CORS policy (no additional changes needed)
- No changes required to: `TranscriptionWebSocketController`, frontend `api-client.ts`, or the middleware ordering from [R11](#r11-middleware-ordering-relative-to-websockets)
- For Aspire local dev, no changes needed — `CORS:AllowedOrigins` is empty, so the localhost-only policy applies automatically

---

#### R14: `FileSettingsService` / Settings Endpoints

**Status:** ✅ Complete (Out of Scope) | **Blocks:** —

**Background:** The backend persists LLM settings in `~/.prompt-babbler/settings.json` via `FileSettingsService`. If there is a settings API endpoint, it would need authentication.

**Finding:** No `SettingsController` exists in the API — settings are currently local-only (frontend `useSettings` only calls `/api/status`). **No action needed** unless a settings endpoint is added as part of this work.

---

### Frontend / MSAL

#### R15: Configuration Injection for SPA Client IDs

**Status:** ✅ Complete | **Blocks:** Task 3

**Background:** The React SPA needs the MSAL client ID and tenant ID at runtime. We need to determine the best mechanism to inject these values, especially given the existing Vite build-time configuration pattern.

**Finding:** Use a **combination approach** — Vite `define` for build-time injection (matching the existing `__API_BASE_URL__` pattern) with Aspire environment variables.

**Implementation:**

```typescript
// vite.config.ts — add alongside existing API_BASE_URL
const msalClientId = process.env.MSAL_CLIENT_ID ?? '';
const msalTenantId = process.env.MSAL_TENANT_ID ?? '';

export default defineConfig({
  // ...
  define: {
    __API_BASE_URL__: JSON.stringify(apiBaseUrl),
    __MSAL_CLIENT_ID__: JSON.stringify(msalClientId),
    __MSAL_TENANT_ID__: JSON.stringify(msalTenantId),
  },
});
```

```csharp
// AppHost.cs — add to frontend Vite app
builder.AddViteApp("frontend", "../../../../prompt-babbler-app", "dev")
    .WithEnvironment("MSAL_CLIENT_ID", spaClientId)
    .WithEnvironment("MSAL_TENANT_ID", builder.Configuration["Azure:TenantId"])
    // ... existing config
```

**Why this is best:**

- Matches the existing `__API_BASE_URL__` pattern — consistent with codebase conventions
- Works during local dev (Aspire injects env vars into Vite dev server)
- Client ID and tenant ID are public values for SPAs — no secrets exposed
- No need for `.env` files or a runtime config endpoint
- TypeScript `declare const` provides type safety

---

#### R16: UX for Unauthenticated Users

**Status:** ✅ Complete | **Blocks:** Task 3

**Background:** With authentication required, we need a UX pattern for users who haven't signed in yet. All pages are data-driven (require API calls), so they can't function without auth.

**Finding:** Use a **degraded view with in-context sign-in prompt.** Show the UI shell with a sign-in prompt instead of empty/broken content.

**Recommended pattern:**

```tsx
// In pages that require data
<AuthenticatedTemplate>
  {/* Full functionality */}
  <BabbleList babbles={babbles} />
</AuthenticatedTemplate>

<UnauthenticatedTemplate>
  {/* Degraded view with sign-in prompt */}
  <EmptyState
    icon={<Lock />}
    title="Sign in to continue"
    description="Sign in with your organizational account to record babbles and generate prompts."
    action={<LoginButton />}
  />
</UnauthenticatedTemplate>
```

**Key decisions:**

- HomePage shows layout shell + sign-in prompt (not blank)
- Navigation remains visible (user can explore structure)
- No automatic redirect to Entra login — user initiates sign-in
- `StatusController` (`/api/status`) remains anonymous (health check)
- All data hooks (`useBabbles`, `useTemplates`) skip API calls when unauthenticated

---

#### R17: Token Refresh and Error Handling

**Status:** ✅ Complete | **Blocks:** Task 3

**Background:** MSAL tokens expire and need to be refreshed. API calls and WebSocket connections both need valid tokens. We need a centralized pattern for token acquisition, refresh, and error handling.

**Finding:** Use a **custom `useAuthenticatedFetch` hook** that wraps `acquireTokenSilent` + fetch. This centralizes token acquisition and error handling without requiring a global fetch interceptor.

> **Note:** [R19](#r19-api-clientts-token-injection-architecture) refines this approach. Instead of a standalone `useAuthenticatedFetch` hook, R19 recommends adding an optional `accessToken` parameter to each `api-client.ts` function. Each consuming hook (e.g., `useTemplates`, `usePromptGeneration`) acquires the token via its own inline `getAuthToken()` helper using `useMsal()`. The core token acquisition and error handling pattern below remains the same — only the wrapping changes. **Prefer the R19 pattern for implementation.**

**Recommended pattern:**

```typescript
// hooks/useAuthenticatedFetch.ts
export function useAuthenticatedFetch() {
  const { instance, accounts } = useMsal();

  return useCallback(async function authFetch<T>(
    path: string,
    init?: RequestInit
  ): Promise<T> {
    const request = {
      scopes: ['api://prompt-babbler-api/access_as_user'],
      account: accounts[0],
    };

    try {
      const response = await instance.acquireTokenSilent(request);
      const res = await fetch(`${getApiBaseUrl()}${path}`, {
        ...init,
        headers: {
          Authorization: `Bearer ${response.accessToken}`,
          'Content-Type': 'application/json',
          ...init?.headers,
        },
      });
      if (!res.ok) throw new Error(`API error ${res.status}`);
      return res.json() as Promise<T>;
    } catch (err) {
      if (err instanceof InteractionRequiredAuthError) {
        // Trigger interactive login
        await instance.acquireTokenPopup(request);
        // Retry... or let error boundary handle
      }
      throw err;
    }
  }, [instance, accounts]);
}
```

**For the WebSocket client**, extract a token separately before connecting:

```typescript
const tokenResponse = await instance.acquireTokenSilent(request);
const ws = new WebSocket(
  `${wsBase}/api/transcribe/stream?access_token=${tokenResponse.accessToken}`
);
```

---

#### R18: MSAL Package Version Compatibility

**Status:** ✅ Complete | **Blocks:** Task 3

**Background:** The frontend uses React 19.2.0. We need to confirm that the MSAL packages are compatible.

**Finding:** Both packages are **fully compatible with React 19**.

| Package | Recommended Version | React Compat |
|---------|-------------------|--------------|
| `@azure/msal-browser` | `^4.5.0` | React 16.8+ |
| `@azure/msal-react` | `^2.1.0` | React 16.8+ |

**Current app:** React 19.2.0 — no known compatibility issues.

**Install command:**

```bash
pnpm add @azure/msal-browser@^4.5.0 @azure/msal-react@^2.1.0
```

No breaking changes between 4.x minor versions. `@azure/msal-react` requires `@azure/msal-browser` as a peer dependency (auto-resolved by pnpm).

---

#### R19: `api-client.ts` Token Injection Architecture

**Status:** ✅ Complete | **Blocks:** Task 3

**Background:** The current `api-client.ts` is a **pure module of standalone exported functions** (`getTemplates()`, `generatePrompt()`, etc.) — not a class or hook. The research proposes a `useAuthenticatedFetch` React hook ([R17](#r17-token-refresh-and-error-handling)), but doesn't explain how to bridge the gap between the existing function-based API module and the hook-based auth pattern. Additionally, `generatePrompt()` returns a `ReadableStream<Uint8Array>` for SSE streaming and `deleteTemplate()` uses raw `fetch` (not `fetchJson`) — the token injection pattern must work for all three response types (JSON, void, and streaming).

**Finding: Option A — Optional `accessToken` Parameter (Recommended)**

Three options were evaluated:

| Option | Approach | Verdict |
|--------|----------|---------|
| **A: Optional `accessToken` parameter** | Each function accepts `accessToken?: string`; hooks acquire and pass token | ✅ **Recommended** |
| **B: Replace with hook** | Remove `api-client.ts`; consuming hooks call generic `useAuthenticatedFetch` | ❌ Breaking change, loses API clarity, can't call outside React |
| **C: Module-level initializer** | `configureApiClient(getToken)` sets global state | ❌ Anti-pattern (global mutable state), hard to test |

**Rationale for Option A:**

1. **Minimal refactoring** — each function gains one optional parameter; no structural changes
1. **Explicit** — function signatures show which calls need tokens
1. **Portable** — functions remain callable outside React (scripts, tests, docs)
1. **Works with all response types** — JSON, void, and streaming use the same pattern
1. **Backward compatible** — `getStatus()` remains parameterless (unauthenticated)

**Implementation pattern:**

**Shared utility:**

```typescript
function addAuthHeader(headers: HeadersInit = {}, accessToken?: string): HeadersInit {
  if (!accessToken) return headers;
  return { ...headers, Authorization: `Bearer ${accessToken}` };
}
```

**JSON response (`getTemplates`, `createTemplate`, `updateTemplate`):**

```typescript
export async function getTemplates(forceRefresh = false, accessToken?: string): Promise<PromptTemplate[]> {
  const query = forceRefresh ? '?forceRefresh=true' : '';
  return fetchJson<PromptTemplate[]>(`/api/templates${query}`, undefined, accessToken);
}
```

**Void response (`deleteTemplate`):**

```typescript
export async function deleteTemplate(id: string, accessToken?: string): Promise<void> {
  const base = getApiBaseUrl();
  const res = await fetch(`${base}/api/templates/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`API error ${res.status}: ${text}`);
  }
}
```

**Streaming response (`generatePrompt`):**

```typescript
export async function generatePrompt(
  babbleText: string, templateId: string,
  promptFormat: string = 'text', allowEmojis: boolean = false,
  accessToken?: string
): Promise<ReadableStream<Uint8Array>> {
  const base = getApiBaseUrl();
  const res = await fetch(`${base}/api/prompts/generate`, {
    method: 'POST',
    headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
    body: JSON.stringify({ babbleText, templateId, promptFormat, allowEmojis }),
  });
  if (!res.ok) { throw new Error(`Generation error ${res.status}`); }
  if (!res.body) { throw new Error('No response body for streaming'); }
  return res.body;
}
```

**Unauthenticated function (`getStatus` — NO CHANGES):**

```typescript
export async function getStatus(): Promise<StatusResponse> {
  return fetchJson<StatusResponse>('/api/status');  // No token parameter
}
```

**Consuming hook pattern (e.g., `useTemplates`):**

Each consuming hook gains a `getAuthToken()` helper via `useMsal()`:

```typescript
const { instance, accounts } = useMsal();

const getAuthToken = useCallback(async (): Promise<string | undefined> => {
  if (accounts.length === 0) return undefined;
  const response = await instance.acquireTokenSilent({
    scopes: ['api://prompt-babbler-api/access_as_user'],
    account: accounts[0],
  });
  return response.accessToken;
}, [instance, accounts]);

// Usage in hook methods:
const token = await getAuthToken();
const data = await api.getTemplates(forceRefresh, token);
```

**Key design decisions:**

- `getStatus()` remains unauthenticated — no token parameter, no `Authorization` header. The backend `[AllowAnonymous]` StatusController handles it.
- Token acquisition errors (`InteractionRequiredAuthError`) propagate to the calling hook and are handled by an App-level error boundary.
- All three response types (JSON, void, streaming) use the same `addAuthHeader()` mechanism.

**Migration path:**

1. **Phase 1:** Add `addAuthHeader()` utility + `accessToken?: string` to all authenticated functions (backward compatible, no consumers change)
1. **Phase 2:** Update consuming hooks (`useTemplates`, `usePromptGeneration`) to acquire tokens via `useMsal()` and pass to API functions
1. **Phase 3:** Verify `useSettings` still works without changes (calls `getStatus()` with no token)

---

#### R20: `TranscriptionStream` Token Lifecycle

**Status:** ✅ Complete | **Blocks:** Task 3

**Background:** `TranscriptionStream` is a plain TypeScript class (not a React component or hook). It cannot call `useMsal()`. The research confirms that WebSocket auth uses `?access_token={token}` query parameters ([R7](#r7-websocket-authentication)), but the integration pattern for acquiring and refreshing tokens in long-duration recording sessions needs to be designed.

**Finding — Three Design Decisions:**

**1. Token Acquisition Responsibility: `useTranscription` Hook**

The `useTranscription` hook must acquire the MSAL access token because:

- It manages the WebSocket connection lifecycle (via `TranscriptionStream`)
- `useAudioRecording` is purely concerned with audio capture and should not depend on auth
- Only React hooks can call `useMsal()`; `TranscriptionStream` is a plain TS class
- Separation of concerns: audio capture ≠ authentication

**2. Token Passing API: `open(language?: string, accessToken?: string)` Parameter**

Add an `accessToken` parameter to `TranscriptionStream.open()`:

```typescript
open(language?: string, accessToken?: string): void {
  if (this.ws) return;

  const base = getWsBaseUrl();
  const params = new URLSearchParams();
  if (language) params.append('language', language);
  if (accessToken) params.append('access_token', accessToken);

  const queryString = params.toString();
  const url = `${base}/api/transcribe/stream${queryString ? `?${queryString}` : ''}`;

  this.ws = new WebSocket(url);
  // ... rest unchanged ...
}
```

**Rationale:** Matches the backend R7 pattern (`?access_token={token}`). Keeps `TranscriptionStream` as a simple, stateless connector. No constructor injection or module-level state needed.

**3. Token Refresh Strategy: Automatic Reconnect**

MSAL tokens expire after ~1 hour. Recording sessions can exceed this. The browser WebSocket API does not support changing headers or query parameters on an existing connection — a new connection is required.

**Recommended approach: automatic reconnect with fresh token**

| Approach | Pros | Cons | Choice |
|----------|------|------|--------|
| **Automatic reconnect** | Transparent to user; supports long sessions | Requires state tracking; brief interruption during reconnect | ✅ Selected |
| **Manual close/reopen** | Simpler code | Disrupts recording; user must manually resume | Not selected |
| **Accept timeout** | Minimal code | Sessions fail silently after 1 hour; poor UX | Not selected |

**Implementation pattern in `useTranscription`:**

```typescript
export function useTranscription() {
  const { instance, accounts } = useMsal();
  // ... existing state ...
  const tokenRefreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const sessionStateRef = useRef<{ language?: string; isActive: boolean }>({
    language: undefined, isActive: false
  });

  const acquireAccessToken = useCallback(async (): Promise<string | null> => {
    if (!accounts.length) return null;
    try {
      const response = await instance.acquireTokenSilent({
        scopes: ['api://prompt-babbler-api/access_as_user'],
        account: accounts[0],
      });
      return response.accessToken;
    } catch {
      setError('Failed to acquire access token');
      return null;
    }
  }, [instance, accounts]);

  const reconnectWithFreshToken = useCallback(async () => {
    if (!sessionStateRef.current.isActive) return;
    streamRef.current?.close();
    streamRef.current = null;

    const newToken = await acquireAccessToken();
    if (!newToken) {
      setError('Unable to refresh token for continued recording');
      return;
    }

    const stream = new TranscriptionStream(handleMessage, handleError);
    streamRef.current = stream;
    stream.open(sessionStateRef.current.language, newToken);
    setIsConnected(true);

    // Reschedule for the next token expiry cycle
    tokenRefreshTimerRef.current = setTimeout(reconnectWithFreshToken, 55 * 60 * 1000);
  }, [acquireAccessToken, handleMessage, handleError]);

  const connect = useCallback(async (language?: string) => {
    if (streamRef.current) return;
    setError(null);
    sessionStateRef.current = { language, isActive: true };

    const token = await acquireAccessToken();
    if (!token) {
      setError('Failed to acquire access token');
      return;
    }

    const stream = new TranscriptionStream(handleMessage, handleError);
    streamRef.current = stream;
    stream.open(language, token);
    setIsConnected(true);

    // Schedule token refresh 5 minutes before ~1 hour expiry
    tokenRefreshTimerRef.current = setTimeout(reconnectWithFreshToken, 55 * 60 * 1000);
  }, [acquireAccessToken, handleMessage, handleError, reconnectWithFreshToken]);

  const disconnect = useCallback(() => {
    sessionStateRef.current.isActive = false;
    streamRef.current?.close();
    streamRef.current = null;
    setIsConnected(false);
    setPartialText('');
    if (tokenRefreshTimerRef.current) {
      clearTimeout(tokenRefreshTimerRef.current);
      tokenRefreshTimerRef.current = null;
    }
  }, []);

  // ... rest of hook unchanged ...
}
```

**Key design points:**

- `connect()` becomes `async` — acquires token before opening WebSocket
- `sessionStateRef` tracks active recording state and language for automatic reconnection
- Token refresh timer fires 5 minutes before the ~1 hour MSAL expiry
- `reconnectWithFreshToken()` gracefully closes the old WebSocket and opens a new one with a fresh token
- Brief interruption during reconnect is acceptable for transcription (partial results already accumulated)
- If token refresh fails, the user sees an error message and can manually retry

---

### Aspire

#### R21: .NET Aspire Entra ID Integration Support

**Status:** ✅ Complete | **Blocks:** Task 4

**Background:** We need to determine whether .NET Aspire 13.1 has built-in support for Entra ID authentication or whether manual configuration is required.

**Finding:** **Confirmed — Aspire 13.1 has NO built-in Entra ID authentication integration.** No `Aspire.Hosting.Azure.EntraId` package exists. Auth config must be passed manually via `WithEnvironment`.

**Recommended pattern for `AppHost.cs`:**

```csharp
// Read auth config from user secrets (or config)
var spaClientId = builder.Configuration["EntraAuth:SpaClientId"];
var apiClientId = builder.Configuration["EntraAuth:ApiClientId"];
var tenantId = builder.Configuration["Azure:TenantId"];

var apiService = builder.AddProject<Projects.PromptBabbler_Api>("api")
    .WithReference(foundry)
    .WithReference(cosmos)
    .WithEnvironment("AzureAd__ClientId", apiClientId)
    .WithEnvironment("AzureAd__TenantId", tenantId)
    .WithEnvironment("AzureAd__Instance", "https://login.microsoftonline.com/")
    // ... existing config

builder.AddViteApp("frontend", "../../../../prompt-babbler-app", "dev")
    .WithEnvironment("MSAL_CLIENT_ID", spaClientId)
    .WithEnvironment("MSAL_TENANT_ID", tenantId)
    // ... existing config
```

**Store client IDs via user secrets:**

```bash
dotnet user-secrets set "EntraAuth:SpaClientId" "<spa-client-id>" --project src/Orchestration/AppHost
dotnet user-secrets set "EntraAuth:ApiClientId" "<api-client-id>" --project src/Orchestration/AppHost
```

---

#### R22: Local Dev Credential Flow

**Status:** ✅ Complete | **Blocks:** Task 4

**Background:** The API already uses `DefaultAzureCredential` for authenticating to Azure services (AI Foundry, Cosmos DB, Speech). Adding JWT Bearer authentication for user→API requests must not conflict with this existing credential flow.

**Finding:** **No conflict.** The two auth flows are completely independent:

| Concern | Mechanism | Purpose |
|---------|-----------|---------|
| API → Azure services | `DefaultAzureCredential` | Backend authenticates to AI Foundry, Cosmos DB, Speech |
| User → API | JWT Bearer (MSAL tokens) | Frontend user authenticates to the API |

- `DefaultAzureCredential` operates at startup and for outbound calls
- JWT Bearer operates in the middleware pipeline for inbound requests
- They use different token audiences and different credential stores
- **Middleware order is critical** — `UseAuthentication()` + `UseAuthorization()` must be placed before `MapControllers()` but after `UseCors()` (see [R11](#r11-middleware-ordering-relative-to-websockets) for exact ordering)

---

### Testing

#### R23: Integration Test Auth Mocking Pattern

**Status:** ✅ Complete | **Blocks:** Task 5

**Background:** The integration tests use `WebApplicationFactory<Program>`. We need a pattern to mock authentication so tests can make authenticated requests without real Entra ID tokens.

**Finding:** Use the standard **`TestAuthHandler` + `ConfigureTestServices`** pattern.

**Implementation:**

```csharp
// TestAuthHandler.cs
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("oid", "00000000-0000-0000-0000-000000000000"),
            new Claim("preferred_username", "testuser@contoso.com"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestAuth");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// CustomWebApplicationFactory.cs
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication("TestAuth")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuth", _ => { });
        });
    }
}
```

With `"TestAuth"` as the default scheme, `HandleAuthenticateAsync()` always returns `Success` — every request is automatically authenticated as `test-user-id`. To test **401 responses**, either:

- Use a separate `WebApplicationFactory` that does **not** register `TestAuthHandler`, or
- Override `HandleAuthenticateAsync()` to return `AuthenticateResult.NoResult()` for specific tests (e.g., via a static flag or per-test DI override).

---

#### R24: Frontend MSAL Mocking for Vitest

**Status:** ✅ Complete | **Blocks:** Task 5

**Background:** Frontend tests use Vitest. `@azure/msal-react` does not provide built-in test utilities, so we need a mocking strategy.

**Finding:** Use `vi.mock` to mock the `@azure/msal-react` module globally.

**Implementation in `vitest.setup.ts`:**

```typescript
import { vi } from 'vitest';

vi.mock('@azure/msal-react', () => ({
  MsalProvider: ({ children }: { children: React.ReactNode }) => children,
  useMsal: vi.fn(() => ({
    instance: {
      acquireTokenSilent: vi.fn().mockResolvedValue({
        accessToken: 'mock-access-token',
        expiresOn: new Date(Date.now() + 3600000),
      }),
    },
    inProgress: 'none',
    accounts: [{
      homeAccountId: 'test|test-user',
      localAccountId: 'test-user',
      username: 'testuser@contoso.com',
      name: 'Test User',
    }],
  })),
  useIsAuthenticated: vi.fn(() => true),
  AuthenticatedTemplate: ({ children }: { children: React.ReactNode }) => children,
  UnauthenticatedTemplate: () => null,
}));
```

Per-test overrides for unauthenticated scenarios:

```typescript
vi.mocked(useMsal).mockReturnValue({
  instance: {} as any,
  inProgress: 'none',
  accounts: [],
} as any);
```

---

#### R25: Existing Unit Test Updates for `HttpContext.User`

**Status:** ✅ Complete | **Blocks:** Task 5

**Background:** The existing unit tests (e.g., `PromptTemplateControllerTests.cs`) construct controllers with `new DefaultHttpContext()`. After auth is added, controllers will call `User.GetObjectId()` instead of using `AnonymousUserId`. The tests need `HttpContext.User` populated with claims.

**Finding:** Small change per test class — set up claims on `DefaultHttpContext`:

```csharp
var httpContext = new DefaultHttpContext();
httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
{
    new Claim("oid", "test-user-id"),
    new Claim("preferred_username", "test@contoso.com"),
}, "TestAuth"));
```

This affects `PromptTemplateControllerTests`, `PromptControllerTests`, and `TranscriptionControllerTests`. The pattern is identical for each — a helper method or shared fixture can reduce duplication.

---

#### R26: Integration Test Infrastructure From Scratch

**Status:** ✅ Complete | **Blocks:** Task 5

**Background:** The `Api.IntegrationTests` project exists (`.csproj` with test package references: `MSTest.Sdk`, `FluentAssertions`, `NSubstitute`, `Microsoft.AspNetCore.Mvc.Testing`, `coverlet.collector`) but has **zero `.cs` files**. The entire test infrastructure must be created from scratch. The auth mocking pattern from [R23](#r23-integration-test-auth-mocking-pattern) provides the authentication foundation.

**Finding — Five Design Decisions:**

**1. Integration Test Infrastructure: Part of Auth Work (Task 5)**

**Decision:** Create integration test infrastructure as part of the auth work, not as a separate prerequisite. Once Task 2 adds auth middleware, all integration tests must mock auth. Delaying infrastructure past Task 2 leaves the API untested in an integrated context.

**Recommended phasing:**

- **Task 5a (parallel with Task 2):** Create infrastructure files (`TestAuthHandler`, `CustomWebApplicationFactory`, `NoAuthWebApplicationFactory`, fixtures)
- **Task 5b (after Task 2):** Write initial test suites using the infrastructure
- **Phase 3 (backlog):** Replace mocks with real services (Cosmos emulator, etc.)

**2. Initial Test Set: Auth Enforcement + Controller Contracts**

| Endpoint | Test Methods |
|----------|-------------|
| `GET /api/status` | `ReturnsOk`, `DoesNotRequireAuthentication` |
| `GET /api/templates` | `WithAuth_ReturnsOk`, `WithoutAuth_Returns401` |
| `GET /api/templates/{id}` | `ExistingId_ReturnsTemplate`, `NonExistentId_Returns404` |
| `POST /api/templates` | `ValidRequest_Returns201`, `EmptyName_Returns400`, `WithoutAuth_Returns401` |
| `PUT /api/templates/{id}` | `ValidRequest_ReturnsOk`, `BuiltIn_Returns403` |
| `DELETE /api/templates/{id}` | `ExistingId_Returns204`, `BuiltIn_Returns403` |
| `POST /api/prompts/generate` | `WithAuth_ReturnsSSEStream`, `WithoutAuth_Returns401` |
| `GET /api/transcribe/stream` | `WithAuth_AcceptsWebSocket`, `NonWebSocket_Returns400` |

**Scope:** Auth status (authenticated vs unauthenticated), input validation, HTTP status codes, response shape. Do NOT test business logic (covered by unit tests).

**3. Test Data Seeding: Domain Model Fixtures with NSubstitute**

Use fixture builders with NSubstitute mocks — no database seeding, no Docker containers:

```csharp
public static class PromptTemplateFixtures
{
    public const string TestUserId = "00000000-0000-0000-0000-000000000000";

    public static PromptTemplate CreateUserTemplate(
        string userId = TestUserId, string id = "template-user-1",
        string name = "User Template")
    {
        return new PromptTemplate
        {
            Id = id, UserId = userId, Name = name,
            Description = "A test user template",
            SystemPrompt = "You are a helpful assistant.",
            IsBuiltIn = false,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
        };
    }
}
```

Tests configure NSubstitute return values per test case, then make HTTP requests and assert responses.

**4. External Service Dependencies: Mock All Azure Services**

**Strategy:** Replace all external service interfaces in `CustomWebApplicationFactory`:

```csharp
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Auth mocking (from R23)
            services.AddAuthentication("TestAuth")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuth", _ => { });

            // Replace domain services with mocks
            services.AddScoped<IPromptTemplateService>(_ => Substitute.For<IPromptTemplateService>());
            services.AddScoped<IPromptGenerationService>(_ => Substitute.For<IPromptGenerationService>());
            services.AddScoped<IRealtimeTranscriptionService>(_ => Substitute.For<IRealtimeTranscriptionService>());
            services.AddScoped<IChatClient>(_ => Substitute.For<IChatClient>());
        });
    }
}
```

A second factory (`NoAuthWebApplicationFactory`) omits the `TestAuthHandler` to test 401 responses.

**5. File Structure**

```
Api.IntegrationTests/
├── PromptBabbler.Api.IntegrationTests.csproj   (existing, no changes)
├── Infrastructure/
│   ├── TestAuthHandler.cs                      (from R23)
│   ├── CustomWebApplicationFactory.cs          (main factory with test auth + mocked services)
│   └── NoAuthWebApplicationFactory.cs          (factory without auth for 401 tests)
├── Fixtures/
│   └── PromptTemplateFixtures.cs               (test data builders)
├── Controllers/
│   ├── StatusControllerTests.cs
│   ├── PromptTemplateControllerTests.cs
│   ├── PromptControllerTests.cs
│   └── TranscriptionWebSocketControllerTests.cs
```

**Test execution strategy:**

```bash
# Run all integration tests
dotnet test --solution PromptBabbler.slnx --filter "TestCategory=Integration" --configuration Release

# Run specific controller tests
dotnet test --solution PromptBabbler.slnx --filter "TestCategory=Integration&ClassName~PromptTemplate"
```

**Implementation notes:**

- Tests use `[TestCategory("Integration")]` to match the existing `dotnet test --filter` pattern in the workspace tasks
- `TestAuthHandler` produces claims matching `User.GetObjectId()` expectations: `oid` = `00000000-0000-0000-0000-000000000000`
- Tests do NOT validate JWT middleware internals — they verify controller contracts (200/201/400/401/403/404 responses)
- The `CustomWebApplicationFactory` pattern is identical to the existing `Infrastructure.IntegrationTests` and `Orchestration.IntegrationTests` projects (consistent conventions)

---

## Recommendations Summary

### Key Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Identifier URI format | `api://prompt-babbler-api` (well-known) | Avoids `appId` self-reference circular dependency in Bicep |
| WebSocket auth | `OnMessageReceived` event + `?access_token=` query param | Matches SignalR pattern, integrates with JWT pipeline |
| SPA config injection | Vite `define` via Aspire env vars | Matches existing `__API_BASE_URL__` convention |
| Unauthenticated UX | Degraded view with sign-in prompt | Shows app shell, lets user initiate login |
| Token error handling | Optional `accessToken` param in `api-client.ts` functions; per-hook `getAuthToken()` via `useMsal()` (R19) | Minimal refactoring, explicit, portable, works with all response types |
| MSAL versions | `msal-browser@^4.5.0`, `msal-react@^2.1.0` | Stable, React 19 compatible |
| Identity.Web version | `4.5.0` | Latest, .NET 10 + Aspire 13.1 compatible |
| CORS credentials | No changes needed | Current config already correct |
| CORS production origins | App-level CORS with env-based allowlist | Container Apps ingress lacks CORS support |
| Integration test auth | `TestAuthHandler` + `ConfigureTestServices` | Standard ASP.NET Core pattern |
| Frontend test mocking | Global `vi.mock('@azure/msal-react')` | No built-in test utilities; manual mocking required |
| Middleware ordering | `UseCors → UseWebSockets → UseAuth → MapControllers` | Ensures WebSocket upgrade before auth |
| `/api/status` auth | `[AllowAnonymous]` | Useful for debugging without login |
| Production config flow | Bicep outputs → Container App env vars + `azd` hooks | Same pattern as existing infra; single-pass `azd up` feasible |
| API client token injection | Optional `accessToken` parameter (Option A) | Minimal refactoring; explicit; works with all response types |
| WebSocket token lifecycle | `useTranscription` acquires token; auto-reconnect on expiry | Transparent 1hr+ session support |
| Integration test scope | Auth enforcement + controller contracts, not business logic | Part of auth work (Task 5), not separate prerequisite |

### Remaining Risks

1. **Graph Bicep extension is in preview** (`0.2.0-preview`) — potential for breaking changes before GA. Mitigate by pinning the extension version in `bicepconfig.json`.
1. **`preAuthorizedApplications` cross-reference in single Bicep deployment** — needs practical validation. If it fails, fall back to a post-deployment `az ad app update` command.
1. **Deployment permissions** — one-time admin setup required for `Application.ReadWrite.All`. Document clearly in QUICKSTART.md.
1. **SPA redirect URI determinism** — Static Web App hostname may include a random hash suffix. If deterministic naming proves unreliable, use the post-deployment script fallback documented in R6.
1. **`azd` hooks platform specificity** — PostProvision hooks require platform-specific scripts (PowerShell on Windows, Bash on CI/Linux). Document both variants.

## Recommended Implementation Phases

1. **Phase 1 — Infrastructure**: Bicep app registrations (Task 1) + `azd` hooks for production config flow (R6).
1. **Phase 2 — Backend + Frontend Auth**: Tasks 2, 3, 4 in parallel. Backend: JWT auth + CORS production origins (R13). Frontend: MSAL integration with Option A token injection (R19) + WebSocket token lifecycle (R20).
1. **Phase 3 — Testing + Docs**: Tasks 5, 6. Create integration test infrastructure from scratch (R26) + update documentation.

## Research Resources

| Topic | URL | Status |
|-------|-----|--------|
| MSAL React SPA tutorial | <https://learn.microsoft.com/en-us/entra/identity-platform/tutorial-single-page-app-react-prepare-app> | Reviewed |
| SPA code configuration | <https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-app-configuration> | Reviewed |
| SPA call a web API | <https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-call-api> | Reviewed |
| ASP.NET Core web API protection | <https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-web-api-dotnet-protect-app> | Reviewed |
| Build & secure ASP.NET Core web API | <https://docs.azure.cn/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app> | Reviewed |
| Microsoft Graph Bicep `applications` reference | <https://learn.microsoft.com/en-us/graph/templates/bicep/reference/applications> | Reviewed |
| Graph Bicep `servicePrincipals` reference | <https://learn.microsoft.com/en-us/graph/templates/bicep/reference/servicePrincipals> | Reviewed |
| Graph Bicep quickstart | <https://learn.microsoft.com/en-us/graph/templates/quickstart-create-graph-bicep> | Reviewed |
| Azure-Samples MSAL React SPA | <https://github.com/Azure-Samples/ms-identity-docs-code-javascript/tree/main/react-spa> | Reviewed |
| .NET Aspire docs | <https://aspire.dev/docs/> | Reviewed (no auth section) |
| WebSocket auth (SignalR pattern) | <https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz> | Reviewed |
| Microsoft.Identity.Web NuGet | <https://www.nuget.org/packages/Microsoft.Identity.Web> | Reviewed |
