import { Link } from 'react-router';
import { Pin, PinOff } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { TagList } from '@/components/ui/tag-list';
import { cn } from '@/lib/utils';
import type { Babble } from '@/types';

interface BabbleListItemProps {
  babble: Babble;
  onTogglePin: (babbleId: string) => void;
}

export function BabbleListItem({ babble, onTogglePin }: BabbleListItemProps) {
  const dateStr = new Date(babble.createdAt).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  return (
    <div
      className={cn(
        'flex items-center gap-3 rounded-md border px-4 py-3 transition-colors hover:bg-accent/50',
        babble.isPinned && 'border-primary/30 bg-primary/5',
      )}
    >
      <div className="min-w-0 flex-1">
        <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:gap-3">
          <Link
            to={`/babble/${babble.id}`}
            className="truncate text-sm font-medium hover:underline"
          >
            {babble.title}
          </Link>
          <span className="shrink-0 text-xs text-muted-foreground">{dateStr}</span>
        </div>
        {babble.tags && babble.tags.length > 0 && (
          <TagList tags={babble.tags} className="mt-1" />
        )}
      </div>
      <Button
        variant="ghost"
        size="icon"
        className={cn(
          'size-7 shrink-0 text-muted-foreground hover:text-foreground',
          babble.isPinned && 'text-primary',
        )}
        onClick={() => onTogglePin(babble.id)}
        aria-label={babble.isPinned ? 'Unpin babble' : 'Pin babble'}
      >
        {babble.isPinned ? (
          <Pin className="size-3.5 fill-current" />
        ) : (
          <PinOff className="size-3.5" />
        )}
      </Button>
    </div>
  );
}
