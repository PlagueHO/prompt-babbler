import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { Lock } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useMsal } from '@azure/msal-react';
import { isAuthConfigured, loginRequest } from '@/auth/authConfig';
import type { ReactNode } from 'react';

interface SignInPromptProps {
  message?: string;
}

function SignInPrompt({ message }: SignInPromptProps) {
  const { instance } = useMsal();

  return (
    <div className="flex flex-col items-center gap-4 rounded-lg border border-dashed p-12 text-center">
      <div className="rounded-full bg-muted p-4">
        <Lock className="size-8 text-muted-foreground" />
      </div>
      <div>
        <h2 className="text-lg font-semibold">Sign in to continue</h2>
        <p className="text-sm text-muted-foreground">
          {message ?? 'Sign in with your organizational account to access this feature.'}
        </p>
      </div>
      <Button onClick={() => void instance.loginPopup(loginRequest)}>
        Sign in
      </Button>
    </div>
  );
}

interface AuthGuardProps {
  children: ReactNode;
  message?: string;
}

export function AuthGuard({ children, message }: AuthGuardProps) {
  // When Entra ID is not configured, bypass auth — single-user anonymous mode.
  if (!isAuthConfigured) {
    return <>{children}</>;
  }

  return (
    <>
      <AuthenticatedTemplate>{children}</AuthenticatedTemplate>
      <UnauthenticatedTemplate>
        <SignInPrompt message={message} />
      </UnauthenticatedTemplate>
    </>
  );
}
