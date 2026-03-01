import { AlertTriangle } from 'lucide-react';
import { isStorageWarning, getStorageUsage } from '@/services/local-storage';

export function StorageWarning() {
  if (!isStorageWarning()) return null;

  const { percentage } = getStorageUsage();

  return (
    <div className="rounded-lg border border-orange-500/30 bg-orange-50 px-4 py-3 dark:bg-orange-900/20">
      <div className="flex items-center gap-2 text-sm text-orange-800 dark:text-orange-200">
        <AlertTriangle className="size-4 shrink-0" />
        <span>
          Local storage is {percentage}% full. Consider deleting old babbles to
          free up space.
        </span>
      </div>
    </div>
  );
}
