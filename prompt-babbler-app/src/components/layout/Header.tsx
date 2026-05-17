import { NavLink } from 'react-router';
import { Home, Mic, FileText } from 'lucide-react';
import { cn } from '@/lib/utils';
import { UserMenu } from '@/components/layout/UserMenu';
import { SearchBar } from '@/components/search/SearchBar';

const navItems = [
  { to: '/', label: 'Home', icon: Home },
  { to: '/record', label: 'New Babble', icon: Mic },
  { to: '/templates', label: 'Templates', icon: FileText },
] as const;

export function Header() {
  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex h-14 w-full max-w-7xl items-center gap-4 px-4">
        {/* Left: Icon + Heading */}
        <NavLink to="/" className="flex items-center gap-2">
          <Mic className="size-5 text-primary" />
          <span className="gradient-brand text-lg font-bold tracking-tight">
            Prompt Babbler
          </span>
        </NavLink>

        {/* Navigation */}
        <nav className="flex items-center gap-1">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
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
        </nav>

        {/* Center: Search */}
        <div className="ml-auto mr-2 hidden sm:block">
          <SearchBar />
        </div>

        {/* Right: User Menu */}
        <UserMenu />
      </div>
    </header>
  );
}
