import { useEffect, useRef } from 'react';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Loader2 } from 'lucide-react';

interface PromptDisplayProps {
  text: string;
  isGenerating: boolean;
}

function TypingIndicator() {
  return (
    <div className="flex items-center gap-1.5 py-1">
      <span className="size-1.5 animate-bounce rounded-full bg-primary [animation-delay:0ms]" />
      <span className="size-1.5 animate-bounce rounded-full bg-primary [animation-delay:150ms]" />
      <span className="size-1.5 animate-bounce rounded-full bg-primary [animation-delay:300ms]" />
    </div>
  );
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
          {isGenerating && <TypingIndicator />}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center gap-3 py-8">
          <Loader2 className="size-6 animate-spin text-primary" />
          <p className="text-sm font-medium text-muted-foreground">Generating prompt&hellip;</p>
        </div>
      )}
      <div ref={bottomRef} />
    </ScrollArea>
  );
}
