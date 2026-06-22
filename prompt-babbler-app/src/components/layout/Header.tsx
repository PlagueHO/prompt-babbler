import { useState } from 'react';
import { NavLink } from 'react-router';
import { Home, Mic, FileText, Menu, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { UserMenu } from '@/components/layout/UserMenu';
import { ThemeToggle } from '@/components/layout/ThemeToggle';
import { SearchBar } from '@/components/search/SearchBar';
import { Button } from '@/components/ui/button';

const navItems = [
  { to: '/', label: 'Home', icon: Home },
  { to: '/record', label: 'New Babble', icon: Mic },
  { to: '/templates', label: 'Templates', icon: FileText },
] as const;

function NavItems({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <>
      {navItems.map(({ to, label, icon: Icon }) => (
        <NavLink
          key={to}
          to={to}
          end={to === '/'}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              'inline-flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors',
              isActive
                ? 'bg-accent text-accent-foreground'
                : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
            )
          }
        >
          <Icon className="size-4" />
          {label}
        </NavLink>
      ))}
    </>
  );
}

export function Header() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex h-14 w-full max-w-7xl items-center gap-4 px-4">
        {/* Brand */}
        <NavLink to="/" className="flex shrink-0 items-center gap-2">
          <Mic className="size-5 text-primary" />
          <span className="gradient-brand text-lg font-bold tracking-tight">
            Prompt Babbler
          </span>
        </NavLink>

        {/* Desktop Navigation */}
        <nav className="hidden items-center gap-1 sm:flex">
          <NavItems />
        </nav>

        {/* Spacer */}
        <div className="flex-1" />

        {/* Desktop Search */}
        <div className="mr-2 hidden sm:block">
          <SearchBar />
        </div>

        {/* Theme + User Menu */}
        <div className="flex items-center gap-2">
          <ThemeToggle />
          <UserMenu />
        </div>

        {/* Mobile hamburger */}
        <Button
          variant="ghost"
          size="icon"
          className="sm:hidden"
          onClick={() => setMobileMenuOpen((open) => !open)}
          aria-label={mobileMenuOpen ? 'Close menu' : 'Open menu'}
          aria-expanded={mobileMenuOpen}
        >
          {mobileMenuOpen ? <X className="size-5" /> : <Menu className="size-5" />}
        </Button>
      </div>

      {/* Mobile navigation panel */}
      {mobileMenuOpen && (
        <nav
          className="border-t px-4 py-3 sm:hidden"
          aria-label="Mobile navigation"
        >
          <div className="flex flex-col gap-1">
            <NavItems onNavigate={() => setMobileMenuOpen(false)} />
          </div>
          <div className="mt-3 pb-1">
            <SearchBar />
          </div>
        </nav>
      )}
    </header>
  );
}
