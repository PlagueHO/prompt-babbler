import { useEffect, useRef } from 'react';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Skeleton } from '@/components/ui/skeleton';

interface PromptDisplayProps {
  text: string;
  isGenerating: boolean;
}

export function PromptDisplay({ text, isGenerating }: PromptDisplayProps) {
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [text]);

  if (!text && !isGenerating) return null;

  return (
    <ScrollArea className="h-64 w-full rounded-md border bg-muted/30 p-4">
      {text ? (
        <div className="space-y-1">
          <p className="whitespace-pre-wrap text-sm leading-relaxed">{text}</p>
          {isGenerating && (
            <span className="inline-block size-2 animate-pulse rounded-full bg-primary" />
          )}
        </div>
      ) : (
        <div className="space-y-2">
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
        </div>
      )}
      <div ref={bottomRef} />
    </ScrollArea>
  );
}
