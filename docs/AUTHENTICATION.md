---
title: Authentication
description: Configure anonymous, access code, or Entra ID authentication for Prompt Babbler.
---

Prompt Babbler supports three authentication modes that control how users are identified and how access to the API is protected. The mode you choose depends on whether you are running the application for a single user or multiple users, and how much protection you need.

| Mode | Users | Use case |
| --- | --- | --- |
| Anonymous | Single | Local development, private networks with no access control needed |
| Access Code | Single | Personal deployments — lightweight protection without user accounts |
| Entra ID | Multi | Shared team or organizational deployments requiring individual identity |

## Anonymous mode

Anonymous mode is the default. No environment variables or configuration are required. All API requests are accepted without any access check, and every request is attributed to a single shared identity: `_anonymous`.

All babbles, generated prompts, and templates are stored in Cosmos DB under the `_anonymous` user partition. There is no way to distinguish between different browser sessions or end users.

**When to use:** Local development, a personal instance on a private network, or any scenario where you do not need access control.

### Configure for local development

No configuration is needed. Start the app normally and it runs in anonymous mode.

If a previously set access code is present in your user secrets, remove it:

```bash
cd prompt-babbler-service/src/Api
dotnet user-secrets remove AccessControl:AccessCode
```

Ensure the `ACCESS_CODE` environment variable is not set, then run:

```bash
aspire run
```

### Configure for Azure deployment

Do not set the `ACCESS_CODE` GitHub Actions secret or `azd` environment variable. Ensure no value is present:

```bash
azd env set ACCESS_CODE ""
```

Then deploy as normal:

```bash
azd up
```

---

## Access Code mode

Access Code mode adds a lightweight password gate to a single-user deployment. When an access code is configured:

1. The frontend checks `GET /api/config/access-status` on startup.
1. If an access code is required, a modal prompts for the code before the app loads.
1. The code is stored in browser session storage (cleared when the tab closes).
1. Every API request includes the code in the `X-Access-Code` header.
1. The backend `AccessCodeMiddleware` validates the header using constant-time comparison and returns `401 Unauthorized` if the code is missing or incorrect.

Health and status endpoints (`/health`, `/alive`, `/api/config/access-status`) are exempt from the check and always accessible.

The user identity remains `_anonymous` — this mode protects access to a single-user instance, it does not support multiple users.

> **Security note:** Access Code mode is a lightweight access gate, not a high-security authentication mechanism. For production multi-user deployments, use Entra ID mode.

### Configure for local development

**Option 1 — User secrets (recommended for development)**

```bash
cd prompt-babbler-service/src/Api
dotnet user-secrets set AccessControl:AccessCode "your-access-code"
```

**Option 2 — `appsettings.Development.json`**

Add or update the `AccessControl` section in `prompt-babbler-service/src/Api/appsettings.Development.json`:

```json
{
  "AccessControl": {
    "AccessCode": "your-access-code"
  }
}
```

> Do not commit access codes to source control. Prefer user secrets for local development.

**Option 3 — Environment variable**

On Linux/macOS:

```bash
export ACCESS_CODE="your-access-code"
```

On Windows (PowerShell):

```powershell
$env:ACCESS_CODE = "your-access-code"
```

### Configure for Azure deployment

The access code flows from a GitHub Actions secret into the deployed Container App environment variable.

1. Go to your GitHub repository **Settings** → **Secrets and variables** → **Actions**.
1. Click **New repository secret** (or add to the `prod` environment).
1. Set **Name** to `ACCESS_CODE` and **Value** to your desired access code.

The secret flows through the pipeline into the Bicep `accessCode` parameter → Container App `ACCESS_CODE` environment variable.

Alternatively, set the value in the `azd` environment before deploying:

```bash
azd env set ACCESS_CODE "your-access-code"
azd up
```

To remove access code protection from an existing deployment, clear the value and redeploy:

```bash
azd env set ACCESS_CODE ""
azd provision
```

