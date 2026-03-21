import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useBabbles } from '@/hooks/useBabbles';
import type { Babble } from '@/types';

// Mock the api-client module
// The hook calls getBabbles multiple times with different options:
//   - Bubbles: { isPinned: true, ... } and { isPinned: false, ... }
//   - List: { sortBy, sortDirection, pageSize, ... }
vi.mock('@/services/api-client', () => {
  const baseBabble = {
    id: 'b1',
    title: 'Test Babble',
    text: 'Hello world',
    isPinned: false,
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
  };
  return {
    getBabbles: vi.fn().mockImplementation(async (options: { isPinned?: boolean }) => {
      if (options?.isPinned === true) {
        return { items: [], continuationToken: null };
      }
      return { items: [{ ...baseBabble }], continuationToken: null };
    }),
    getBabble: vi.fn().mockResolvedValue({ ...baseBabble }),
    createBabble: vi.fn().mockImplementation(async (req: { title: string; text: string }) => ({
      id: 'b-new',
      ...req,
      isPinned: false,
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
    })),
    updateBabble: vi.fn().mockImplementation(async (id: string, req: { title: string; text: string }) => ({
      id,
      ...req,
      isPinned: false,
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
    })),
    deleteBabble: vi.fn().mockResolvedValue(undefined),
    pinBabble: vi.fn().mockImplementation(async (id: string, isPinned: boolean) => ({
      id,
      title: 'Test Babble',
      text: 'Hello world',
      isPinned,
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
    })),
  };
});

// Mock migration module to skip migration
vi.mock('@/services/migration', () => ({
  isMigrationNeeded: vi.fn().mockReturnValue(false),
  migrateLocalBabbles: vi.fn().mockResolvedValue(undefined),
}));

describe('useBabbles', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fetches babbles from the API into bubbles and list', async () => {
    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    // Non-pinned babble should appear in bubbles (pinned + recent fill top 6)
    expect(result.current.bubbleBabbles).toHaveLength(1);
    expect(result.current.bubbleBabbles[0].id).toBe('b1');
    // List section also gets data from its own fetch
    expect(result.current.listBabbles).toHaveLength(1);
  });

  it('createBabble calls API and refreshes', async () => {
    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.createBabble({ title: 'New', text: 'Content' });
    });

    const { createBabble } = await import('@/services/api-client');
    expect(createBabble).toHaveBeenCalledOnce();
  });

  it('deleteBabble calls API and removes from lists', async () => {
    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.deleteBabble('b1');
    });

    const { deleteBabble } = await import('@/services/api-client');
    expect(deleteBabble).toHaveBeenCalledWith('b1', 'mock-access-token');
    expect(result.current.bubbleBabbles).toHaveLength(0);
    expect(result.current.listBabbles).toHaveLength(0);
  });

  it('updateBabble calls API and updates in both lists', async () => {
    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.updateBabble('b1', { title: 'Updated', text: 'New text' });
    });

    const { updateBabble } = await import('@/services/api-client');
    expect(updateBabble).toHaveBeenCalledWith('b1', { title: 'Updated', text: 'New text' }, 'mock-access-token');
    expect(result.current.bubbleBabbles[0].title).toBe('Updated');
  });

  it('getBabble fetches a single babble by id', async () => {
    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    let babble: Babble | null = null;
    await act(async () => {
      babble = await result.current.getBabble('b1');
    });

    expect(babble).not.toBeNull();
    expect(babble!.id).toBe('b1');
  });

  it('handles API errors gracefully', async () => {
    const { getBabbles } = await import('@/services/api-client');
    (getBabbles as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.error).toBe('Network error');
    expect(result.current.totalBabbles).toBe(0);
  });

  it('exposes search and sort state', async () => {
    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.search).toBe('');
    expect(result.current.sortBy).toBe('createdAt');
    expect(result.current.sortDirection).toBe('desc');
  });
});
