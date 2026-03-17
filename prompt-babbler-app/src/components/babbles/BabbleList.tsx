import { Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import type { Babble } from '@/types';
import { BabbleCard } from './BabbleCard';

interface BabbleListProps {
  babbles: Babble[];
  hasMore?: boolean;
  loading?: boolean;
  onLoadMore?: () => void;
}

export function BabbleList({ babbles, hasMore, loading, onLoadMore }: BabbleListProps) {
  const sorted = [...babbles].sort(
    (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
  );

  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {sorted.map((babble) => (
          <BabbleCard key={babble.id} babble={babble} />
        ))}
      </div>

      {loading && (
        <div className="flex justify-center py-2">
          <Loader2 className="size-5 animate-spin text-muted-foreground" />
        </div>
      )}

      {hasMore && !loading && onLoadMore && (
        <Button variant="outline" className="w-full" onClick={onLoadMore}>
          Load More
        </Button>
      )}
    </div>
  );
}
