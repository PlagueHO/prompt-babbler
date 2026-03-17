import { useState, useCallback, useEffect } from 'react';
import { useNavigate } from 'react-router';
import { toast } from 'sonner';
import { RecordButton } from '@/components/recording/RecordButton';
import { RecordingIndicator } from '@/components/recording/RecordingIndicator';
import { TranscriptPreview } from '@/components/recording/TranscriptPreview';
import { WaveformVisualizer } from '@/components/recording/WaveformVisualizer';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import { useAudioRecording } from '@/hooks/useAudioRecording';
import { useTranscription } from '@/hooks/useTranscription';
import { useBabbles } from '@/hooks/useBabbles';
import { useTemplates } from '@/hooks/useTemplates';
import { getSpeechLanguage } from '@/services/local-storage';
import { AuthGuard } from '@/components/layout/AuthGuard';
import { Save, ChevronDown, Sparkles, Loader2, Trash2 } from 'lucide-react';

export function RecordPage() {
  const navigate = useNavigate();
  const { createBabble } = useBabbles();
  const { templates } = useTemplates();
  const {
    transcribedText,
    partialText,
    isConnected,
    error: transcriptionError,
    connect,
    sendAudio,
    disconnect,
    reset,
  } = useTranscription();
  const [title, setTitle] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  const language = getSpeechLanguage();

  const onPcmFrame = useCallback(
    (buffer: ArrayBuffer) => {
      sendAudio(buffer);
    },
    [sendAudio],
  );

  const { isRecording, duration, start: startRecording, stop: stopRecording, analyserRef } = useAudioRecording({
    onPcmFrame,
  });

  const handleStart = useCallback(async () => {
    await connect(language || undefined);
    await startRecording();
  }, [connect, language, startRecording]);

  const handleStop = useCallback(() => {
    stopRecording();
    disconnect();
  }, [stopRecording, disconnect]);

  // Warn before leaving while recording
  useEffect(() => {
    const handler = (e: BeforeUnloadEvent) => {
      if (isRecording || transcribedText) {
        e.preventDefault();
        e.returnValue = '';
      }
    };
    window.addEventListener('beforeunload', handler);
    return () => window.removeEventListener('beforeunload', handler);
  }, [isRecording, transcribedText]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      reset();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const saveBabble = useCallback(async () => {
    const babble = await createBabble({
      title: title.trim() || `Babble ${new Date().toLocaleDateString()}`,
      text: transcribedText,
    });
    return babble;
  }, [title, transcribedText, createBabble]);

  const handleSave = async () => {
    if (!transcribedText.trim()) {
      toast.error('Nothing to save. Record something first.');
      return;
    }
    try {
      setIsSaving(true);
      const babble = await saveBabble();
      toast.success('Babble saved!');
      void navigate(`/babble/${babble.id}`);
    } catch {
      toast.error('Failed to save babble');
    } finally {
      setIsSaving(false);
    }
  };

  const handleSaveAndGenerate = async (templateId: string) => {
    if (!transcribedText.trim()) {
      toast.error('Nothing to save. Record something first.');
      return;
    }
    try {
      setIsSaving(true);
      const babble = await saveBabble();
      toast.success('Babble saved!');
      void navigate(`/babble/${babble.id}?autoGenerate=${encodeURIComponent(templateId)}`);
    } catch {
      toast.error('Failed to save babble');
    } finally {
      setIsSaving(false);
    }
  };

  const transcriptionDone = !isRecording && !isConnected;

  const handleClear = useCallback(() => {
    if (window.confirm('Clear the entire transcript? This cannot be undone.')) {
      reset();
      setTitle('');
    }
  }, [reset]);

  return (
    <AuthGuard message="Sign in with your organizational account to record babbles.">
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Record a Babble</h1>
        <p className="text-sm text-muted-foreground">
          Speak your thoughts freely. We&apos;ll transcribe them for you.
        </p>
      </div>

      <div className="space-y-4">
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Give your babble a title (optional)"
        />

        <div className="flex items-center gap-4 rounded-lg border px-4 py-3">
          <RecordButton
            isRecording={isRecording}
            onStart={handleStart}
            onStop={handleStop}
          />
          <RecordingIndicator
            isRecording={isRecording}
            duration={duration}
          />
          <WaveformVisualizer
            analyserRef={analyserRef}
            isRecording={isRecording}
          />
        </div>

        <TranscriptPreview
          finalText={transcribedText}
          partialText={partialText}
          isTranscribing={isConnected}
        />

        {transcriptionError && (
          <p className="text-sm text-destructive">{transcriptionError}</p>
        )}

        <div className="flex gap-2">
          <Button
            onClick={() => void handleSave()}
            disabled={!transcribedText.trim() || !transcriptionDone || isSaving}
          >
            {isSaving ? (
              <Loader2 className="size-4 animate-spin" />
            ) : (
              <Save className="size-4" />
            )}
            Save Babble
          </Button>

          <div className="flex">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="secondary"
                  disabled={!transcribedText.trim() || !transcriptionDone || isSaving}
                >
                  <Sparkles className="size-4" />
                  Save &amp; Generate Prompt
                  <ChevronDown className="size-3" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start">
                {templates.map((t) => (
                  <DropdownMenuItem
                    key={t.id}
                    onClick={() => void handleSaveAndGenerate(t.id)}
                  >
                    {t.name}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <Button
            variant="ghost"
            onClick={handleClear}
            disabled={!transcribedText.trim() || !transcriptionDone}
          >
            <Trash2 className="size-4" />
            Clear
          </Button>
        </div>
      </div>
    </div>
    </AuthGuard>
  );
}
