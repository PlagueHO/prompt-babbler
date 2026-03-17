import { useState, useCallback, useEffect, useRef } from 'react';
import type { GeneratedPrompt } from '@/types';
import * as api from '@/services/api-client';
import { isAuthConfigured } from '@/auth/authConfig';
import { useAuthToken, useAccountCount } from '@/hooks/useAuthToken';

export function useGeneratedPrompts(babbleId: string | undefined) {
  const [prompts, setPrompts] = useState<GeneratedPrompt[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [continuationToken, setContinuationToken] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const getAuthToken = useAuthToken();
  const accountCount = useAccountCount();
  // Stabilize getAuthToken reference to avoid infinite re-render loops.
  const getAuthTokenRef = useRef(getAuthToken);
  getAuthTokenRef.current = getAuthToken;

  const fetchPrompts = useCallback(async (append = false, token?: string | null) => {
    if (!babbleId) return;
    try {
      setLoading(true);
      setError(null);
      const authToken = await getAuthTokenRef.current();
      if (isAuthConfigured && !authToken) {
        setPrompts([]);
        return;
      }
      const data = await api.getGeneratedPrompts(babbleId, append ? token : null, 20, authToken);
      setPrompts((prev) => append ? [...prev, ...data.items] : data.items);
      setContinuationToken(data.continuationToken);
      setHasMore(!!data.continuationToken);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load prompts');
    } finally {
      setLoading(false);
    }
  }, [babbleId]);

  useEffect(() => {
    if (!babbleId) return;
    if (isAuthConfigured && accountCount === 0) {
      setPrompts([]);
      setLoading(false);
    } else {
      void fetchPrompts();
    }
  }, [fetchPrompts, accountCount, babbleId]);

  const loadMore = useCallback(() => {
    if (hasMore && continuationToken) {
      void fetchPrompts(true, continuationToken);
    }
  }, [fetchPrompts, hasMore, continuationToken]);

  const createPrompt = useCallback(
    async (request: { templateId: string; templateName: string; promptText: string }): Promise<GeneratedPrompt> => {
      if (!babbleId) throw new Error('No babble ID');
      const authToken = await getAuthTokenRef.current();
      const created = await api.createGeneratedPrompt(babbleId, request, authToken);
      // Prepend to the list (newest first).
      setPrompts((prev) => [created, ...prev]);
      return created;
    },
    [babbleId],
  );

  const deletePrompt = useCallback(
    async (id: string): Promise<void> => {
      if (!babbleId) throw new Error('No babble ID');
      const authToken = await getAuthTokenRef.current();
      await api.deleteGeneratedPrompt(babbleId, id, authToken);
      setPrompts((prev) => prev.filter((p) => p.id !== id));
    },
    [babbleId],
  );

  return {
    prompts,
    loading,
    error,
    hasMore,
    loadMore,
    createPrompt,
    deletePrompt,
    refresh: fetchPrompts,
  };
}
