import { Moon, Sun } from 'lucide-react';
import { useTheme } from '@/hooks/useTheme';
import { cn } from '@/lib/utils';

interface ThemeToggleProps {
  className?: string;
}

export function ThemeToggle({ className }: ThemeToggleProps) {
  const { resolvedTheme, setTheme } = useTheme();
  const isDark = resolvedTheme === 'dark';

  return (
    <button
      type="button"
      onClick={() => setTheme(isDark ? 'light' : 'dark')}
      aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
      className={cn(
        'inline-flex items-center gap-0.5 rounded-full border border-border bg-muted/40 p-0.5',
        'cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background',
        className,
      )}
    >
      <span
        aria-hidden="true"
        data-testid="sun-segment"
        className={cn(
          'flex h-7 w-7 items-center justify-center rounded-full transition-all duration-200',
          !isDark ? 'bg-background text-amber-500 shadow-sm' : 'text-muted-foreground',
        )}
      >
        <Sun className="h-3.5 w-3.5" />
      </span>

      <span
        aria-hidden="true"
        data-testid="moon-segment"
        className={cn(
          'flex h-7 w-7 items-center justify-center rounded-full transition-all duration-200',
          isDark
            ? 'bg-background text-violet-400 shadow-sm dark:bg-zinc-800'
            : 'text-muted-foreground',
        )}
      >
        <Moon className="h-3.5 w-3.5" />
      </span>
    </button>
  );
}
