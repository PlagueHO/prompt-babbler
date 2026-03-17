import { useContext } from 'react';
import { ThemeContext } from '@/components/layout/ThemeContext';
import type { ThemeContextValue } from '@/components/layout/ThemeContext';

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within a ThemeProvider');
  return ctx;
}
