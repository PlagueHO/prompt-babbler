import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { BabbleList } from '@/components/babbles/BabbleList';
import type { Babble } from '@/types';

function makeBabble(id: string, title: string): Babble {
  return {
    id,
    title,
    text: `Content for ${title}`,
    createdAt: '2024-01-15T10:00:00.000Z',
    updatedAt: '2024-01-15T10:00:00.000Z',
    lastGeneratedPrompt: null,
  };
}

describe('BabbleList', () => {
  it('renders list of babbles', () => {
    const babbles = [makeBabble('b1', 'First'), makeBabble('b2', 'Second')];
    render(
      <MemoryRouter>
        <BabbleList babbles={babbles} />
      </MemoryRouter>
    );
    expect(screen.getByText('First')).toBeInTheDocument();
    expect(screen.getByText('Second')).toBeInTheDocument();
  });

  it('shows empty state when no babbles', () => {
    const { container } = render(
      <MemoryRouter>
        <BabbleList babbles={[]} />
      </MemoryRouter>
    );
    // With an empty list, there should be no card elements
    expect(container.querySelectorAll('[class*="card"]').length).toBe(0);
  });
});
