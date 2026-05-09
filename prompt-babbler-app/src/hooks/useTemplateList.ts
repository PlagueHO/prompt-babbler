import { useState, useCallback, useEffect, useRef } from 'react';
import type { PromptTemplate } from '@/types';
import * as api from '@/services/api-client';
import { isAuthConfigured } from '@/auth/authConfig';
import { useAuthToken, useAccountCount } from '@/hooks/useAuthToken';

const PAGE_SIZE = 20;

export type TemplateOrder = 'alphabetical' | 'recentlyUsed';

export function useTemplateList() {
  const [templates, setTemplates] = useState<PromptTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [continuationToken, setContinuationToken] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [nameFilter, setNameFilter] = useState('');
  const [tagFilter, setTagFilter] = useState('');
  const [order, setOrder] = useState<TemplateOrder>('recentlyUsed');
  const getAuthToken = useAuthToken();
  const accountCount = useAccountCount();
  const getAuthTokenRef = useRef(getAuthToken);
  getAuthTokenRef.current = getAuthToken;

  const fetchTemplates = useCallback(async (
    append = false,
    token?: string | null,
    forceRefresh = false,
  ) => {
    try {
      if (append) {
        setLoadingMore(true);
      } else {
        setLoading(true);
      }
      setError(null);

      const authToken = await getAuthTokenRef.current();
      if (isAuthConfigured && !authToken) {
        setTemplates([]);
        setHasMore(false);
        setContinuationToken(null);
        return;
      }

      const sortBy = order === 'alphabetical' ? 'name' : 'updatedAt';
      const sortDirection = order === 'alphabetical' ? 'asc' : 'desc';

      const response = await api.listTemplates({
        continuationToken: append ? token : null,
        pageSize: PAGE_SIZE,
        search: nameFilter || undefined,
        tag: tagFilter || undefined,
        sortBy,
        sortDirection,
        forceRefresh,
      }, authToken);

      setTemplates((prev) => append ? [...prev, ...response.items] : response.items);
      setContinuationToken(response.continuationToken);
      setHasMore(!!response.continuationToken);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load templates');
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  }, [nameFilter, order, tagFilter]);

  useEffect(() => {
    if (isAuthConfigured && accountCount === 0) {
      setTemplates([]);
      setLoading(false);
      setHasMore(false);
      setContinuationToken(null);
    } else {
      void fetchTemplates();
    }
  }, [accountCount, fetchTemplates]);

  const loadMore = useCallback(() => {
    if (hasMore && continuationToken && !loadingMore) {
      void fetchTemplates(true, continuationToken);
    }
  }, [continuationToken, fetchTemplates, hasMore, loadingMore]);

  return {
    templates,
    loading,
    loadingMore,
    error,
    hasMore,
    nameFilter,
    setNameFilter,
    tagFilter,
    setTagFilter,
    order,
    setOrder,
    loadMore,
    refresh: () => fetchTemplates(false, null, true),
  };
}
