import '@testing-library/jest-dom/vitest'
import { vi } from 'vitest'
import React from 'react'

// Global MSAL mock — default: authenticated user with mock token
vi.mock('@azure/msal-react', () => ({
  MsalProvider: ({ children }: { children: React.ReactNode }) => children,
  useMsal: vi.fn(() => ({
    instance: {
      acquireTokenSilent: vi.fn().mockResolvedValue({
        accessToken: 'mock-access-token',
        expiresOn: new Date(Date.now() + 3600000),
      }),
      loginPopup: vi.fn().mockResolvedValue({}),
      logoutPopup: vi.fn().mockResolvedValue(undefined),
    },
    inProgress: 'none',
    accounts: [{
      homeAccountId: 'test|test-user',
      localAccountId: 'test-user',
      username: 'testuser@contoso.com',
      name: 'Test User',
      environment: 'login.microsoftonline.com',
      tenantId: 'test-tenant',
    }],
  })),
  useIsAuthenticated: vi.fn(() => true),
  AuthenticatedTemplate: ({ children }: { children: React.ReactNode }) => children,
  UnauthenticatedTemplate: () => null,
}))

// Global mock for authConfig to prevent MSAL initialization errors in tests
vi.mock('@/auth/authConfig', () => ({
  msalInstance: {
    initialize: vi.fn().mockResolvedValue(undefined),
    acquireTokenSilent: vi.fn().mockResolvedValue({ accessToken: 'mock-access-token' }),
  },
  loginRequest: {
    scopes: ['api://prompt-babbler-api/access_as_user'],
  },
  isAuthConfigured: true,
}))
