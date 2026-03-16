import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { useNavigate } from 'react-router';
import { CircleUser, LogIn, LogOut, Settings } from 'lucide-react';
import { isAuthConfigured, loginRequest } from '@/auth/authConfig';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

/** Extracts up to two initials from a display name. */
function getInitials(name: string | undefined): string {
  if (!name) return '?';
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  return parts[0][0]?.toUpperCase() ?? '?';
}

/** Dropdown menu for authenticated users (rendered inside MsalProvider). */
function AuthenticatedUserMenu() {
  const isAuthenticated = useIsAuthenticated();
  const { instance, accounts } = useMsal();
  const navigate = useNavigate();

  const account = accounts[0];
  const displayName = account?.name ?? account?.username ?? '';
  const email = account?.username ?? '';

  if (!isAuthenticated) {
    return (
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="sm">
            <LogIn className="size-4" />
            Sign in
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          <DropdownMenuItem onClick={() => void instance.loginPopup(loginRequest)}>
            <LogIn className="size-4" />
            Sign in with Entra ID
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={() => void navigate('/settings')}>
            <Settings className="size-4" />
            Settings
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    );
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="gap-2">
          <span className="flex size-6 items-center justify-center rounded-full bg-primary text-[10px] font-semibold text-primary-foreground">
            {getInitials(displayName)}
          </span>
          <span className="max-w-[120px] truncate text-sm">{displayName}</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="font-normal">
          <div className="flex flex-col space-y-1">
            <p className="text-sm font-medium leading-none">{displayName}</p>
            {email && (
              <p className="text-xs leading-none text-muted-foreground">{email}</p>
            )}
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
          <DropdownMenuItem onClick={() => void navigate('/settings')}>
            <Settings className="size-4" />
            Settings
          </DropdownMenuItem>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={() => void instance.logoutPopup()}>
          <LogOut className="size-4" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/** User menu shown when Entra ID auth is not configured (anonymous mode). */
function AnonymousUserMenu() {
  const navigate = useNavigate();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className="gap-2 opacity-60"
          title="Entra ID SSO is not enabled. Running in anonymous single-user mode."
        >
          <CircleUser className="size-5" />
          <span className="text-sm">Anonymous</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="font-normal">
          <div className="flex flex-col space-y-1">
            <p className="text-sm font-medium leading-none">Anonymous Mode</p>
            <p className="text-xs leading-none text-muted-foreground">
              Entra ID SSO is not configured.
            </p>
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
          <DropdownMenuItem onClick={() => void navigate('/settings')}>
            <Settings className="size-4" />
            Settings
          </DropdownMenuItem>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          disabled
          title="Enable Entra ID SSO to sign in. See QUICKSTART.md for setup instructions."
        >
          <LogIn className="size-4" />
          Sign in
          <span className="ml-auto text-xs text-muted-foreground">Disabled</span>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/**
 * User menu component for the header. Renders different UI based on auth mode:
 * - Auth configured + signed in: user initials, name, dropdown with Settings + Sign out
 * - Auth configured + not signed in: Sign in button with dropdown
 * - Auth not configured: Anonymous indicator with dropdown (Settings + disabled Sign in)
 */
export function UserMenu() {
  if (isAuthConfigured) {
    return <AuthenticatedUserMenu />;
  }
  return <AnonymousUserMenu />;
}
