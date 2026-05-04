import { Badge } from '@/components/ui/badge';
import { getTagColor } from '@/lib/tag-colors';

interface TagListProps {
  tags: string[] | undefined;
  className?: string;
}

export function TagList({ tags, className }: TagListProps) {
  if (!tags || tags.length === 0) return null;

  return (
    <div className={`flex flex-wrap gap-1 ${className ?? ''}`}>
      {tags.map((tag) => (
        <Badge key={tag} variant="outline" className={`text-xs border-transparent ${getTagColor(tag)}`}>
          {tag}
        </Badge>
      ))}
    </div>
  );
}
