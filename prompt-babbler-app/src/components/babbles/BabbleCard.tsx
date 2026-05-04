import { Link } from 'react-router';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card';
import { TagList } from '@/components/ui/tag-list';
import type { Babble } from '@/types';

interface BabbleCardProps {
  babble: Babble;
}

export function BabbleCard({ babble }: BabbleCardProps) {
  const dateStr = new Date(babble.updatedAt).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  const truncated =
    babble.text.length > 150
      ? `${babble.text.slice(0, 150)}…`
      : babble.text;

  return (
    <Link to={`/babble/${babble.id}`} className="block">
      <Card className="transition-colors hover:bg-accent/50">
        <CardHeader>
          <CardTitle className="text-base">{babble.title}</CardTitle>
          <TagList tags={babble.tags} className="mt-1" />
          <CardDescription>{dateStr}</CardDescription>
        </CardHeader>
        <CardContent>
          <p className="line-clamp-3 text-sm text-muted-foreground">
            {truncated || 'No content yet.'}
          </p>
        </CardContent>
      </Card>
    </Link>
  );
}
