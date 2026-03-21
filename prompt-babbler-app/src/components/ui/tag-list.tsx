import { Badge } from '@/components/ui/badge';

interface TagListProps {
  tags: string[] | undefined;
  className?: string;
}

export function TagList({ tags, className }: TagListProps) {
  if (!tags || tags.length === 0) return null;

  return (
    <div className={`flex flex-wrap gap-1 ${className ?? ''}`}>
      {tags.map((tag) => (
        <Badge key={tag} variant="outline" className="text-xs">
          {tag}
        </Badge>
      ))}
    </div>
  );
}
