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
    <tr
      className={cn(
        'transition-colors hover:bg-accent/50',
        babble.isPinned && 'bg-primary/5',
      )}
    >
      <td className="py-2 pl-2 pr-4 whitespace-nowrap">
        <Link
          to={`/babble/${babble.id}`}
          className="text-sm font-medium hover:underline"
        >
          {babble.title}
        </Link>
      </td>
      <td className="w-full py-2 pr-4">
        <TagList tags={babble.tags} />
      </td>
      <td className="py-2 pr-2 whitespace-nowrap text-xs text-muted-foreground">
        {dateStr}
      </td>
      <td className="py-2 pr-2">
        <Button
          variant="ghost"
          size="icon"
          className={cn(
            'size-7 text-muted-foreground hover:text-foreground',
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
      </td>
    </tr>
  );
}
