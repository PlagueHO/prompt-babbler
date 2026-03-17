import { describe, it, expect, beforeEach, vi } from 'vitest';
import { isMigrationNeeded, migrateLocalBabbles } from '@/services/migration';

const MIGRATION_KEY = 'prompt-babbler:migrated';
const BABBLES_KEY = 'prompt-babbler:babbles';

// Mock api-client
vi.mock('@/services/api-client', () => ({
  createBabble: vi.fn().mockResolvedValue({
    id: 'server-id',
    title: 'Test',
    text: 'Hello',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  }),
}));

describe('migration', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  describe('isMigrationNeeded', () => {
    it('returns false when already migrated', () => {
      localStorage.setItem(MIGRATION_KEY, 'true');
      expect(isMigrationNeeded()).toBe(false);
    });

    it('returns false and sets flag when no babbles exist', () => {
      expect(isMigrationNeeded()).toBe(false);
      expect(localStorage.getItem(MIGRATION_KEY)).toBe('true');
    });

    it('returns false when babbles key is empty array', () => {
      localStorage.setItem(BABBLES_KEY, '[]');
      expect(isMigrationNeeded()).toBe(false);
    });

    it('returns true when babbles exist and not migrated', () => {
      localStorage.setItem(BABBLES_KEY, JSON.stringify([
        { id: 'b1', title: 'Test', text: 'Hello', createdAt: '', updatedAt: '' },
      ]));
      expect(isMigrationNeeded()).toBe(true);
    });
  });

  describe('migrateLocalBabbles', () => {
    it('migrates babbles to API and clears localStorage', async () => {
      localStorage.setItem(BABBLES_KEY, JSON.stringify([
        { id: 'b1', title: 'First', text: 'Hello', createdAt: '', updatedAt: '' },
        { id: 'b2', title: 'Second', text: 'World', createdAt: '', updatedAt: '' },
      ]));

      await migrateLocalBabbles('test-token');

      const { createBabble } = await import('@/services/api-client');
      expect(createBabble).toHaveBeenCalledTimes(2);
      expect(createBabble).toHaveBeenCalledWith({ title: 'First', text: 'Hello' }, 'test-token');
      expect(createBabble).toHaveBeenCalledWith({ title: 'Second', text: 'World' }, 'test-token');

      expect(localStorage.getItem(BABBLES_KEY)).toBeNull();
      expect(localStorage.getItem(MIGRATION_KEY)).toBe('true');
    });

    it('skips migration when already done', async () => {
      localStorage.setItem(MIGRATION_KEY, 'true');

      await migrateLocalBabbles('test-token');

      const { createBabble } = await import('@/services/api-client');
      expect(createBabble).not.toHaveBeenCalled();
    });

    it('stops on API error and does not mark as migrated', async () => {
      localStorage.setItem(BABBLES_KEY, JSON.stringify([
        { id: 'b1', title: 'First', text: 'Hello', createdAt: '', updatedAt: '' },
        { id: 'b2', title: 'Second', text: 'World', createdAt: '', updatedAt: '' },
      ]));

      const { createBabble } = await import('@/services/api-client');
      (createBabble as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce({ id: 'server-1' })
        .mockRejectedValueOnce(new Error('Network error'));

      await migrateLocalBabbles('test-token');

      // Migration should not be marked as done
      expect(localStorage.getItem(MIGRATION_KEY)).toBeNull();
      // Babbles should still be in localStorage
      expect(localStorage.getItem(BABBLES_KEY)).not.toBeNull();
    });
  });
});
