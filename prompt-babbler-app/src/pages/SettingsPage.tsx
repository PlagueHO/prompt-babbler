import { useEffect, useState } from 'react';
import { LanguageSelector } from '@/components/settings/LanguageSelector';
import { ThemeSelector } from '@/components/settings/ThemeSelector';
import { Separator } from '@/components/ui/separator';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { useSettings } from '@/hooks/useSettings';
import { useUserSettings } from '@/hooks/useUserSettings';
import { useTheme } from '@/hooks/useTheme';
import { useAuthToken } from '@/hooks/useAuthToken';
import {
  startExport,
  getExportJob,
  downloadExport,
  startImport,
  getImportJob,
} from '@/services/api-client';
import { CheckCircle, XCircle, RefreshCw, Loader2, Upload, Download } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { usePageTitle } from '@/hooks/usePageTitle';
import type { ImportExportJob, ThemeMode } from '@/types';

export function SettingsPage() {
  usePageTitle('Settings');

  const { isConnected, isLoading: statusLoading, error: statusError, refresh } = useSettings();
  const { settings, loading: settingsLoading, error: settingsError, updateSettings } = useUserSettings();
  const { setTheme } = useTheme();
  const getAuthToken = useAuthToken();

  const [includeBabbles, setIncludeBabbles] = useState(true);
  const [includeGeneratedPrompts, setIncludeGeneratedPrompts] = useState(true);
  const [includeUserTemplates, setIncludeUserTemplates] = useState(true);
  const [includeSemanticVectors, setIncludeSemanticVectors] = useState(false);
  const [overwriteExisting, setOverwriteExisting] = useState(false);
  const [importFile, setImportFile] = useState<File | null>(null);

  const [exportJob, setExportJob] = useState<ImportExportJob | null>(null);
  const [importJob, setImportJob] = useState<ImportExportJob | null>(null);
  const [dataMessage, setDataMessage] = useState<string | null>(null);
  const [isStartingExport, setIsStartingExport] = useState(false);
  const [isStartingImport, setIsStartingImport] = useState(false);

  const isExportRunning = exportJob?.status === 'Queued' || exportJob?.status === 'Running';
  const isImportRunning = importJob?.status === 'Queued' || importJob?.status === 'Running';

  const handleThemeChange = (value: ThemeMode) => {
    setTheme(value);
    void updateSettings({ theme: value });
  };

  const handleLanguageChange = (value: string) => {
    const lang = value === 'auto' ? '' : value;
    void updateSettings({ speechLanguage: lang });
  };

  const handleStartExport = async () => {
    if (!includeBabbles && !includeGeneratedPrompts && !includeUserTemplates) {
      setDataMessage('Select at least one data type for export.');
      return;
    }

    setDataMessage(null);
    setIsStartingExport(true);

    try {
      const token = await getAuthToken();
      const jobId = await startExport(
        {
          includeBabbles,
          includeGeneratedPrompts,
          includeUserTemplates,
          includeSemanticVectors,
        },
        token,
      );
      const job = await getExportJob(jobId, token);
      setExportJob(job);
      setDataMessage('Export started.');
    } catch (err) {
      setDataMessage(err instanceof Error ? err.message : 'Failed to start export.');
    } finally {
      setIsStartingExport(false);
    }
  };

  const handleDownloadExport = async () => {
    if (!exportJob) {
      return;
    }

    setDataMessage(null);
    try {
      const token = await getAuthToken();
      const blob = await downloadExport(exportJob.id, token);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `prompt-babbler-export-${exportJob.id}.zip`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      setDataMessage(err instanceof Error ? err.message : 'Failed to download export.');
    }
  };

  const handleStartImport = async () => {
    if (!importFile) {
      setDataMessage('Choose a .zip file to import.');
      return;
    }

    setDataMessage(null);
    setIsStartingImport(true);

    try {
      const token = await getAuthToken();
      const jobId = await startImport(importFile, overwriteExisting, token);
      const job = await getImportJob(jobId, token);
      setImportJob(job);
      setDataMessage('Import started.');
    } catch (err) {
      setDataMessage(err instanceof Error ? err.message : 'Failed to start import.');
    } finally {
      setIsStartingImport(false);
    }
  };

  useEffect(() => {
    if (!isExportRunning && !isImportRunning) {
      return;
    }

    let isDisposed = false;

    const poll = async () => {
      try {
        const token = await getAuthToken();

        if (exportJob && (exportJob.status === 'Queued' || exportJob.status === 'Running')) {
          const latestExportJob = await getExportJob(exportJob.id, token);
          if (!isDisposed) {
            setExportJob(latestExportJob);
          }
        }

        if (importJob && (importJob.status === 'Queued' || importJob.status === 'Running')) {
          const latestImportJob = await getImportJob(importJob.id, token);
          if (!isDisposed) {
            setImportJob(latestImportJob);
          }
        }
      } catch {
        // Polling should not interrupt the rest of the settings page.
      }
    };

    void poll();
    const intervalId = window.setInterval(() => {
      void poll();
    }, 2000);

    return () => {
      isDisposed = true;
      window.clearInterval(intervalId);
    };
  }, [getAuthToken, exportJob, importJob, isExportRunning, isImportRunning]);

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
        <h2 className="text-lg font-semibold">Data</h2>
        <p className="text-sm text-muted-foreground">
          Export all babbles and settings to a ZIP backup, or restore from an existing backup.
        </p>

        <div className="grid gap-4 md:grid-cols-2">
          <Card className="gap-4 py-0">
            <CardHeader className="px-4 pt-4 pb-0">
              <CardTitle className="text-lg">Export</CardTitle>
              <CardDescription>
                Download a backup ZIP of your babbles, templates, and generated prompts.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4 px-4 pb-4">
              <div className="space-y-2 text-sm">
                <div className="flex items-center gap-2">
                  <Checkbox checked={includeBabbles} onCheckedChange={(checked) => setIncludeBabbles(checked === true)} id="export-babbles" />
                  <Label htmlFor="export-babbles">Babbles</Label>
                </div>
                <div className="flex items-center gap-2">
                  <Checkbox checked={includeGeneratedPrompts} onCheckedChange={(checked) => setIncludeGeneratedPrompts(checked === true)} id="export-generated-prompts" />
                  <Label htmlFor="export-generated-prompts">Generated prompts</Label>
                </div>
                <div className="flex items-center gap-2">
                  <Checkbox checked={includeUserTemplates} onCheckedChange={(checked) => setIncludeUserTemplates(checked === true)} id="export-user-templates" />
                  <Label htmlFor="export-user-templates">User templates</Label>
                </div>
                <div className="flex items-center gap-2">
                  <Checkbox checked={includeSemanticVectors} onCheckedChange={(checked) => setIncludeSemanticVectors(checked === true)} id="export-semantic-vectors" />
                  <Label htmlFor="export-semantic-vectors">Include semantic vectors</Label>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-2">
                <Button onClick={() => void handleStartExport()} disabled={isStartingExport || isExportRunning}>
                  {isStartingExport ? <Loader2 className="size-4 animate-spin" /> : <Download className="size-4" />}
                  <span>Start Export</span>
                </Button>
                <Button
                  variant="outline"
                  onClick={() => void handleDownloadExport()}
                  disabled={exportJob?.status !== 'Completed'}
                >
                  Download ZIP
                </Button>
              </div>

              {exportJob && (
                <p className="text-xs text-muted-foreground">
                  Export job: {exportJob.status} ({exportJob.progressPercentage}%)
                  {exportJob.currentStage ? ` - ${exportJob.currentStage}` : ''}
                </p>
              )}
            </CardContent>
          </Card>

          <Card className="gap-4 py-0">
            <CardHeader className="px-4 pt-4 pb-0">
              <CardTitle className="text-lg">Import</CardTitle>
              <CardDescription>
                Restore manuscripts from a previously exported ZIP backup.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4 px-4 pb-4">
              <div className="space-y-2">
                <Label htmlFor="import-backup-zip">Backup ZIP</Label>
                <Input
                  id="import-backup-zip"
                  type="file"
                  accept=".zip"
                  onChange={(event) => {
                    setImportFile(event.target.files?.[0] ?? null);
                  }}
                />
              </div>

              <div className="flex items-center gap-2 text-sm">
                <Checkbox checked={overwriteExisting} onCheckedChange={(checked) => setOverwriteExisting(checked === true)} id="import-overwrite" />
                <Label htmlFor="import-overwrite">Overwrite existing records</Label>
              </div>

              <Button onClick={() => void handleStartImport()} disabled={isStartingImport || isImportRunning || !importFile}>
                {isStartingImport ? <Loader2 className="size-4 animate-spin" /> : <Upload className="size-4" />}
                <span>Start Import</span>
              </Button>

              {importJob && (
                <p className="text-xs text-muted-foreground">
                  Import job: {importJob.status} ({importJob.progressPercentage}%)
                  {importJob.currentStage ? ` - ${importJob.currentStage}` : ''}
                </p>
              )}
            </CardContent>
          </Card>
        </div>

        {dataMessage && (
          <div className="rounded-md bg-blue-50 px-3 py-2 text-sm text-blue-800 dark:bg-blue-900/20 dark:text-blue-200">
            {dataMessage}
          </div>
        )}
      </div>

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
