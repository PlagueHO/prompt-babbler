import { Link } from 'react-router';
import { Settings } from 'lucide-react';

interface SettingsRequiredBannerProps {
  isConfigured: boolean;
}

export function SettingsRequiredBanner({
  isConfigured,
}: SettingsRequiredBannerProps) {
  if (isConfigured) return null;

  return (
    <div className="rounded-lg border border-yellow-500/30 bg-yellow-50 px-4 py-3 dark:bg-yellow-900/20">
      <div className="flex items-center gap-2 text-sm text-yellow-800 dark:text-yellow-200">
        <Settings className="size-4 shrink-0" />
        <span>
          LLM settings are not configured.{' '}
          <Link
            to="/settings"
            className="font-medium underline underline-offset-4 hover:text-yellow-900 dark:hover:text-yellow-100"
          >
            Configure settings
          </Link>{' '}
          to enable transcription and prompt generation.
        </span>
      </div>
    </div>
  );
}
