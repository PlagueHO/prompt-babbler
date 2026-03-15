import { Square, Mic } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface RecordingIndicatorProps {
  isRecording: boolean;
  duration: number;
  onStart: () => Promise<void> | void;
  onStop: () => void;
  disabled?: boolean;
}

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
}

export function RecordingIndicator({
  isRecording,
  duration,
  onStart,
  onStop,
  disabled = false,
}: RecordingIndicatorProps) {
  return (
    <div className="flex items-center gap-3">
      {isRecording ? (
        <>
          <div className="flex items-center gap-1.5">
            <span className="size-2 animate-pulse rounded-full bg-destructive" />
            <span className="text-xs font-medium text-destructive">
              Recording
            </span>
          </div>
          <span className="font-mono text-sm tabular-nums">
            {formatDuration(duration)}
          </span>
          <Button size="xs" variant="destructive" onClick={onStop}>
            <Square className="size-3" />
            Stop
          </Button>
        </>
      ) : (
        <Button size="xs" onClick={onStart} disabled={disabled}>
          <Mic className="size-3" />
          Start Recording
        </Button>
      )}
    </div>
  );
}
