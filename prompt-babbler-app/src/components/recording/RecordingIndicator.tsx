interface RecordingIndicatorProps {
  isRecording: boolean;
  duration: number;
  hasTranscription?: boolean;
}

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
}

export function RecordingIndicator({
  isRecording,
  duration,
  hasTranscription,
}: RecordingIndicatorProps) {
  if (!isRecording) {
    return (
      <span className="text-sm text-muted-foreground">
        {hasTranscription ? 'Press to continue recording' : 'Press to start recording'}
      </span>
    );
  }

  return (
    <div className="flex items-center gap-2">
      <span className="size-2 animate-pulse rounded-full bg-destructive" />
      <span className="text-xs font-medium text-destructive">Recording</span>
      <span className="font-mono text-sm tabular-nums">
        {formatDuration(duration)}
      </span>
    </div>
  );
}
