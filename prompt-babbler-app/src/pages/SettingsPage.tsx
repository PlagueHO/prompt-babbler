import { LanguageSelector } from '@/components/settings/LanguageSelector';
import { ThemeSelector } from '@/components/settings/ThemeSelector';
import { Separator } from '@/components/ui/separator';
import { useSettings } from '@/hooks/useSettings';
import { useUserSettings } from '@/hooks/useUserSettings';
import { useTheme } from '@/hooks/useTheme';
import { CheckCircle, XCircle, RefreshCw, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { usePageTitle } from '@/hooks/usePageTitle';
import type { ThemeMode } from '@/types';

export function SettingsPage() {
  usePageTitle('Settings');

  const { isConnected, isLoading: statusLoading, error: statusError, refresh } = useSettings();
  const { settings, loading: settingsLoading, error: settingsError, updateSettings } = useUserSettings();
  const { setTheme } = useTheme();

  const handleThemeChange = (value: ThemeMode) => {
    setTheme(value);
    void updateSettings({ theme: value });
  };

  const handleLanguageChange = (value: string) => {
    const lang = value === 'auto' ? '' : value;
    void updateSettings({ speechLanguage: lang });
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Settings</h1>
        <p className="text-sm text-muted-foreground">
          Appearance, speech settings, and backend status.
        </p>
      </div>

      {settingsLoading ? (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="size-4 animate-spin" />
          <span>Loading settings…</span>
        </div>
      ) : (
        <>
          {settingsError && (
            <div className="rounded-md bg-yellow-50 px-3 py-2 text-sm text-yellow-800 dark:bg-yellow-900/20 dark:text-yellow-200">
              Settings stored locally (backend unavailable: {settingsError})
            </div>
          )}

          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Appearance</h2>
            <ThemeSelector value={settings.theme} onChange={handleThemeChange} />
          </div>

          <Separator />

          <LanguageSelector
            value={settings.speechLanguage || 'auto'}
            onChange={handleLanguageChange}
          />
        </>
      )}

      <Separator />

      <div className="space-y-4">
        <h2 className="text-lg font-semibold">Backend Status</h2>
        <div className="flex items-center gap-3">
          {statusLoading ? (
            <span className="text-sm text-muted-foreground">Checking…</span>
          ) : isConnected ? (
            <div className="flex items-center gap-2 rounded-md bg-green-50 px-3 py-2 text-sm text-green-800 dark:bg-green-900/20 dark:text-green-200">
              <CheckCircle className="size-4" />
              <span>Backend connected</span>
            </div>
          ) : (
            <div className="flex items-center gap-2 rounded-md bg-red-50 px-3 py-2 text-sm text-red-800 dark:bg-red-900/20 dark:text-red-200">
              <XCircle className="size-4" />
              <span>{statusError ?? 'Unable to reach backend'}</span>
            </div>
          )}
          <Button variant="ghost" size="sm" onClick={() => void refresh()}>
            <RefreshCw className="size-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