---

## Entra ID mode

Entra ID mode uses Microsoft Entra ID (formerly Azure Active Directory) to authenticate users. Each user signs in with their organizational account. The user's Entra ID object ID is used as the user partition key in Cosmos DB, so each user's documents and sessions are isolated from all other users.

Two Entra ID app registrations are created automatically by the pre-provision hook:

| Registration | Purpose |
| --- | --- |
| `{env}-prompt-babbler-api` | Represents the backend API; exposes the `access_as_user` OAuth 2.0 scope |
| `{env}-prompt-babbler-spa` | Represents the frontend SPA; acquires tokens for the API scope |

> [!NOTE]
> Entra ID multi-user mode requires the `Application.ReadWrite.All` Microsoft Graph permission to create app registrations during provisioning.

### Configure for local development

Entra ID mode is not supported for local Aspire development. Run in Anonymous mode or Access Code mode locally, and use Entra ID only for deployed Azure environments.

### Configure for Azure deployment

**Prerequisites**

- An Azure subscription with a linked Entra ID tenant
- An account with permission to create Entra ID app registrations (`Application.ReadWrite.All` Microsoft Graph permission, or equivalent)
- The `azd` environment initialized (`azd env new <env-name>`)

**Step 1 — Enable Entra ID authentication**

Set the `ENABLE_ENTRA_AUTH` variable in your `azd` environment:

```bash
azd env set ENABLE_ENTRA_AUTH true
```

**Step 2 — Provision**

Run `azd provision` (or `azd up` to provision and deploy together):

```bash
azd up
```

During the pre-provision hook, the pipeline:

1. Runs `infra/hooks/preprovision.ps1` (Windows) or `infra/hooks/preprovision.sh` (Linux/macOS).
1. Deploys `infra/entra-id/app-registrations.bicep` using the Microsoft Graph Bicep extension to create both app registrations.
1. Writes `AZURE_AD_API_CLIENT_ID` and `AZURE_AD_SPA_CLIENT_ID` back to the `azd` environment.

The Bicep deployment then passes these values to the backend Container App as environment variables:

| Environment variable | Value |
| --- | --- |
| `AzureAd__ClientId` | API app registration client ID |
| `AzureAd__TenantId` | Entra ID tenant ID |
| `AzureAd__Instance` | Entra ID login endpoint (e.g., `https://login.microsoftonline.com/`) |

**Step 3 — Verify**

After deployment, confirm the app registrations were created in the [Azure Portal](https://portal.azure.com) under **Microsoft Entra ID** → **App registrations**. Look for:

- `{env}-prompt-babbler-api`
- `{env}-prompt-babbler-spa`

**Skipping re-provisioning of app registrations**

The pre-provision hook skips app registration creation if `AZURE_AD_API_CLIENT_ID` is already set in the `azd` environment. This prevents duplicate registrations on subsequent `azd up` or `azd provision` runs.

To force recreation (for example, after `azd down`), clear the stored client IDs:

```bash
azd env set AZURE_AD_API_CLIENT_ID ""
azd env set AZURE_AD_SPA_CLIENT_ID ""
azd provision
```

**Disabling Entra ID authentication**

To switch back to Anonymous or Access Code mode:

```bash
azd env set ENABLE_ENTRA_AUTH false
azd env set AZURE_AD_API_CLIENT_ID ""
azd env set AZURE_AD_SPA_CLIENT_ID ""
azd provision
```

---

## Comparing modes

| Capability | Anonymous | Access Code | Entra ID |
| --- | --- | --- | --- |
| No configuration required | Yes | No | No |
| Protects against unauthorized access | No | Yes | Yes |
| Supports multiple users | No | No | Yes |
| Per-user data isolation | No | No | Yes |
| Requires Azure AD tenant | No | No | Yes |
| Works locally (Aspire) | Yes | Yes | No |
| Recommended for production | No | Personal use only | Yes |
