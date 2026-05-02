import { useState, useEffect, useCallback } from 'react';
import type { StatusResponse } from '@/types';
import * as api from '@/services/api-client';

export function useSettings() {
  const [status, setStatus] = useState<StatusResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchStatus = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await api.getStatus();
      setStatus(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reach backend');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchStatus();
  }, [fetchStatus]);

  const isConnected = status?.status === 'Healthy';

  return { status, isConnected, isLoading, error, refresh: fetchStatus };
}
