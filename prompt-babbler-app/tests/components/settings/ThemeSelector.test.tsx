import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ThemeSelector } from '@/components/settings/ThemeSelector';

describe('ThemeSelector', () => {
  it('renders the label and current value', () => {
    render(<ThemeSelector value="system" onChange={vi.fn()} />);
    expect(screen.getByText('Theme')).toBeInTheDocument();
    expect(screen.getByText('System')).toBeInTheDocument();
  });

  it('shows Light when light is selected', () => {
    render(<ThemeSelector value="light" onChange={vi.fn()} />);
    expect(screen.getByText('Light')).toBeInTheDocument();
  });

  it('shows Dark when dark is selected', () => {
    render(<ThemeSelector value="dark" onChange={vi.fn()} />);
    expect(screen.getByText('Dark')).toBeInTheDocument();
  });

  it('displays description text', () => {
    render(<ThemeSelector value="light" onChange={vi.fn()} />);
    expect(
      screen.getByText(/automatically match your browser or OS/),
    ).toBeInTheDocument();
  });
});
