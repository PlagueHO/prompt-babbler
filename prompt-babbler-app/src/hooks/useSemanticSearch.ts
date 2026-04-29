import { useState, useEffect, useRef } from 'react';
import { searchBabbles } from '@/services/api-client';
import type { BabbleSearchResultItem } from '@/types';

export function useSemanticSearch(query: string, topK: number = 10) {
  const [results, setResults] = useState<BabbleSearchResultItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    if (query.trim().length < 2) {
      setResults([]);
      setLoading(false);
      return;
    }

    setLoading(true);

    const timeoutId = setTimeout(async () => {
      abortControllerRef.current?.abort();
      abortControllerRef.current = new AbortController();

      try {
        const response = await searchBabbles(query, topK, abortControllerRef.current.signal);
        setResults(response.results);
        setError(null);
      } catch (err) {
        if (err instanceof DOMException && err.name === 'AbortError') return;
        setError(err instanceof Error ? err.message : 'Search failed');
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, 300);

    return () => {
      clearTimeout(timeoutId);
      abortControllerRef.current?.abort();
    };
  }, [query, topK]);

  return { results, loading, error };
}
