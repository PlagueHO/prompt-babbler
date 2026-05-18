import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeToggle } from '@/components/layout/ThemeToggle';
import { useTheme } from '@/hooks/useTheme';
import type { ThemeMode } from '@/types';

vi.mock('@/hooks/useTheme', () => ({
  useTheme: vi.fn(),
}));

describe('ThemeToggle', () => {
  const mockSetTheme = vi.fn();
  const mockedUseTheme = vi.mocked(useTheme);

  beforeEach(() => {
    vi.clearAllMocks();
  });

  function mockTheme(resolvedTheme: 'light' | 'dark', theme: ThemeMode = resolvedTheme) {
    mockedUseTheme.mockReturnValue({
      theme,
      resolvedTheme,
      setTheme: mockSetTheme,
    });
  }

  it('shows light segment as active when light mode is active', () => {
    mockTheme('light');
    render(<ThemeToggle />);

    const toggle = screen.getByRole('button', { name: 'Switch to dark mode' });
    expect(toggle).toBeInTheDocument();
    expect(screen.getByTestId('sun-segment')).toHaveClass('bg-background');
    expect(screen.getByTestId('moon-segment')).toHaveClass('text-muted-foreground');
  });

  it('shows dark segment as active when dark mode is active', () => {
    mockTheme('dark');
    render(<ThemeToggle />);

    const toggle = screen.getByRole('button', { name: 'Switch to light mode' });
    expect(toggle).toBeInTheDocument();
    expect(screen.getByTestId('moon-segment')).toHaveClass('bg-background');
    expect(screen.getByTestId('sun-segment')).toHaveClass('text-muted-foreground');
  });

  it('sets dark mode when toggled on from light mode', async () => {
    mockTheme('light');
    render(<ThemeToggle />);

    await userEvent.click(screen.getByRole('button', { name: 'Switch to dark mode' }));

    expect(mockSetTheme).toHaveBeenCalledWith('dark');
  });

  it('sets light mode when toggled off from dark mode', async () => {
    mockTheme('dark');
    render(<ThemeToggle />);

    await userEvent.click(screen.getByRole('button', { name: 'Switch to light mode' }));

    expect(mockSetTheme).toHaveBeenCalledWith('light');
  });
});
