import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useBabbles } from '@/hooks/useBabbles';
import type { Babble, PagedResponse } from '@/types';

// Mock the api-client module
vi.mock('@/services/api-client', () => ({
  getBabbles: vi.fn().mockResolvedValue({
    items: [
      {
        id: 'b1',
        title: 'Test Babble',
        text: 'Hello world',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
    ],
    continuationToken: null,
  } as PagedResponse<Babble>),
  getBabble: vi.fn().mockResolvedValue({
    id: 'b1',
    title: 'Test Babble',
    text: 'Hello world',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  }),
  createBabble: vi.fn().mockImplementation(async (req: { title: string; text: string }) => ({
    id: 'b-new',
    ...req,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  })),
  updateBabble: vi.fn().mockImplementation(async (id: string, req: { title: string; text: string }) => ({
    id,
    ...req,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  })),
  deleteBabble: vi.fn().mockResolvedValue(undefined),
}));

// Mock migration module to skip migration
vi.mock('@/services/migration', () => ({
  isMigrationNeeded: vi.fn().mockReturnValue(false),
  migrateLocalBabbles: vi.fn().mockResolvedValue(undefined),
}));

describe('useBabbles', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fetches babbles from the API', async () => {
    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.babbles).toHaveLength(1);
    expect(result.current.babbles[0].id).toBe('b1');
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

  it('deleteBabble calls API and removes from list', async () => {
    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.deleteBabble('b1');
    });

    const { deleteBabble } = await import('@/services/api-client');
    expect(deleteBabble).toHaveBeenCalledWith('b1', 'mock-access-token');
    expect(result.current.babbles).toHaveLength(0);
  });

  it('updateBabble calls API and updates in list', async () => {
    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.updateBabble('b1', { title: 'Updated', text: 'New text' });
    });

    const { updateBabble } = await import('@/services/api-client');
    expect(updateBabble).toHaveBeenCalledWith('b1', { title: 'Updated', text: 'New text' }, 'mock-access-token');
    expect(result.current.babbles[0].title).toBe('Updated');
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
    (getBabbles as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('Network error'));

    const { result } = renderHook(() => useBabbles());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.error).toBe('Network error');
    expect(result.current.babbles).toHaveLength(0);
  });
});
