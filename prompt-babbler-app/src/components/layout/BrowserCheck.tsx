import { AlertTriangle } from 'lucide-react';

export function BrowserCheck() {
  const isSupported =
    typeof navigator !== 'undefined' &&
    typeof navigator.mediaDevices !== 'undefined' &&
    typeof MediaRecorder !== 'undefined';

  if (isSupported) return null;

  return (
    <div className="border-b border-destructive/30 bg-destructive/10 px-4 py-3">
      <div className="mx-auto flex max-w-5xl items-center gap-2 text-sm text-destructive">
        <AlertTriangle className="size-4 shrink-0" />
        <span>
          Your browser does not support audio recording. Please use a modern
          browser like Chrome, Firefox, or Edge.
        </span>
      </div>
    </div>
  );
}
