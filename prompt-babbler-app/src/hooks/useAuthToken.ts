import { useCallback } from 'react';
import { useMsal } from '@azure/msal-react';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { isAuthConfigured, loginRequest } from '@/auth/authConfig';

/**
 * Returns a `getAuthToken` callback that acquires an access token when
 * Entra ID authentication is configured, or returns `undefined` in
 * anonymous single-user mode.
 *
 * Must only be called from components rendered inside `<MsalProvider>`.
 * For anonymous mode (no MsalProvider), use `useAuthTokenAnonymous` instead.
 */
function useAuthTokenMsal(): () => Promise<string | undefined> {
  const { instance, accounts } = useMsal();

  return useCallback(async (): Promise<string | undefined> => {
    if (accounts.length === 0) return undefined;
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account: accounts[0],
      });
      return response.accessToken;
    } catch (err) {
      if (err instanceof InteractionRequiredAuthError) {
        const response = await instance.acquireTokenPopup(loginRequest);
        return response.accessToken;
      }
      throw err;
    }
  }, [instance, accounts]);
}

/** No-op token provider for anonymous mode. */
function useAuthTokenAnonymous(): () => Promise<string | undefined> {
  return useCallback(async () => undefined, []);
}

/**
 * Returns a `getAuthToken` callback. When Entra ID is configured it
 * acquires a token via MSAL; otherwise it returns `undefined`.
 *
 * IMPORTANT: This hook must only be used from components that are
 * rendered inside `<MsalProvider>` when `isAuthConfigured` is true,
 * which is guaranteed by the conditional wrapping in `main.tsx`.
 */
export const useAuthToken = isAuthConfigured ? useAuthTokenMsal : useAuthTokenAnonymous;

/**
 * Returns the number of signed-in accounts (0 when auth is disabled).
 */
export function useAccountCount(): number {
  if (!isAuthConfigured) return 0;
  // This is safe because when isAuthConfigured is true, MsalProvider is present.
  // eslint-disable-next-line react-hooks/rules-of-hooks
  const { accounts } = useMsal();
  return accounts.length;
}
