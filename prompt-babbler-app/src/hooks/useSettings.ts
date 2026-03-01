import { useState, useEffect, useCallback } from 'react';
import type { LlmSettingsView, LlmSettingsSaveRequest, TestConnectionResponse } from '@/types';
import * as api from '@/services/api-client';

export function useSettings() {
  const [settings, setSettings] = useState<LlmSettingsView | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchSettings = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await api.getSettings();
      setSettings(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load settings');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchSettings();
  }, [fetchSettings]);

  const updateSettings = useCallback(
    async (req: LlmSettingsSaveRequest): Promise<LlmSettingsView> => {
      setError(null);
      try {
        const data = await api.updateSettings(req);
        setSettings(data);
        return data;
      } catch (err) {
        const msg =
          err instanceof Error ? err.message : 'Failed to save settings';
        setError(msg);
        throw err;
      }
    },
    []
  );

  const testConnection = useCallback(async (): Promise<TestConnectionResponse> => {
    setError(null);
    try {
      return await api.testConnection();
    } catch (err) {
      const msg =
        err instanceof Error ? err.message : 'Connection test failed';
      setError(msg);
      throw err;
    }
  }, []);

  return { settings, isLoading, error, updateSettings, testConnection };
}
