import { Square, Mic } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface RecordingIndicatorProps {
  isRecording: boolean;
  duration: number;
  onStart: () => void;
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
    <div className="flex items-center gap-4">
      {isRecording ? (
        <>
          <div className="flex items-center gap-2">
            <span className="size-3 animate-pulse rounded-full bg-destructive" />
            <span className="text-sm font-medium text-destructive">
              Recording
            </span>
          </div>
          <span className="font-mono text-lg tabular-nums">
            {formatDuration(duration)}
          </span>
          <Button size="sm" variant="destructive" onClick={onStop}>
            <Square className="size-4" />
            Stop
          </Button>
        </>
      ) : (
        <Button size="sm" onClick={onStart} disabled={disabled}>
          <Mic className="size-4" />
          Start Recording
        </Button>
      )}
    </div>
  );
}
