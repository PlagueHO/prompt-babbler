const KEYS = {
  speechLang: 'prompt-babbler:settings:speechLang',
  theme: 'prompt-babbler:settings:theme',
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

// Speech language
export function getSpeechLanguage(): string {
  return read<string>(KEYS.speechLang, '');
}

export function setSpeechLanguage(lang: string): void {
  write(KEYS.speechLang, lang);
}

// Theme mode
export function getThemeMode(): string {
  return read<string>(KEYS.theme, 'system');
}

export function setThemeMode(mode: string): void {
  write(KEYS.theme, mode);
}
