import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CopyButton } from '@/components/prompts/CopyButton';

describe('CopyButton', () => {
  beforeEach(() => {
    Object.assign(navigator, {
      clipboard: { writeText: vi.fn().mockResolvedValue(undefined) },
    });
  });

  it('renders copy button', () => {
    render(<CopyButton text="hello" />);
    expect(screen.getByRole('button', { name: /copy/i })).toBeInTheDocument();
  });

  it('renders without crashing', () => {
    const { container } = render(<CopyButton text="test" />);
    expect(container).toBeTruthy();
  });
});
