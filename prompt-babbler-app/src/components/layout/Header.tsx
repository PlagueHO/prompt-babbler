import { NavLink } from 'react-router';
import { Home, Mic, FileText, Search } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { UserMenu } from '@/components/layout/UserMenu';

const isMac = /Mac/i.test(navigator.platform);

const navItems = [
  { to: '/', label: 'Home', icon: Home },
  { to: '/record', label: 'New Babble', icon: Mic },
  { to: '/templates', label: 'Templates', icon: FileText },
] as const;

export function Header() {
  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex h-14 max-w-5xl items-center gap-6 px-4">
        <NavLink to="/" className="gradient-brand text-lg font-bold tracking-tight">
          Prompt Babbler
        </NavLink>
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
        <div className="ml-auto flex items-center gap-2">
          <Button
            variant="outline"
            className="relative h-9 w-9 p-0 xl:h-10 xl:w-60 xl:justify-start xl:px-3 xl:py-2"
            onClick={() => document.dispatchEvent(new CustomEvent('babble:open-search'))}
            aria-label="Search babbles"
          >
            <Search className="h-4 w-4 xl:mr-2" />
            <span className="hidden xl:inline-flex">Search babbles...</span>
            <kbd className="pointer-events-none absolute right-1.5 top-2 hidden h-6 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-xs font-medium opacity-100 xl:flex">
              {isMac ? <><span className="text-xs">⌘</span>K</> : <>Ctrl K</>}
            </kbd>
          </Button>
          <UserMenu />
        </div>
      </div>
    </header>
  );
}
