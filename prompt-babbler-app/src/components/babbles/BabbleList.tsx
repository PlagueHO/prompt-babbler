import type { Babble } from '@/types';
import { BabbleCard } from './BabbleCard';

interface BabbleListProps {
  babbles: Babble[];
}

export function BabbleList({ babbles }: BabbleListProps) {
  const sorted = [...babbles].sort(
    (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
  );

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {sorted.map((babble) => (
        <BabbleCard key={babble.id} babble={babble} />
      ))}
    </div>
  );
}
