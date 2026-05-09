import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useTemplateList } from '@/hooks/useTemplateList';
import type { PromptTemplate } from '@/types';

vi.mock('@/services/api-client', () => ({
  listTemplates: vi.fn().mockResolvedValue({
    items: [{
      id: 't1',
      name: 'Writer',
      description: 'Helps with writing',
      instructions: 'Full instructions',
      tags: ['creative'],
      isBuiltIn: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    } as PromptTemplate],
    continuationToken: null,
  }),
}));

const template: PromptTemplate = {
  id: 't1',
  name: 'Writer',
  description: 'Helps with writing',
  instructions: 'Full instructions',
  tags: ['creative'],
  isBuiltIn: true,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

describe('useTemplateList', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fetches the first page of templates', async () => {
    const { result } = renderHook(() => useTemplateList());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.templates).toHaveLength(1);
    expect(result.current.templates[0].name).toBe('Writer');
  });

  it('loads more templates when continuation token exists', async () => {
    const { listTemplates } = await import('@/services/api-client');
    (listTemplates as ReturnType<typeof vi.fn>)
      .mockResolvedValueOnce({
        items: [template],
        continuationToken: 'next',
      })
      .mockResolvedValueOnce({
        items: [{ ...template, id: 't2', name: 'Editor' }],
        continuationToken: null,
      });

    const { result } = renderHook(() => useTemplateList());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      result.current.loadMore();
    });

    await waitFor(() => {
      expect(result.current.templates).toHaveLength(2);
    });
  });
});
