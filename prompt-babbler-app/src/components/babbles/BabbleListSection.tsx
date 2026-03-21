import { useEffect, useRef, useCallback } from 'react';
import { Search, ArrowUpDown, ArrowUp, ArrowDown, Loader2 } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { BabbleListItem } from '@/components/babbles/BabbleListItem';
import type { Babble } from '@/types';

interface BabbleListSectionProps {
  babbles: Babble[];
  search: string;
  onSearchChange: (value: string) => void;
  sortBy: 'createdAt' | 'title';
  onSortByChange: (value: 'createdAt' | 'title') => void;
  sortDirection: 'desc' | 'asc';
  onSortDirectionChange: (value: 'desc' | 'asc') => void;
  loadMore: () => void;
  loadingMore: boolean;
  loading: boolean;
  onTogglePin: (babbleId: string) => void;
}

export function BabbleListSection({
  babbles,
  search,
  onSearchChange,
  sortBy,
  onSortByChange,
  sortDirection,
  onSortDirectionChange,
  loadMore,
  loadingMore,
  loading,
  onTogglePin,
}: BabbleListSectionProps) {
  const sentinelRef = useRef<HTMLDivElement>(null);
  const loadMoreRef = useRef(loadMore);

  useEffect(() => {
    loadMoreRef.current = loadMore;
  }, [loadMore]);

  // IntersectionObserver for infinite scroll
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

  // Debounce search: hold an internal pending value and emit after 300ms
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const handleSearchInput = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const value = e.target.value;
      if (debounceRef.current) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => {
        onSearchChange(value);
      }, 300);
    },
    [onSearchChange],
  );

  const SortIcon = sortDirection === 'desc' ? ArrowDown : ArrowUp;
  const sortLabel = sortBy === 'createdAt' ? 'Date Created' : 'Title';

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <Separator className="flex-1" />
        <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
          Older Babbles
        </span>
        <Separator className="flex-1" />
      </div>

      {/* Controls */}
      <div className="flex items-center gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Filter babbles…"
            className="pl-8"
            defaultValue={search}
            onChange={handleSearchInput}
            aria-label="Filter babbles"
          />
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm" className="shrink-0 gap-1.5">
              <ArrowUpDown className="size-3.5" />
              <span>Sort: {sortLabel}</span>
              <SortIcon className="size-3.5" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem
              onClick={() => onSortByChange('createdAt')}
              aria-selected={sortBy === 'createdAt'}
              className={sortBy === 'createdAt' ? 'font-medium' : ''}
            >
              Date Created
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={() => onSortByChange('title')}
              aria-selected={sortBy === 'title'}
              className={sortBy === 'title' ? 'font-medium' : ''}
            >
              Title
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={() => onSortDirectionChange('desc')}>
              <ArrowDown className="size-3.5 mr-1.5" />
              Descending
              {sortDirection === 'desc' && <span className="ml-auto text-primary">✓</span>}
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onSortDirectionChange('asc')}>
              <ArrowUp className="size-3.5 mr-1.5" />
              Ascending
              {sortDirection === 'asc' && <span className="ml-auto text-primary">✓</span>}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* List */}
      {loading ? (
        <div className="flex justify-center py-8">
          <Loader2 className="size-5 animate-spin text-muted-foreground" />
        </div>
      ) : babbles.length === 0 ? (
        <div className="rounded-md border border-dashed p-8 text-center">
          <p className="text-sm text-muted-foreground">No babbles found.</p>
        </div>
      ) : (
        <div className="space-y-1.5">
          {babbles.map((babble) => (
            <BabbleListItem key={babble.id} babble={babble} onTogglePin={onTogglePin} />
          ))}
        </div>
      )}

      {/* Infinite scroll sentinel */}
      <div ref={sentinelRef} className="h-1" aria-hidden="true" />

      {/* Loading more indicator */}
      {loadingMore && (
        <div className="flex justify-center py-4">
          <Loader2 className="size-5 animate-spin text-muted-foreground" />
        </div>
      )}
    </div>
  );
}
