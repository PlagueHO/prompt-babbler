import { useState, useEffect, useCallback } from 'react';
import { getAccessStatus, getTemplates, setAccessCode } from '@/services/api-client';

const SESSION_STORAGE_KEY = 'prompt-babbler-access-code';

export function useAccessCode() {
  const [accessCodeRequired, setAccessCodeRequired] = useState(false);
  const [isVerified, setIsVerified] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function checkAccess() {
      try {
        const status = await getAccessStatus();

        if (cancelled) return;

        if (!status.accessCodeRequired) {
          setIsVerified(true);
          setIsLoading(false);
          return;
        }

        setAccessCodeRequired(true);

        // Check for cached access code in sessionStorage
        const cached = sessionStorage.getItem(SESSION_STORAGE_KEY);
        if (cached) {
          setAccessCode(cached);
          try {
            await getTemplates();
            if (cancelled) return;
            setIsVerified(true);
            setIsLoading(false);
            return;
          } catch {
            // Cached code is invalid — clear it
            if (cancelled) return;
            sessionStorage.removeItem(SESSION_STORAGE_KEY);
            setAccessCode(null);
          }
        }

        setIsLoading(false);
      } catch {
        if (cancelled) return;
        // Server unreachable — let the user through so other error handling
        // can display the offline message
        setIsVerified(true);
        setIsLoading(false);
      }
    }

    checkAccess();

    return () => {
      cancelled = true;
    };
  }, []);

  const submitCode = useCallback(async (code: string) => {
    setError(null);
    setIsLoading(true);

    setAccessCode(code);

    try {
      await getTemplates();
      sessionStorage.setItem(SESSION_STORAGE_KEY, code);
      setIsVerified(true);
    } catch (err) {
      setAccessCode(null);
      if (err instanceof Error && err.message.includes('401')) {
        setError('Invalid access code');
      } else {
        setError('Unable to connect to the server');
      }
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { accessCodeRequired, isVerified, isLoading, error, submitCode };
}
