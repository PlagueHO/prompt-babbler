# Skill: MSAL Authentication

## Confidence: medium

## Overview

prompt-babbler supports two authentication modes: anonymous single-user (default) and Entra ID multi-user. This skill covers the MSAL configuration, the dual-mode architecture, the critical `useRef` pattern for preventing infinite re-renders, and WebSocket token handling.

## Dual-Mode Architecture

### Mode Detection

Authentication mode is determined by the presence of `AzureAd:ClientId` in backend config and `MSAL_CLIENT_ID` in frontend env:

**Frontend** (`src/auth/authConfig.ts`):

```typescript
export const isAuthConfigured = !!clientId;  // false if MSAL_CLIENT_ID is empty
```

**Backend** (`Program.cs`):

```csharp
var isAuthEnabled = !string.IsNullOrEmpty(builder.Configuration["AzureAd:ClientId"]);
```

### Anonymous Mode (Backend)

When auth is disabled, the backend injects a synthetic `ClaimsPrincipal`:

- Object ID claim: `_anonymous`
- Scope claim: `access_as_user`
- Default authorization policy: allows all
- All controllers still use `[Authorize]` + `[RequiredScope]` — they work because the synthetic claims satisfy the requirements

### Entra ID Mode (Backend)

- Microsoft.Identity.Web JWT Bearer validation
- Authority: `https://login.microsoftonline.com/{tenantId}`
- Audience: API app registration client ID
- Required scope: `access_as_user`
- WebSocket: Custom token extraction from `?access_token=` query parameter for `/api/transcribe`

## Frontend MSAL Configuration

```typescript
// authConfig.ts
const msalConfig: Configuration = {
  auth: {
    clientId,                                          // From Vite define constant
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',                  // Cleared on browser close
  },
};

export const loginRequest = {
  scopes: ['api://prompt-babbler-api/access_as_user'], // API scope
};
```

### Conditional MsalProvider

```typescript
// main.tsx
const app = isAuthConfigured ? (
  <MsalProvider instance={msalInstance}>
    <App />
  </MsalProvider>
) : (
  <App />
);
```

MSAL instance is initialized before rendering: `msalInstance.initialize().then(renderApp)`.

## The useRef Token Pattern (Critical)

**Problem:** `useMsal()` returns unstable references on every render. If `getAuthToken` (derived from MSAL) is used as a `useEffect` dependency, it triggers infinite re-fetching.

**Solution:** Stabilize with `useRef`:

```typescript
export function useBabbles() {
  const getAuthToken = useAuthToken();

  // Stabilize to prevent infinite loops
  const getAuthTokenRef = useRef(getAuthToken);
  getAuthTokenRef.current = getAuthToken;

  useEffect(() => {
    const fetchBabbles = async () => {
      const token = await getAuthTokenRef.current();
      // ... fetch data
    };
    fetchBabbles();
  }, []); // Empty deps — ref is always current
}
```

**Every hook that fetches data on mount with auth tokens MUST use this pattern.** Hooks that use it: `useBabbles`, `useGeneratedPrompts`, `useTemplates`, `useUserSettings`.

## Token Acquisition

```typescript
// useAuthToken.ts
export function useAuthToken() {
  if (!isAuthConfigured) return async () => undefined; // No-op in anonymous mode

  const { instance, accounts } = useMsal();

  return async () => {
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account: accounts[0],
      });
      return response.accessToken;
    } catch (e) {
      if (e instanceof InteractionRequiredAuthError) {
        const response = await instance.acquireTokenPopup(loginRequest);
        return response.accessToken;
      }
      throw e;
    }
  };
}
```

## WebSocket Token Handling

WebSocket connections can't use Authorization headers. The transcription endpoint accepts JWT via query string:

```typescript
// Frontend: transcription-stream.ts
const wsUrl = `${baseUrl}/api/transcribe/stream?language=${lang}&access_token=${token}`;
```

```csharp
// Backend: Program.cs — custom JwtBearerEvents
OnMessageReceived = context => {
    if (context.Request.Path.StartsWithSegments("/api/transcribe")) {
        var accessToken = context.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(accessToken)) {
            context.Token = accessToken;
        }
    }
};
```

## Entra ID App Registrations

Two app registrations are required for Entra ID mode:

1. **API app** (`{env}-prompt-babbler-api`):
   - Identifier URI: `api://prompt-babbler-api`
   - OAuth2 scope: `access_as_user`
   - Access token version 2

1. **SPA app** (`{env}-prompt-babbler-spa`):
   - Redirect URIs: `http://localhost:5173` (dev) + production
   - Required resource access: API app's `access_as_user` scope

Created via Microsoft Graph Bicep extension or `infra/hooks/preprovision` scripts.
