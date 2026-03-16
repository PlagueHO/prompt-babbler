import { NavLink } from 'react-router';
import { Home, Mic, FileText, Settings, LogIn, LogOut } from 'lucide-react';
import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { cn } from '@/lib/utils';
import { loginRequest } from '@/auth/authConfig';
import { Button } from '@/components/ui/button';

const navItems = [
  { to: '/', label: 'Home', icon: Home },
  { to: '/record', label: 'Record', icon: Mic },
  { to: '/templates', label: 'Templates', icon: FileText },
  { to: '/settings', label: 'Settings', icon: Settings },
] as const;

export function Header() {
  const isAuthenticated = useIsAuthenticated();
  const { instance, accounts } = useMsal();

  const handleLogin = () => {
    void instance.loginPopup(loginRequest);
  };

  const handleLogout = () => {
    void instance.logoutPopup();
  };

  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex h-14 max-w-5xl items-center gap-6 px-4">
        <NavLink to="/" className="text-lg font-bold tracking-tight">
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
          {isAuthenticated ? (
            <>
              <span className="text-sm text-muted-foreground">
                {accounts[0]?.name ?? accounts[0]?.username ?? ''}
              </span>
              <Button variant="ghost" size="sm" onClick={handleLogout}>
                <LogOut className="size-4" />
                Sign out
              </Button>
            </>
          ) : (
            <Button variant="ghost" size="sm" onClick={handleLogin}>
              <LogIn className="size-4" />
              Sign in
            </Button>
          )}
        </div>
      </div>
    </header>
  );
}
