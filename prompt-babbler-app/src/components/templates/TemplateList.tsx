import type { PromptTemplate } from '@/types';
import { Button } from '@/components/ui/button';
import { Plus } from 'lucide-react';
import { TemplateCard } from './TemplateCard';

interface TemplateListProps {
  templates: PromptTemplate[];
  onSelect: (template: PromptTemplate) => void;
  onCreate: () => void;
}

export function TemplateList({
  templates,
  onSelect,
  onCreate,
}: TemplateListProps) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Prompt Templates</h2>
        <Button size="sm" onClick={onCreate}>
          <Plus className="size-4" />
          Create Template
        </Button>
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        {templates.map((t) => (
          <TemplateCard
            key={t.id}
            template={t}
            onClick={() => onSelect(t)}
          />
        ))}
      </div>
    </div>
  );
}
