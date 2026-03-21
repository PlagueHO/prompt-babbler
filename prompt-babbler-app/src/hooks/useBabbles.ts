import { useState, useCallback, useEffect, useRef } from 'react';
import type { Babble } from '@/types';
import * as api from '@/services/api-client';
import { isAuthConfigured } from '@/auth/authConfig';
import { useAuthToken, useAccountCount } from '@/hooks/useAuthToken';
import { isMigrationNeeded, migrateLocalBabbles } from '@/services/migration';

const BUBBLES_PAGE_SIZE = 6;
const LIST_PAGE_SIZE = 20;

export function useBabbles() {
  // Bubbles state (top section — always pinned first then recent, not affected by search/sort)
  const [bubbleBabbles, setBubbleBabbles] = useState<Babble[]>([]);
  const [bubblesLoading, setBubblesLoading] = useState(true);

  // List state (filtered/sorted, paginated)
  const [listBabbles, setListBabbles] = useState<Babble[]>([]);
  const [listLoading, setListLoading] = useState(true);
  const [listContinuationToken, setListContinuationToken] = useState<string | null>(null);
  const [listHasMore, setListHasMore] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);

  // Filter/sort state for list
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState<'createdAt' | 'title'>('createdAt');
  const [sortDirection, setSortDirection] = useState<'desc' | 'asc'>('desc');

  // General
  const [error, setError] = useState<string | null>(null);

  const getAuthToken = useAuthToken();
  const accountCount = useAccountCount();
  const migrationDone = useRef(false);

  // Stabilize getAuthToken reference
  const getAuthTokenRef = useRef(getAuthToken);
  getAuthTokenRef.current = getAuthToken;

  // Fetch bubbles: pinned first, then recent, up to 6 total
  const fetchBubbles = useCallback(async () => {
    try {
      setBubblesLoading(true);
      const authToken = await getAuthTokenRef.current();
      if (isAuthConfigured && !authToken) {
        setBubbleBabbles([]);
        return;
      }

      // Fetch pinned babbles and recent non-pinned in parallel
      const [pinnedResult, recentResult] = await Promise.all([
        api.getBabbles(
          { isPinned: true, sortBy: 'createdAt', sortDirection: 'desc', pageSize: BUBBLES_PAGE_SIZE },
          authToken,
        ),
        api.getBabbles(
          { isPinned: false, sortBy: 'createdAt', sortDirection: 'desc', pageSize: BUBBLES_PAGE_SIZE },
          authToken,
        ),
      ]);

      // Combine: pinned first, then non-pinned, up to 6
      const pinned = pinnedResult.items;
      const nonPinned = recentResult.items;
      const bubbles = [...pinned, ...nonPinned].slice(0, BUBBLES_PAGE_SIZE);
      setBubbleBabbles(bubbles);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load babbles');
    } finally {
      setBubblesLoading(false);
    }
  }, []);

  // Fetch list section (filtered/sorted)
  const fetchList = useCallback(async (
    searchVal: string,
    sortByVal: 'createdAt' | 'title',
    sortDirVal: 'desc' | 'asc',
    append = false,
    token?: string | null,
  ) => {
    try {
      if (append) {
        setLoadingMore(true);
      } else {
        setListLoading(true);
      }
      setError(null);
      const authToken = await getAuthTokenRef.current();
      if (isAuthConfigured && !authToken) {
        setListBabbles([]);
        return;
      }

      const data = await api.getBabbles(
        {
          search: searchVal || undefined,
          sortBy: sortByVal,
          sortDirection: sortDirVal,
          pageSize: LIST_PAGE_SIZE,
          continuationToken: append ? token : null,
        },
        authToken,
      );
      setListBabbles((prev) => append ? [...prev, ...data.items] : data.items);
      setListContinuationToken(data.continuationToken);
      setListHasMore(!!data.continuationToken);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load babbles');
    } finally {
      setListLoading(false);
      setLoadingMore(false);
    }
  }, []);

  const didMount = useRef(false);

  // Initial load
  useEffect(() => {
    if (isAuthConfigured && accountCount === 0) {
      setBubbleBabbles([]);
      setListBabbles([]);
      setBubblesLoading(false);
      setListLoading(false);
      didMount.current = true;
    } else {
      const doInit = async () => {
        if (!migrationDone.current && isMigrationNeeded()) {
          const authToken = await getAuthTokenRef.current();
          await migrateLocalBabbles(authToken);
          migrationDone.current = true;
        }
        await Promise.all([
          fetchBubbles(),
          fetchList('', 'createdAt', 'desc'),
        ]);
        didMount.current = true;
      };
      void doInit();
    }
  }, [fetchBubbles, fetchList, accountCount]);

  // Refetch list when search/sort changes (skip on initial mount)
  useEffect(() => {
    if (!didMount.current) return;
    void fetchList(search, sortBy, sortDirection);
  }, [search, sortBy, sortDirection, fetchList]);

  const loadMore = useCallback(() => {
    if (listHasMore && listContinuationToken && !loadingMore) {
      void fetchList(search, sortBy, sortDirection, true, listContinuationToken);
    }
  }, [fetchList, listHasMore, listContinuationToken, loadingMore, search, sortBy, sortDirection]);

  // Toggle pin with optimistic update
  const togglePin = useCallback(async (babbleId: string) => {
    // Find in bubbles or list
    const allBabbles = [...bubbleBabbles, ...listBabbles];
    const babble = allBabbles.find((b) => b.id === babbleId);
    if (!babble) return;

    const newIsPinned = !babble.isPinned;

    // Optimistic update
    setBubbleBabbles((prev) => {
      const updated = prev.map((b) => b.id === babbleId ? { ...b, isPinned: newIsPinned } : b);
      // Re-sort: pinned first, then by createdAt desc
      return [...updated].sort((a, b) => {
        if (a.isPinned !== b.isPinned) return a.isPinned ? -1 : 1;
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      });
    });
    setListBabbles((prev) =>
      prev.map((b) => b.id === babbleId ? { ...b, isPinned: newIsPinned } : b)
    );

    try {
      const authToken = await getAuthTokenRef.current();
      await api.pinBabble(babbleId, newIsPinned, authToken);
      // Refresh bubbles to reflect new pin state accurately
      void fetchBubbles();
    } catch {
      // Revert optimistic update on error
      setBubbleBabbles((prev) =>
        prev.map((b) => b.id === babbleId ? { ...b, isPinned: babble.isPinned } : b)
      );
      setListBabbles((prev) =>
        prev.map((b) => b.id === babbleId ? { ...b, isPinned: babble.isPinned } : b)
      );
    }
  }, [bubbleBabbles, listBabbles, fetchBubbles]);

  const createBabble = useCallback(
    async (request: { title: string; text: string; tags?: string[] }): Promise<Babble> => {
      const authToken = await getAuthTokenRef.current();
      const created = await api.createBabble(request, authToken);
      // Refresh both sections
      void fetchBubbles();
      void fetchList(search, sortBy, sortDirection);
      return created;
    },
    [fetchBubbles, fetchList, search, sortBy, sortDirection],
  );

  const updateBabble = useCallback(
    async (id: string, request: { title: string; text: string; tags?: string[] }): Promise<Babble> => {
      const authToken = await getAuthTokenRef.current();
      const updated = await api.updateBabble(id, request, authToken);
      setBubbleBabbles((prev) => prev.map((b) => (b.id === id ? updated : b)));
      setListBabbles((prev) => prev.map((b) => (b.id === id ? updated : b)));
      return updated;
    },
    [],
  );

  const deleteBabble = useCallback(
    async (id: string): Promise<void> => {
      const authToken = await getAuthTokenRef.current();
      await api.deleteBabble(id, authToken);
      setBubbleBabbles((prev) => prev.filter((b) => b.id !== id));
      setListBabbles((prev) => prev.filter((b) => b.id !== id));
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

  const loading = bubblesLoading && listLoading;
  const totalBabbles = bubbleBabbles.length + listBabbles.length;

  return {
    // Bubbles section
    bubbleBabbles,
    bubblesLoading,
    // List section
    listBabbles,
    listLoading,
    listHasMore,
    loadingMore,
    loadMore,
    // Filter/sort
    search,
    setSearch,
    sortBy,
    setSortBy,
    sortDirection,
    setSortDirection,
    // General
    loading,
    error,
    totalBabbles,
    // Actions
    togglePin,
    createBabble,
    updateBabble,
    deleteBabble,
    getBabble,
    refresh: () => {
      void fetchBubbles();
      void fetchList(search, sortBy, sortDirection);
    },
  };
}
