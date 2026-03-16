import { describe, it, expect, beforeEach } from 'vitest';
import {
  getBabbles,
  createBabble,
  deleteBabble,
  getStorageUsage,
} from '@/services/local-storage';
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

describe('local-storage service', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('getBabbles returns empty array when no data', () => {
    expect(getBabbles()).toEqual([]);
  });

  it('saveBabble stores and retrieves babble', () => {
    const babble = makeBabble();
    createBabble(babble);
    const result = getBabbles();
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('b1');
  });

  it('deleteBabble removes babble', () => {
    createBabble(makeBabble({ id: 'b1' }));
    createBabble(makeBabble({ id: 'b2', title: 'Second' }));
    deleteBabble('b1');
    const result = getBabbles();
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('b2');
  });

  it('getStorageUsage returns usage info', () => {
    localStorage.setItem('test-key', 'test-value');
    const usage = getStorageUsage();
    expect(usage).toHaveProperty('used');
    expect(usage).toHaveProperty('quota');
    expect(usage).toHaveProperty('percentage');
    expect(usage.used).toBeGreaterThan(0);
    expect(usage.quota).toBe(5 * 1024 * 1024);
  });
});
