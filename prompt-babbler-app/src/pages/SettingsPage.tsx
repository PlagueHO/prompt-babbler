import { useState } from 'react';
import { LanguageSelector } from '@/components/settings/LanguageSelector';
import { Separator } from '@/components/ui/separator';
import { useSettings } from '@/hooks/useSettings';
import { CheckCircle, XCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  getSpeechLanguage,
  setSpeechLanguage,
} from '@/services/local-storage';

export function SettingsPage() {
  const { isConnected, isLoading, error, refresh } = useSettings();
  const [language, setLanguage] = useState(() => getSpeechLanguage());

  const handleLanguageChange = (value: string) => {
    const lang = value === 'auto' ? '' : value;
    setLanguage(lang);
    setSpeechLanguage(lang);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Settings</h1>
        <p className="text-sm text-muted-foreground">
          Speech settings and backend status.
        </p>
      </div>

      <div className="space-y-4">
        <h2 className="text-lg font-semibold">Backend Status</h2>
        <div className="flex items-center gap-3">
          {isLoading ? (
            <span className="text-sm text-muted-foreground">Checking…</span>
          ) : isConnected ? (
            <div className="flex items-center gap-2 rounded-md bg-green-50 px-3 py-2 text-sm text-green-800 dark:bg-green-900/20 dark:text-green-200">
              <CheckCircle className="size-4" />
              <span>Backend connected</span>
            </div>
          ) : (
            <div className="flex items-center gap-2 rounded-md bg-red-50 px-3 py-2 text-sm text-red-800 dark:bg-red-900/20 dark:text-red-200">
              <XCircle className="size-4" />
              <span>{error ?? 'Unable to reach backend'}</span>
            </div>
          )}
          <Button variant="ghost" size="sm" onClick={() => void refresh()}>
            <RefreshCw className="size-4" />
          </Button>
        </div>
      </div>

      <Separator />

      <LanguageSelector
        value={language || 'auto'}
        onChange={handleLanguageChange}
      />
    </div>
  );
}
