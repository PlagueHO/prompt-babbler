import { Badge } from '@/components/ui/badge';
import { TagList } from '@/components/ui/tag-list';
import { cn } from '@/lib/utils';
import type { PromptTemplate } from '@/types';

interface TemplateListItemProps {
  template: PromptTemplate;
  onSelect: (template: PromptTemplate) => void;
}

export function TemplateListItem({ template, onSelect }: TemplateListItemProps) {
  const updatedAt = new Date(template.updatedAt).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  return (
    <tr
      className={cn(
        'cursor-pointer transition-colors hover:bg-accent/50',
        template.isBuiltIn && 'bg-primary/5',
      )}
      onClick={() => onSelect(template)}
    >
      <td className="py-2 pl-2 pr-4 whitespace-nowrap">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium hover:underline">{template.name}</span>
          {template.isBuiltIn && <Badge variant="secondary">Built-in</Badge>}
        </div>
      </td>
      <td className="w-full py-2 pr-4">
        <p className="line-clamp-2 text-xs text-muted-foreground">{template.description}</p>
      </td>
      <td className="py-2 pr-4 whitespace-nowrap">
        <TagList tags={template.tags} />
      </td>
      <td className="py-2 pr-2 whitespace-nowrap text-xs text-muted-foreground">
        {updatedAt}
      </td>
    </tr>
  );
}
