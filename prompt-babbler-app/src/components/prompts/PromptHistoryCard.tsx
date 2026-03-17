import { useState } from 'react';
import { Copy, Check, Trash2, RefreshCw, ChevronDown, ChevronRight } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import type { GeneratedPrompt } from '@/types';

interface PromptHistoryCardProps {
  prompt: GeneratedPrompt;
  onDelete: (id: string) => void;
  onRegenerate: (prompt: GeneratedPrompt) => void;
  isDeleting?: boolean;
}

export function PromptHistoryCard({
  prompt,
  onDelete,
  onRegenerate,
  isDeleting = false,
}: PromptHistoryCardProps) {
  const [open, setOpen] = useState(false);
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(prompt.promptText);
      setCopied(true);
      toast.success('Copied to clipboard');
      setTimeout(() => setCopied(false), 2000);
    } catch {
      toast.error('Failed to copy to clipboard');
    }
  };

  const dateStr = new Date(prompt.generatedAt).toLocaleString();
  const truncated =
    prompt.promptText.length > 120
      ? `${prompt.promptText.slice(0, 120)}…`
      : prompt.promptText;

  return (
    <div className="rounded-lg border">
      <button
        type="button"
        className="flex w-full items-start gap-3 p-3 text-left hover:bg-accent/50 transition-colors"
        onClick={() => setOpen((prev) => !prev)}
      >
        {open ? (
          <ChevronDown className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
        ) : (
          <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
        )}
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium">{prompt.templateName}</span>
            <span className="text-xs text-muted-foreground">{dateStr}</span>
          </div>
          {!open && (
            <p className="mt-1 truncate text-xs text-muted-foreground">
              {truncated}
            </p>
          )}
        </div>
      </button>
      {open && (
        <div className="border-t px-3 pb-3 pt-2">
          <p className="whitespace-pre-wrap text-sm leading-relaxed">
            {prompt.promptText}
          </p>
          <div className="mt-3 flex gap-2">
            <Button
              size="sm"
              variant="outline"
              onClick={() => void handleCopy()}
            >
              {copied ? (
                <Check className="size-3" />
              ) : (
                <Copy className="size-3" />
              )}
              {copied ? 'Copied' : 'Copy'}
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => onRegenerate(prompt)}
            >
              <RefreshCw className="size-3" />
              Regenerate
            </Button>
            <Button
              size="sm"
              variant="destructive"
              disabled={isDeleting}
              onClick={() => onDelete(prompt.id)}
            >
              <Trash2 className="size-3" />
              Delete
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
