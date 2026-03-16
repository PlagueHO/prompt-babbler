import { PublicClientApplication, type Configuration } from '@azure/msal-browser';

// Injected by Vite from Aspire env vars at build/dev time
declare const __MSAL_CLIENT_ID__: string;
declare const __MSAL_TENANT_ID__: string;

const clientId = typeof __MSAL_CLIENT_ID__ !== 'undefined' ? __MSAL_CLIENT_ID__ : '';
const tenantId = typeof __MSAL_TENANT_ID__ !== 'undefined' ? __MSAL_TENANT_ID__ : '';

/** Whether Entra ID authentication is configured (client ID injected at build time). */
export const isAuthConfigured = !!clientId;

const msalConfig: Configuration = {
  auth: {
    clientId,
    authority: tenantId
      ? `https://login.microsoftonline.com/${tenantId}`
      : 'https://login.microsoftonline.com/common',
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
};

export const msalInstance = new PublicClientApplication(msalConfig);

export const loginRequest = {
  scopes: ['api://prompt-babbler-api/access_as_user'],
};
