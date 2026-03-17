import { useEffect, useRef, useState } from 'react';
import { ScrollArea } from '@/components/ui/scroll-area';

interface TranscriptPreviewProps {
  finalText: string;
  partialText: string;
  isTranscribing: boolean;
}

export function TranscriptPreview({
  finalText,
  partialText,
  isTranscribing,
}: TranscriptPreviewProps) {
  const bottomRef = useRef<HTMLDivElement>(null);
  const [prevFinalLength, setPrevFinalLength] = useState(0);
  const [fadeKey, setFadeKey] = useState(0);

  // Track when new final words arrive so we can fade them in
  useEffect(() => {
    if (finalText.length > prevFinalLength) {
      setFadeKey((k) => k + 1);
    }
    setPrevFinalLength(finalText.length);
  }, [finalText, prevFinalLength]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [finalText, partialText]);

  const hasContent = finalText || partialText;

  // Split final text into the "old" stable portion and the newly-added tail
  const oldText = finalText.slice(0, prevFinalLength < finalText.length ? prevFinalLength : finalText.length);
  const newText = finalText.slice(oldText.length);

  return (
    <ScrollArea className="h-64 w-full rounded-md border p-4">
      {hasContent ? (
        <p className="whitespace-pre-wrap text-sm leading-relaxed">
          {oldText}
          {newText && (
            <span key={fadeKey} className="animate-[fade-in-word_300ms_ease-out]">
              {newText}
            </span>
          )}
          {partialText && (
            <>
              {finalText ? ' ' : ''}
              <span className="text-muted-foreground animate-[fade-in-word_300ms_ease-out]">
                {partialText}
              </span>
            </>
          )}
          {isTranscribing && (
            <span
              className="ml-0.5 inline-block h-4 w-0.5 translate-y-0.5 animate-[blink-cursor_1s_steps(2)_infinite] bg-primary"
              aria-hidden="true"
            />
          )}
        </p>
      ) : (
        <p className="text-sm text-muted-foreground">
          {isTranscribing
            ? 'Listening...'
            : 'Start recording to see your transcript here.'}
        </p>
      )}
      <div ref={bottomRef} />
    </ScrollArea>
  );
}
