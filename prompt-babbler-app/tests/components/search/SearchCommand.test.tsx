import { describe, it, expect, vi, beforeAll, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { SearchBar } from '@/components/search/SearchBar';
import { useSearch } from '@/hooks/useSearch';

const mockNavigate = vi.hoisted(() => vi.fn());

beforeAll(() => {
  global.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
  Element.prototype.scrollIntoView = vi.fn();
});

vi.mock('@/hooks/useSearch', () => ({
  useSearch: vi.fn().mockReturnValue({
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

describe('SearchBar', () => {
  beforeEach(() => {
    vi.mocked(useSearch).mockReturnValue({ results: [], loading: false, error: null });
    mockNavigate.mockClear();
  });

  it('should render an always-visible search input', () => {
    render(
      <MemoryRouter>
        <SearchBar />
      </MemoryRouter>
    );

    expect(screen.getByRole('textbox', { name: /search babbles/i })).toBeInTheDocument();
  });

  it('should show results in dropdown after typing', () => {
    vi.mocked(useSearch).mockReturnValue({ results: mockResults, loading: false, error: null });

    render(
      <MemoryRouter>
        <SearchBar />
      </MemoryRouter>
    );

    const input = screen.getByRole('textbox', { name: /search babbles/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'babble' } });

    expect(screen.getByText('My First Babble')).toBeInTheDocument();
    expect(screen.getByText('Another Babble')).toBeInTheDocument();
    expect(screen.getByText('This is a test snippet about something interesting.')).toBeInTheDocument();
  });

  it('should render result tags', () => {
    vi.mocked(useSearch).mockReturnValue({ results: mockResults, loading: false, error: null });

    render(
      <MemoryRouter>
        <SearchBar />
      </MemoryRouter>
    );

    const input = screen.getByRole('textbox', { name: /search babbles/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'babble' } });

    expect(screen.getByText('ai')).toBeInTheDocument();
    expect(screen.getByText('test')).toBeInTheDocument();
  });

  it('should show loading spinner when search is in progress', () => {
    vi.mocked(useSearch).mockReturnValue({ results: [], loading: true, error: null });

    render(
      <MemoryRouter>
        <SearchBar />
      </MemoryRouter>
    );

    const input = screen.getByRole('textbox', { name: /search babbles/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'loading query' } });

    const spinner = document.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
  });

  it('should show "No results found." when search returns empty', () => {
    vi.mocked(useSearch).mockReturnValue({ results: [], loading: false, error: null });

    render(
      <MemoryRouter>
        <SearchBar />
      </MemoryRouter>
    );

    const input = screen.getByRole('textbox', { name: /search babbles/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'nonexistent' } });

    expect(screen.getByText('No results found.')).toBeInTheDocument();
  });

  it('should navigate to babble route when result is clicked', () => {
    vi.mocked(useSearch).mockReturnValue({ results: mockResults, loading: false, error: null });

    render(
      <MemoryRouter>
        <SearchBar />
      </MemoryRouter>
    );

    const input = screen.getByRole('textbox', { name: /search babbles/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'babble' } });

    fireEvent.click(screen.getByText('My First Babble'));

    expect(mockNavigate).toHaveBeenCalledWith('/babble/babble-1');
  });

  it('should clear input and close dropdown on Escape key', () => {
    vi.mocked(useSearch).mockReturnValue({ results: mockResults, loading: false, error: null });

    render(
      <MemoryRouter>
        <SearchBar />
      </MemoryRouter>
    );

    const input = screen.getByRole('textbox', { name: /search babbles/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'babble' } });
    expect(screen.getByText('My First Babble')).toBeInTheDocument();

    fireEvent.keyDown(document, { key: 'Escape' });

    expect(input).toHaveValue('');
    expect(screen.queryByText('My First Babble')).not.toBeInTheDocument();
  });
});
