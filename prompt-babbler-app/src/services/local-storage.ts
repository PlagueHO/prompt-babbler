import type { Babble, PromptTemplate } from '@/types';
import { DEFAULT_TEMPLATES } from './default-templates';

const KEYS = {
  babbles: 'prompt-babbler:babbles',
  templates: 'prompt-babbler:templates',
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

// Templates
export function getTemplates(): PromptTemplate[] {
  const stored = read<PromptTemplate[] | null>(KEYS.templates, null);
  if (stored === null) {
    write(KEYS.templates, DEFAULT_TEMPLATES);
    return [...DEFAULT_TEMPLATES];
  }
  return stored;
}

export function saveTemplates(templates: PromptTemplate[]): void {
  write(KEYS.templates, templates);
}

export function createTemplate(template: PromptTemplate): PromptTemplate {
  const templates = getTemplates();
  templates.push(template);
  saveTemplates(templates);
  return template;
}

export function updateTemplate(updated: PromptTemplate): PromptTemplate {
  const templates = getTemplates().map((t) =>
    t.id === updated.id ? updated : t
  );
  saveTemplates(templates);
  return updated;
}

export function deleteTemplate(id: string): void {
  const templates = getTemplates().filter((t) => t.id !== id);
  saveTemplates(templates);
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
