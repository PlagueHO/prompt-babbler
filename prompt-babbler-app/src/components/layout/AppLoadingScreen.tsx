import type { LucideIcon } from 'lucide-react';
import { Mic } from 'lucide-react';
import { cn } from '@/lib/utils';

interface AppLoadingScreenProps {
  icon?: LucideIcon;
  className?: string;
  spinnerClassName?: string;
  iconClassName?: string;
}

export function AppLoadingScreen({
  icon: Icon = Mic,
  className,
  spinnerClassName,
  iconClassName,
}: AppLoadingScreenProps) {
  return (
    <div className={cn('flex h-screen items-center justify-center', className)}>
      <div className="relative flex h-24 w-24 items-center justify-center">
        <div
          className={cn(
            'h-24 w-24 animate-spin rounded-full border-4 border-muted border-t-primary',
            spinnerClassName
          )}
        />
        <Icon className={cn('absolute size-9 text-primary', iconClassName)} aria-hidden="true" />
      </div>
    </div>
  );
}