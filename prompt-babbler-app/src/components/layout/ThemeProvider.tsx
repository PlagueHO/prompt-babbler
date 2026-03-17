import {
  useEffect,
  useState,
  useSyncExternalStore,
  useCallback,
} from 'react';
import type { ReactNode } from 'react';
import type { ThemeMode } from '@/types';
import { getThemeMode, setThemeMode } from '@/services/local-storage';
import { ThemeContext } from './ThemeContext';

export type { ThemeContextValue } from './ThemeContext';

function getSystemThemeSnapshot(): 'light' | 'dark' {
  return window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light';
}

function subscribeToSystemTheme(callback: () => void): () => void {
  const mql = window.matchMedia('(prefers-color-scheme: dark)');
  mql.addEventListener('change', callback);
  return () => mql.removeEventListener('change', callback);
}

function applyTheme(resolved: 'light' | 'dark') {
  const root = document.documentElement;
  if (resolved === 'dark') {
    root.classList.add('dark');
  } else {
    root.classList.remove('dark');
  }
}

interface ThemeProviderProps {
  children: ReactNode;
}

export function ThemeProvider({ children }: ThemeProviderProps) {
  const [theme, setThemeState] = useState<ThemeMode>(
    () => (getThemeMode() as ThemeMode) || 'system',
  );

  const systemTheme = useSyncExternalStore(
    subscribeToSystemTheme,
    getSystemThemeSnapshot,
    () => 'light' as const,
  );

  const resolvedTheme = theme === 'system' ? systemTheme : theme;

  const setTheme = useCallback((mode: ThemeMode) => {
    setThemeState(mode);
    setThemeMode(mode);
  }, []);

  // Apply the .dark class whenever resolvedTheme changes
  useEffect(() => {
    applyTheme(resolvedTheme);
  }, [resolvedTheme]);

  return (
    <ThemeContext.Provider value={{ theme, setTheme, resolvedTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}
