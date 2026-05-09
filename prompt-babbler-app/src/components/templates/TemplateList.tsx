import type { PromptTemplate } from '@/types';
import { Button } from '@/components/ui/button';
import { Plus } from 'lucide-react';
import { TemplateListSection } from '@/components/templates/TemplateListSection';
import type { TemplateOrder } from '@/hooks/useTemplateList';

interface TemplateListProps {
  templates: PromptTemplate[];
  onSelect: (template: PromptTemplate) => void;
  onCreate: () => void;
  nameFilter: string;
  onNameFilterChange: (value: string) => void;
  tagFilter: string;
  onTagFilterChange: (value: string) => void;
  order: TemplateOrder;
  onOrderChange: (value: TemplateOrder) => void;
  loadMore: () => void;
  loadingMore: boolean;
  loading: boolean;
}

export function TemplateList({
  templates,
  onSelect,
  onCreate,
  nameFilter,
  onNameFilterChange,
  tagFilter,
  onTagFilterChange,
  order,
  onOrderChange,
  loadMore,
  loadingMore,
  loading,
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
      <TemplateListSection
        templates={templates}
        nameFilter={nameFilter}
        onNameFilterChange={onNameFilterChange}
        tagFilter={tagFilter}
        onTagFilterChange={onTagFilterChange}
        order={order}
        onOrderChange={onOrderChange}
        onSelect={onSelect}
        loadMore={loadMore}
        loadingMore={loadingMore}
        loading={loading}
      />
    </div>
  );
}
