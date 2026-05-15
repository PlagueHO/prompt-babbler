import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useSearch } from '@/hooks/useSearch';

const mockSearchBabbles = vi.hoisted(() =>
  vi.fn().mockResolvedValue({
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
  })
);

vi.mock('@/services/api-client', () => ({
  searchBabbles: mockSearchBabbles,
}));

describe('useSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useRealTimers();
  });

  it('should return empty results for query under 2 characters', () => {
    const { result } = renderHook(() => useSearch('a'));

    expect(result.current.results).toEqual([]);
    expect(result.current.loading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('should return results from API for valid query', async () => {
    const { result } = renderHook(() => useSearch('test query'));

    await waitFor(() => {
      expect(result.current.results).toHaveLength(1);
    }, { timeout: 2000 });

    expect(result.current.results[0].id).toBe('b1');
    expect(result.current.results[0].score).toBe(0.95);
  });

  it('should handle API errors gracefully', async () => {
    mockSearchBabbles.mockRejectedValueOnce(new Error('Network error'));

    const { result } = renderHook(() => useSearch('test query'));

    await waitFor(() => {
      expect(result.current.error).toBe('Network error');
    }, { timeout: 2000 });

    expect(result.current.results).toEqual([]);
  });

  it('should debounce API calls by 300ms', async () => {
    vi.useFakeTimers();

    const { result } = renderHook(() => useSearch('test query'));

    // API should not be called immediately
    expect(mockSearchBabbles).not.toHaveBeenCalled();
    expect(result.current.loading).toBe(true);

    // Advance by less than debounce window — still no call
    await act(async () => { vi.advanceTimersByTime(299); });
    expect(mockSearchBabbles).not.toHaveBeenCalled();

    // Advance past debounce window — call fires
    await act(async () => { vi.advanceTimersByTime(1); });
    expect(mockSearchBabbles).toHaveBeenCalledTimes(1);
    expect(mockSearchBabbles).toHaveBeenCalledWith('test query', 10, expect.any(AbortSignal));
  });

  it('should cancel previous debounced call when query changes rapidly', async () => {
    vi.useFakeTimers();

    const { rerender } = renderHook(
      ({ query }: { query: string }) => useSearch(query),
      { initialProps: { query: 'fir' } }
    );

    // Change query before debounce fires
    await act(async () => { vi.advanceTimersByTime(150); });
    rerender({ query: 'first query' });

    // Only the final query should trigger the API call
    await act(async () => { vi.advanceTimersByTime(300); });
    expect(mockSearchBabbles).toHaveBeenCalledTimes(1);
    expect(mockSearchBabbles).toHaveBeenCalledWith('first query', 10, expect.any(AbortSignal));
  });

  it('should pass abort signal to API call', async () => {
    vi.useFakeTimers();

    renderHook(() => useSearch('test query'));

    await act(async () => { vi.advanceTimersByTime(300); });

    expect(mockSearchBabbles).toHaveBeenCalledWith(
      'test query',
      10,
      expect.any(AbortSignal)
    );
  });
});
