import { Loader2, History } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { PromptHistoryCard } from './PromptHistoryCard';
import type { GeneratedPrompt } from '@/types';

interface PromptHistoryListProps {
  prompts: GeneratedPrompt[];
  loading: boolean;
  error: string | null;
  hasMore: boolean;
  onLoadMore: () => void;
  onDelete: (id: string) => void;
  onRegenerate: (prompt: GeneratedPrompt) => void;
}

export function PromptHistoryList({
  prompts,
  loading,
  error,
  hasMore,
  onLoadMore,
  onDelete,
  onRegenerate,
}: PromptHistoryListProps) {
  if (!loading && prompts.length === 0) return null;

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <History className="size-4 text-muted-foreground" />
        <h3 className="text-sm font-semibold">Prompt History</h3>
        <span className="text-xs text-muted-foreground">
          ({prompts.length}{hasMore ? '+' : ''})
        </span>
      </div>

      {error && (
        <p className="text-sm text-destructive">{error}</p>
      )}

      <div className="space-y-2">
        {prompts.map((prompt) => (
          <PromptHistoryCard
            key={prompt.id}
            prompt={prompt}
            onDelete={onDelete}
            onRegenerate={onRegenerate}
          />
        ))}
      </div>

      {loading && (
        <div className="flex justify-center py-2">
          <Loader2 className="size-5 animate-spin text-muted-foreground" />
        </div>
      )}

      {hasMore && !loading && (
        <Button variant="outline" size="sm" className="w-full" onClick={onLoadMore}>
          Load More
        </Button>
      )}
    </div>
  );
}
