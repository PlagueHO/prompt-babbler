import { Link } from 'react-router';
import { Pin, PinOff } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card';
import { TagList } from '@/components/ui/tag-list';
import { cn } from '@/lib/utils';
import type { Babble } from '@/types';

interface BabbleBubblesProps {
  babbles: Babble[];
  onTogglePin: (babbleId: string) => void;
}

export function BabbleBubbles({ babbles, onTogglePin }: BabbleBubblesProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {babbles.map((babble) => (
        <BabbleBubbleCard key={babble.id} babble={babble} onTogglePin={onTogglePin} />
      ))}
    </div>
  );
}

interface BabbleBubbleCardProps {
  babble: Babble;
  onTogglePin: (babbleId: string) => void;
}

function BabbleBubbleCard({ babble, onTogglePin }: BabbleBubbleCardProps) {
  const dateStr = new Date(babble.updatedAt).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  const truncated =
    babble.text.length > 150 ? `${babble.text.slice(0, 150)}…` : babble.text;

  return (
    <div className="relative group">
      <Link to={`/babble/${babble.id}`} className="block">
        <Card
          className={cn(
            'transition-colors hover:bg-accent/50',
            babble.isPinned && 'border-primary/30 bg-primary/5',
          )}
        >
          <CardHeader className="pr-10">
            <CardTitle className="text-base">{babble.title}</CardTitle>
            <div className="flex items-center gap-2 mt-1">
              <TagList tags={babble.tags} className="flex-1" />
              <CardDescription className="shrink-0">{dateStr}</CardDescription>
            </div>
          </CardHeader>
          <CardContent>
            <p className="line-clamp-3 text-sm text-muted-foreground">
              {truncated || 'No content yet.'}
            </p>
          </CardContent>
        </Card>
      </Link>
      <Button
        variant="ghost"
        size="icon"
        className={cn(
          'absolute right-2 top-2 size-7 opacity-0 group-hover:opacity-100 transition-opacity',
          babble.isPinned && 'opacity-100 text-primary',
        )}
        onClick={(e) => {
          e.preventDefault();
          onTogglePin(babble.id);
        }}
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
