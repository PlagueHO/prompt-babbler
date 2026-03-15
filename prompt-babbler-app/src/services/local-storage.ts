import type { Babble } from '@/types';

const KEYS = {
  babbles: 'prompt-babbler:babbles',
  speechLang: 'prompt-babbler:settings:speechLang',
} as const;

function read<T>(key: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(key);
    if (raw === null) return fallback;
    return JSON.parse(raw) as T;
  } catch {
    return fallback;
  }
}

function write<T>(key: string, value: T): void {
  localStorage.setItem(key, JSON.stringify(value));
}

// Babbles
export function getBabbles(): Babble[] {
  return read<Babble[]>(KEYS.babbles, []);
}

export function saveBabbles(babbles: Babble[]): void {
  write(KEYS.babbles, babbles);
}

export function getBabble(id: string): Babble | undefined {
  return getBabbles().find((b) => b.id === id);
}

export function createBabble(babble: Babble): Babble {
  const babbles = getBabbles();
  babbles.push(babble);
  saveBabbles(babbles);
  return babble;
}

export function updateBabble(updated: Babble): Babble {
  const babbles = getBabbles().map((b) => (b.id === updated.id ? updated : b));
  saveBabbles(babbles);
  return updated;
}

export function deleteBabble(id: string): void {
  const babbles = getBabbles().filter((b) => b.id !== id);
  saveBabbles(babbles);
}

// Speech language
export function getSpeechLanguage(): string {
  return read<string>(KEYS.speechLang, '');
}

export function setSpeechLanguage(lang: string): void {
  write(KEYS.speechLang, lang);
}

// Storage usage
export function getStorageUsage(): {
  used: number;
  quota: number;
  percentage: number;
} {
  let used = 0;
  for (let i = 0; i < localStorage.length; i++) {
    const key = localStorage.key(i);
    if (key) {
      const value = localStorage.getItem(key);
      if (value) {
        used += key.length + value.length;
      }
    }
  }
  // localStorage quota is typically ~5MB (in UTF-16 chars, so ~10MB bytes)
  const quota = 5 * 1024 * 1024;
  return {
    used,
    quota,
    percentage: Math.round((used / quota) * 100),
  };
}

export function isStorageWarning(): boolean {
  return getStorageUsage().percentage >= 80;
}

export function isStorageFull(): boolean {
  return getStorageUsage().percentage >= 95;
}
