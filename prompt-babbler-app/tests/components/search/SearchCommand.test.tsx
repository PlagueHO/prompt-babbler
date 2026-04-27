import { describe, it, expect, vi, beforeAll } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { SearchCommand } from '@/components/search/SearchCommand';

beforeAll(() => {
  global.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

vi.mock('@/hooks/useSemanticSearch', () => ({
  useSemanticSearch: vi.fn().mockReturnValue({
    results: [],
    loading: false,
    error: null,
  }),
}));

vi.mock('react-router', async () => {
  const actual = await vi.importActual('react-router');
  return {
    ...actual,
    useNavigate: vi.fn().mockReturnValue(vi.fn()),
  };
});

describe('SearchCommand', () => {
  it('should open on Ctrl+K keyboard shortcut', () => {
    render(
      <MemoryRouter>
        <SearchCommand />
      </MemoryRouter>
    );

    // Dialog should not be visible initially
    expect(screen.queryByPlaceholderText('Search babbles...')).not.toBeInTheDocument();

    // Simulate Ctrl+K
    fireEvent.keyDown(document, { key: 'k', ctrlKey: true });

    // Dialog should now be visible
    expect(screen.queryByPlaceholderText('Search babbles...')).toBeInTheDocument();
  });

  it('should show "Type to search..." when query is short', () => {
    render(
      <MemoryRouter>
        <SearchCommand />
      </MemoryRouter>
    );

    // Open the dialog
    fireEvent.keyDown(document, { key: 'k', ctrlKey: true });

    expect(screen.getByText('Type to search...')).toBeInTheDocument();
  });
});
