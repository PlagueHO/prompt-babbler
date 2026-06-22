import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { Header } from '@/components/layout/Header';

vi.mock('@/components/search/SearchBar', () => ({
  SearchBar: () => <div data-testid="search-bar" />,
}));

vi.mock('@/components/layout/UserMenu', () => ({
  UserMenu: () => <div data-testid="user-menu" />,
}));

vi.mock('@/components/layout/ThemeToggle', () => ({
  ThemeToggle: () => <div data-testid="theme-toggle" />,
}));

describe('Header', () => {
  it('renders the brand name', () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );
    expect(screen.getByText('Prompt Babbler')).toBeInTheDocument();
  });

  it('renders the desktop navigation links', () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );
    expect(screen.getByRole('link', { name: /home/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /new babble/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /templates/i })).toBeInTheDocument();
  });

  it('renders the mobile menu toggle button', () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );
    expect(screen.getByRole('button', { name: 'Open menu' })).toBeInTheDocument();
  });

  it('does not show mobile navigation panel by default', () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );
    expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument();
  });

  it('opens mobile navigation panel when hamburger is clicked', async () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );

    await userEvent.click(screen.getByRole('button', { name: 'Open menu' }));

    expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Close menu' })).toBeInTheDocument();
  });

  it('closes mobile navigation panel when hamburger is clicked again', async () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );

    await userEvent.click(screen.getByRole('button', { name: 'Open menu' }));
    await userEvent.click(screen.getByRole('button', { name: 'Close menu' }));

    expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument();
  });

  it('closes mobile menu when a nav link is clicked', async () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );

    await userEvent.click(screen.getByRole('button', { name: 'Open menu' }));

    const mobileNav = screen.getByRole('navigation', { name: 'Mobile navigation' });
    const firstLink = mobileNav.querySelector('a');
    expect(firstLink).not.toBeNull();
    await userEvent.click(firstLink!);

    expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument();
  });

  it('shows search bar inside mobile navigation panel', async () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );

    await userEvent.click(screen.getByRole('button', { name: 'Open menu' }));

    const mobileNav = screen.getByRole('navigation', { name: 'Mobile navigation' });
    expect(mobileNav.querySelector('[data-testid="search-bar"]')).toBeInTheDocument();
  });
});
