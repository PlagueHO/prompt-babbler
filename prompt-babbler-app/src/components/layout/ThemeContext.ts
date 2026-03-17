import { createContext } from 'react';
import type { ThemeMode } from '@/types';

export interface ThemeContextValue {
  theme: ThemeMode;
  setTheme: (mode: ThemeMode) => void;
  resolvedTheme: 'light' | 'dark';
}

export const ThemeContext = createContext<ThemeContextValue | undefined>(
  undefined,
);
