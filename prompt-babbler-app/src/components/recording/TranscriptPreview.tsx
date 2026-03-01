import { useEffect, useRef } from 'react';
import { ScrollArea } from '@/components/ui/scroll-area';

interface TranscriptPreviewProps {
  text: string;
  isTranscribing: boolean;
}

export function TranscriptPreview({
  text,
  isTranscribing,
}: TranscriptPreviewProps) {
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [text]);

  return (
    <ScrollArea className="h-64 w-full rounded-md border p-4">
      {text ? (
        <div className="space-y-1">
          <p className="whitespace-pre-wrap text-sm leading-relaxed">{text}</p>
          {isTranscribing && (
            <span className="inline-block size-2 animate-pulse rounded-full bg-primary" />
          )}
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">
          {isTranscribing
            ? 'Transcribing...'
            : 'Start recording to see your transcript here.'}
        </p>
      )}
      <div ref={bottomRef} />
    </ScrollArea>
  );
}
