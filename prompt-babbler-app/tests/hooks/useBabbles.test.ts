import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useBabbles } from '@/hooks/useBabbles';
import type { Babble } from '@/types';

function makeBabble(overrides: Partial<Babble> = {}): Babble {
  return {
    id: 'b1',
    title: 'Test Babble',
    text: 'Hello world',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    lastGeneratedPrompt: null,
    ...overrides,
  };
}

describe('useBabbles', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('returns empty array initially', () => {
    const { result } = renderHook(() => useBabbles());
    expect(result.current.babbles).toEqual([]);
  });

  it('createBabble adds a babble', () => {
    const { result } = renderHook(() => useBabbles());

    act(() => {
      result.current.createBabble(makeBabble());
    });

    expect(result.current.babbles).toHaveLength(1);
    expect(result.current.babbles[0].id).toBe('b1');
  });

  it('deleteBabble removes a babble', () => {
    const { result } = renderHook(() => useBabbles());

    act(() => {
      result.current.createBabble(makeBabble({ id: 'b1' }));
    });
    act(() => {
      result.current.createBabble(makeBabble({ id: 'b2', title: 'Second' }));
    });
    act(() => {
      result.current.deleteBabble('b1');
    });

    expect(result.current.babbles).toHaveLength(1);
    expect(result.current.babbles[0].id).toBe('b2');
  });
});
