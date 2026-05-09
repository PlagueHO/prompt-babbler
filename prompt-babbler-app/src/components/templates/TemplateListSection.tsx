import { useCallback, useEffect, useRef } from 'react';
import { ArrowDownAZ, Clock3, Loader2, Search, Tag } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { TemplateListItem } from '@/components/templates/TemplateListItem';
import type { PromptTemplate } from '@/types';
import type { TemplateOrder } from '@/hooks/useTemplateList';

interface TemplateListSectionProps {
  templates: PromptTemplate[];
  nameFilter: string;
  onNameFilterChange: (value: string) => void;
  tagFilter: string;
  onTagFilterChange: (value: string) => void;
  order: TemplateOrder;
  onOrderChange: (value: TemplateOrder) => void;
  onSelect: (template: PromptTemplate) => void;
  loadMore: () => void;
  loadingMore: boolean;
  loading: boolean;
}

export function TemplateListSection({
  templates,
  nameFilter,
  onNameFilterChange,
  tagFilter,
  onTagFilterChange,
  order,
  onOrderChange,
  onSelect,
  loadMore,
  loadingMore,
  loading,
}: TemplateListSectionProps) {
  const sentinelRef = useRef<HTMLDivElement>(null);
  const loadMoreRef = useRef(loadMore);
  const nameDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const tagDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    loadMoreRef.current = loadMore;
  }, [loadMore]);

  useEffect(() => {
    const sentinel = sentinelRef.current;
    if (!sentinel) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting) {
          loadMoreRef.current();
        }
      },
      { threshold: 0.1 },
    );

    observer.observe(sentinel);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    return () => {
      if (nameDebounceRef.current) clearTimeout(nameDebounceRef.current);
      if (tagDebounceRef.current) clearTimeout(tagDebounceRef.current);
    };
  }, []);

  const handleNameInput = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const value = e.target.value;
      if (nameDebounceRef.current) clearTimeout(nameDebounceRef.current);
      nameDebounceRef.current = setTimeout(() => {
        onNameFilterChange(value);
      }, 300);
    },
    [onNameFilterChange],
  );

  const handleTagInput = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const value = e.target.value;
      if (tagDebounceRef.current) clearTimeout(tagDebounceRef.current);
      tagDebounceRef.current = setTimeout(() => {
        onTagFilterChange(value);
      }, 300);
    },
    [onTagFilterChange],
  );

  const orderLabel = order === 'alphabetical' ? 'Alphabetical' : 'Recently used';

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-2 sm:flex-row">
        <div className="relative flex-1">
          <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Filter by name..."
            className="pl-8"
            defaultValue={nameFilter}
            onChange={handleNameInput}
            aria-label="Filter templates by name"
          />
        </div>
        <div className="relative flex-1">
          <Tag className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Filter by tag..."
            className="pl-8"
            defaultValue={tagFilter}
            onChange={handleTagInput}
            aria-label="Filter templates by tag"
          />
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm" className="shrink-0 gap-1.5">
              {order === 'alphabetical' ? <ArrowDownAZ className="size-3.5" /> : <Clock3 className="size-3.5" />}
              <span>Sort: {orderLabel}</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onOrderChange('recentlyUsed')}>
              Recently used
              {order === 'recentlyUsed' && <span className="ml-auto text-primary">✓</span>}
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onOrderChange('alphabetical')}>
              Alphabetical
              {order === 'alphabetical' && <span className="ml-auto text-primary">✓</span>}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {loading ? (
        <div className="flex justify-center py-8">
          <Loader2 className="size-5 animate-spin text-muted-foreground" />
        </div>
      ) : templates.length === 0 ? (
        <div className="rounded-md border border-dashed p-8 text-center">
          <p className="text-sm text-muted-foreground">No templates found.</p>
        </div>
      ) : (
        <table className="w-full">
          <tbody>
            {templates.map((template) => (
              <TemplateListItem key={template.id} template={template} onSelect={onSelect} />
            ))}
          </tbody>
        </table>
      )}

      <div ref={sentinelRef} className="h-1" aria-hidden="true" />

      {loadingMore && (
        <div className="flex justify-center py-4">
          <Loader2 className="size-5 animate-spin text-muted-foreground" />
        </div>
      )}
    </div>
  );
}
