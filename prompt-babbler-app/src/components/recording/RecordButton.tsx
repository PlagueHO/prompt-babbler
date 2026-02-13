import { useState } from 'react';
import { Mic } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

interface RecordButtonProps {
  isRecording: boolean;
  onStart: () => void;
  onStop: () => void;
  disabled?: boolean;
}

export function RecordButton({
  isRecording,
  onStart,
  onStop,
  disabled = false,
}: RecordButtonProps) {
  const [permissionDenied, setPermissionDenied] = useState(false);

  const handleClick = async () => {
    if (isRecording) {
      onStop();
      return;
    }
    try {
      setPermissionDenied(false);
      onStart();
    } catch {
      setPermissionDenied(true);
    }
  };

  return (
    <div className="flex flex-col items-center gap-2">
      <Button
        size="icon-lg"
        variant={isRecording ? 'destructive' : 'default'}
        disabled={disabled}
        onClick={() => void handleClick()}
        className={cn(
          'size-16 rounded-full transition-all',
          isRecording && 'animate-pulse'
        )}
      >
        <Mic className="size-6" />
      </Button>
      {permissionDenied && (
        <p className="text-sm text-destructive">
          Microphone permission denied. Please allow access in your browser
          settings.
        </p>
      )}
    </div>
  );
}
