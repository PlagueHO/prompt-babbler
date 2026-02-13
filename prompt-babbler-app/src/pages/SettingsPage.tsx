import { useState } from 'react';
import { toast } from 'sonner';
import { SettingsForm } from '@/components/settings/SettingsForm';
import { ConnectionTest } from '@/components/settings/ConnectionTest';
import { LanguageSelector } from '@/components/settings/LanguageSelector';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { useSettings } from '@/hooks/useSettings';
import {
  getSpeechLanguage,
  setSpeechLanguage,
} from '@/services/local-storage';
import type { LlmSettingsSaveRequest } from '@/types';

export function SettingsPage() {
  const { settings, isLoading, error, updateSettings, testConnection } =
    useSettings();
  const [isSaving, setIsSaving] = useState(false);
  const [language, setLanguage] = useState(() => getSpeechLanguage());

  const handleSave = async (data: LlmSettingsSaveRequest) => {
    setIsSaving(true);
    try {
      await updateSettings(data);
      toast.success('Settings saved');
    } catch {
      toast.error('Failed to save settings');
    } finally {
      setIsSaving(false);
    }
  };

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
          Configure your Azure OpenAI connection and speech settings.
        </p>
      </div>

      {error && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {isLoading ? (
        <div className="space-y-4">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      ) : (
        <SettingsForm
          settings={settings}
          onSave={(data) => void handleSave(data)}
          isSaving={isSaving}
        />
      )}

      <Separator />

      <div className="space-y-4">
        <h2 className="text-lg font-semibold">Connection Test</h2>
        <ConnectionTest onTest={testConnection} />
      </div>

      <Separator />

      <LanguageSelector
        value={language || 'auto'}
        onChange={handleLanguageChange}
      />
    </div>
  );
}
