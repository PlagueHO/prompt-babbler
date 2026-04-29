import { describe, it, expect, vi, beforeAll, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { SearchCommand } from '@/components/search/SearchCommand';
import { useSemanticSearch } from '@/hooks/useSemanticSearch';

const mockNavigate = vi.hoisted(() => vi.fn());

beforeAll(() => {
  global.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
  Element.prototype.scrollIntoView = vi.fn();
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
    useNavigate: vi.fn().mockReturnValue(mockNavigate),
  };
});

const mockResults = [
  {
    id: 'babble-1',
    title: 'My First Babble',
    snippet: 'This is a test snippet about something interesting.',
    tags: ['ai', 'test'],
    createdAt: '2026-01-01T00:00:00.000Z',
    isPinned: false,
    score: 0.95,
  },
  {
    id: 'babble-2',
    title: 'Another Babble',
    snippet: 'A second result with no tags.',
    tags: [],
    createdAt: '2026-01-02T00:00:00.000Z',
    isPinned: true,
    score: 0.82,
  },
];

describe('SearchCommand', () => {
  beforeEach(() => {
    vi.mocked(useSemanticSearch).mockReturnValue({ results: [], loading: false, error: null });
    mockNavigate.mockClear();
  });

  it('should open on Ctrl+K keyboard shortcut', () => {
    render(
      <MemoryRouter>
        <SearchCommand />
      </MemoryRouter>
    );

    expect(screen.queryByPlaceholderText('Search babbles...')).not.toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'k', ctrlKey: true });
    expect(screen.queryByPlaceholderText('Search babbles...')).toBeInTheDocument();
  });

  it('should show "Type to search..." when query is short', () => {
    render(
      <MemoryRouter>
        <SearchCommand />
      </MemoryRouter>
    );
    fireEvent.keyDown(document, { key: 'k', ctrlKey: true });
    expect(screen.getByText('Type to search...')).toBeInTheDocument();
  });

  it('should render search results when hook returns data', () => {
    vi.mocked(useSemanticSearch).mockReturnValue({ results: mockResults, loading: false, error: null });

    render(
      <MemoryRouter>
        <SearchCommand />
      </MemoryRouter>
    );
    fireEvent.keyDown(document, { key: 'k', ctrlKey: true });

    expect(screen.getByText('My First Babble')).toBeInTheDocument();
    expect(screen.getByText('Another Babble')).toBeInTheDocument();
    expect(screen.getByText('This is a test snippet about something interesting.')).toBeInTheDocument();
  });

  it('should render result tags', () => {
    vi.mocked(useSemanticSearch).mockReturnValue({ results: mockResults, loading: false, error: null });

    render(
      <MemoryRouter>
        <SearchCommand />
      </MemoryRouter>
    );
    fireEvent.keyDown(document, { key: 'k', ctrlKey: true });

    expect(screen.getByText('ai')).toBeInTheDocument();
    expect(screen.getByText('test')).toBeInTheDocument();
  });

  it('should show loading spinner when loading is true', () => {
    vi.mocked(useSemanticSearch).mockReturnValue({ results: [], loading: true, error: null });

    render(
      <MemoryRouter>
        <SearchCommand />
      </MemoryRouter>
    );
    fireEvent.keyDown(document, { key: 'k', ctrlKey: true });

    const spinner = document.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
  });

  it('should navigate to babble route on result selection', () => {
    vi.mocked(useSemanticSearch).mockReturnValue({ results: mockResults, loading: false, error: null });

    render(
      <MemoryRouter>
        <SearchCommand />
      </MemoryRouter>
    );
    fireEvent.keyDown(document, { key: 'k', ctrlKey: true });

    const item = screen.getByText('My First Babble');
    fireEvent.click(item);

    expect(mockNavigate).toHaveBeenCalledWith('/babble/babble-1');
  });
});
