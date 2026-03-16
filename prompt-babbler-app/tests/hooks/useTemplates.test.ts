import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { useTemplates } from '@/hooks/useTemplates';
import type { PromptTemplate } from '@/types';

// Mock the api-client module to avoid real fetch calls
vi.mock('@/services/api-client', () => {
  const mockTemplates: PromptTemplate[] = [
    {
      id: 'builtin-1',
      name: 'Built-in Template',
      description: 'A built-in template',
      systemPrompt: 'You are helpful.',
      isBuiltIn: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
  ];

  return {
    getTemplates: vi.fn().mockResolvedValue(mockTemplates),
    getTemplate: vi.fn(),
    createTemplate: vi.fn().mockImplementation(async (req: { name: string; description: string; systemPrompt: string }) => ({
      id: 'new-1',
      ...req,
      isBuiltIn: false,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    })),
    updateTemplate: vi.fn(),
    deleteTemplate: vi.fn().mockResolvedValue(undefined),
    getStatus: vi.fn(),
  };
});

describe('useTemplates', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fetches templates from the API', async () => {
    const { result } = renderHook(() => useTemplates());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.templates).toHaveLength(1);
    expect(result.current.templates[0].name).toBe('Built-in Template');
  });

  it('createTemplate calls API and refreshes', async () => {
    const { result } = renderHook(() => useTemplates());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await result.current.createTemplate({
      name: 'Custom Template',
      description: 'A custom template',
      systemPrompt: 'Do something',
    });

    const { createTemplate } = await import('@/services/api-client');
    expect(createTemplate).toHaveBeenCalledOnce();
  });
});
