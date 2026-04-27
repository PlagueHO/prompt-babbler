import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { useSemanticSearch } from '@/hooks/useSemanticSearch';

vi.mock('@/services/api-client', () => ({
  searchBabbles: vi.fn().mockResolvedValue({
    results: [
      {
        id: 'b1',
        title: 'Test Result',
        snippet: 'Some snippet...',
        tags: ['tag1'],
        createdAt: '2026-01-01T00:00:00.000Z',
        isPinned: false,
        score: 0.95,
      },
    ],
  }),
}));

describe('useSemanticSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should return empty results for query under 2 characters', () => {
    const { result } = renderHook(() => useSemanticSearch('a'));

    expect(result.current.results).toEqual([]);
    expect(result.current.loading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('should return results from API for valid query', async () => {
    const { result } = renderHook(() => useSemanticSearch('test query'));

    await waitFor(() => {
      expect(result.current.results).toHaveLength(1);
    }, { timeout: 2000 });

    expect(result.current.results[0].id).toBe('b1');
    expect(result.current.results[0].score).toBe(0.95);
  });

  it('should handle API errors gracefully', async () => {
    const { searchBabbles } = await import('@/services/api-client');
    (searchBabbles as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('Network error'));

    const { result } = renderHook(() => useSemanticSearch('test query'));

    await waitFor(() => {
      expect(result.current.error).toBe('Network error');
    }, { timeout: 2000 });

    expect(result.current.results).toEqual([]);
  });
});
