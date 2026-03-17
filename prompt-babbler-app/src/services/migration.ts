import type { Babble } from '@/types';
import * as api from '@/services/api-client';

const MIGRATION_KEY = 'prompt-babbler:migrated';
const BABBLES_KEY = 'prompt-babbler:babbles';

export function isMigrationNeeded(): boolean {
  if (localStorage.getItem(MIGRATION_KEY)) return false;
  const raw = localStorage.getItem(BABBLES_KEY);
  if (!raw) {
    // Nothing to migrate — mark as done.
    localStorage.setItem(MIGRATION_KEY, 'true');
    return false;
  }
  try {
    const babbles = JSON.parse(raw) as unknown[];
    return Array.isArray(babbles) && babbles.length > 0;
  } catch {
    return false;
  }
}

export async function migrateLocalBabbles(accessToken?: string): Promise<void> {
  if (!isMigrationNeeded()) return;

  const raw = localStorage.getItem(BABBLES_KEY);
  if (!raw) return;

  let babbles: Babble[];
  try {
    babbles = JSON.parse(raw) as Babble[];
  } catch {
    // Corrupt data — mark migrated and move on.
    localStorage.setItem(MIGRATION_KEY, 'true');
    return;
  }

  for (const babble of babbles) {
    try {
      await api.createBabble(
        { title: babble.title, text: babble.text },
        accessToken,
      );
    } catch {
      // Skip individual failures — the babble stays in localStorage
      // and migration will be retried on next load.
      console.warn(`Migration: failed to migrate babble "${babble.title}"`);
      return;
    }
  }

  // All migrated successfully — clean up.
  localStorage.removeItem(BABBLES_KEY);
  localStorage.setItem(MIGRATION_KEY, 'true');
}
