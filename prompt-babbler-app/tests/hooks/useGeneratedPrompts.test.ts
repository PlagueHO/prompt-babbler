import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useGeneratedPrompts } from '@/hooks/useGeneratedPrompts';
import type { GeneratedPrompt, PagedResponse } from '@/types';

vi.mock('@/services/api-client', () => ({
  getGeneratedPrompts: vi.fn().mockResolvedValue({
    items: [
      {
        id: 'p1',
        babbleId: 'b1',
        templateId: 't1',
        templateName: 'Test Template',
        promptText: 'Generated text',
        generatedAt: new Date().toISOString(),
      },
    ],
    continuationToken: null,
  } as PagedResponse<GeneratedPrompt>),
  createGeneratedPrompt: vi.fn().mockImplementation(
    async (_babbleId: string, req: { templateId: string; templateName: string; promptText: string }) => ({
      id: 'p-new',
      babbleId: _babbleId,
      ...req,
      generatedAt: new Date().toISOString(),
    }),
  ),
  deleteGeneratedPrompt: vi.fn().mockResolvedValue(undefined),
}));

describe('useGeneratedPrompts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fetches prompts for a babble', async () => {
    const { result } = renderHook(() => useGeneratedPrompts('b1'));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.prompts).toHaveLength(1);
    expect(result.current.prompts[0].id).toBe('p1');
  });

  it('does not fetch when babbleId is undefined', async () => {
    const { result } = renderHook(() => useGeneratedPrompts(undefined));

    // Should not be loading since babbleId is missing
    expect(result.current.prompts).toHaveLength(0);

    const { getGeneratedPrompts } = await import('@/services/api-client');
    expect(getGeneratedPrompts).not.toHaveBeenCalled();
  });

  it('createPrompt calls API and prepends to list', async () => {
    const { result } = renderHook(() => useGeneratedPrompts('b1'));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.createPrompt({
        templateId: 't1',
        templateName: 'Test Template',
        promptText: 'New prompt text',
      });
    });

    const { createGeneratedPrompt } = await import('@/services/api-client');
    expect(createGeneratedPrompt).toHaveBeenCalledOnce();
    // New prompt should be prepended
    expect(result.current.prompts).toHaveLength(2);
    expect(result.current.prompts[0].id).toBe('p-new');
  });

  it('deletePrompt calls API and removes from list', async () => {
    const { result } = renderHook(() => useGeneratedPrompts('b1'));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.deletePrompt('p1');
    });

    const { deleteGeneratedPrompt } = await import('@/services/api-client');
    expect(deleteGeneratedPrompt).toHaveBeenCalledWith('b1', 'p1', 'mock-access-token');
    expect(result.current.prompts).toHaveLength(0);
  });

  it('handles API errors gracefully', async () => {
    const { getGeneratedPrompts } = await import('@/services/api-client');
    (getGeneratedPrompts as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('Network error'));

    const { result } = renderHook(() => useGeneratedPrompts('b1'));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.error).toBe('Network error');
    expect(result.current.prompts).toHaveLength(0);
  });
});
