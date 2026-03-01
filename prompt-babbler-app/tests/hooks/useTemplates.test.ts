import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useTemplates } from '@/hooks/useTemplates';
import type { PromptTemplate } from '@/types';

describe('useTemplates', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('returns seeded templates initially', () => {
    const { result } = renderHook(() => useTemplates());
    expect(result.current.templates.length).toBeGreaterThanOrEqual(2);
    expect(result.current.templates.every((t) => t.isBuiltIn)).toBe(true);
  });

  it('createTemplate adds a template', () => {
    const { result } = renderHook(() => useTemplates());
    const initialLength = result.current.templates.length;

    const newTemplate: PromptTemplate = {
      id: 'custom-1',
      name: 'Custom Template',
      description: 'A custom template',
      systemPrompt: 'Do something',
      isBuiltIn: false,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    act(() => {
      result.current.createTemplate(newTemplate);
    });

    expect(result.current.templates).toHaveLength(initialLength + 1);
    expect(result.current.templates.find((t) => t.id === 'custom-1')).toBeTruthy();
  });
});
