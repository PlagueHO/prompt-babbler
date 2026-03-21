import { useState, useCallback, useEffect, useRef } from 'react';
import type { Babble } from '@/types';
import * as api from '@/services/api-client';
import { isAuthConfigured } from '@/auth/authConfig';
import { useAuthToken, useAccountCount } from '@/hooks/useAuthToken';
import { isMigrationNeeded, migrateLocalBabbles } from '@/services/migration';

export function useBabbles() {
  const [babbles, setBabbles] = useState<Babble[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [continuationToken, setContinuationToken] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const getAuthToken = useAuthToken();
  const accountCount = useAccountCount();
  const migrationDone = useRef(false);
  // Stabilize getAuthToken reference to avoid infinite re-render loops
  // when the MSAL provider returns new object references each render.
  const getAuthTokenRef = useRef(getAuthToken);
  getAuthTokenRef.current = getAuthToken;

  const fetchBabbles = useCallback(async (append = false, token?: string | null) => {
    try {
      setLoading(true);
      setError(null);
      const authToken = await getAuthTokenRef.current();
      if (isAuthConfigured && !authToken) {
        setBabbles([]);
        return;
      }

      // Run one-time migration from localStorage if needed.
      if (!migrationDone.current && isMigrationNeeded()) {
        await migrateLocalBabbles(authToken);
        migrationDone.current = true;
      }

      const data = await api.getBabbles(append ? token : null, 20, authToken);
      setBabbles((prev) => append ? [...prev, ...data.items] : data.items);
      setContinuationToken(data.continuationToken);
      setHasMore(!!data.continuationToken);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load babbles');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isAuthConfigured && accountCount === 0) {
      setBabbles([]);
      setLoading(false);
    } else {
      void fetchBabbles();
    }
  }, [fetchBabbles, accountCount]);

  const loadMore = useCallback(() => {
    if (hasMore && continuationToken) {
      void fetchBabbles(true, continuationToken);
    }
  }, [fetchBabbles, hasMore, continuationToken]);

  const createBabble = useCallback(
    async (request: { title: string; text: string; tags?: string[] }): Promise<Babble> => {
      const authToken = await getAuthTokenRef.current();
      const created = await api.createBabble(request, authToken);
      await fetchBabbles();
      return created;
    },
    [fetchBabbles],
  );

  const updateBabble = useCallback(
    async (id: string, request: { title: string; text: string; tags?: string[] }): Promise<Babble> => {
      const authToken = await getAuthTokenRef.current();
      const updated = await api.updateBabble(id, request, authToken);
      // Update in-place for immediate UI response.
      setBabbles((prev) => prev.map((b) => (b.id === id ? updated : b)));
      return updated;
    },
    [],
  );

  const deleteBabble = useCallback(
    async (id: string): Promise<void> => {
      const authToken = await getAuthTokenRef.current();
      await api.deleteBabble(id, authToken);
      setBabbles((prev) => prev.filter((b) => b.id !== id));
    },
    [],
  );

  const getBabble = useCallback(
    async (id: string): Promise<Babble | null> => {
      try {
        const authToken = await getAuthTokenRef.current();
        return await api.getBabble(id, authToken);
      } catch {
        return null;
      }
    },
    [],
  );

  return {
    babbles,
    loading,
    error,
    hasMore,
    loadMore,
    createBabble,
    updateBabble,
    deleteBabble,
    getBabble,
    refresh: fetchBabbles,
  };
}
