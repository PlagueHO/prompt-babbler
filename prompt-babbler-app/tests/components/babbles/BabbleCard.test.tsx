import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { BabbleCard } from '@/components/babbles/BabbleCard';
import type { Babble } from '@/types';

const babble: Babble = {
  id: 'b1',
  title: 'My First Babble',
  text: 'Some babble text content here',
  createdAt: '2024-01-15T10:00:00.000Z',
  updatedAt: '2024-01-15T10:00:00.000Z',
  lastGeneratedPrompt: null,
};

describe('BabbleCard', () => {
  it('renders babble title and date', () => {
    render(
      <MemoryRouter>
        <BabbleCard babble={babble} />
      </MemoryRouter>
    );
    expect(screen.getByText('My First Babble')).toBeInTheDocument();
    expect(screen.getByText(/jan/i)).toBeInTheDocument();
  });

  it('renders truncated text preview', () => {
    const longBabble: Babble = {
      ...babble,
      text: 'A'.repeat(200),
    };
    render(
      <MemoryRouter>
        <BabbleCard babble={longBabble} />
      </MemoryRouter>
    );
    const preview = screen.getByText(/A+…/);
    expect(preview).toBeInTheDocument();
  });
});
