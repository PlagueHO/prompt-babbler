import { useState, useCallback, useEffect, useRef } from 'react';
import { useNavigate, useParams, Link } from 'react-router';
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
import { TagInput } from '@/components/ui/tag-input';
import { useAudioRecording } from '@/hooks/useAudioRecording';
import { useTranscription } from '@/hooks/useTranscription';
import { useBabbles } from '@/hooks/useBabbles';
import { useTemplates } from '@/hooks/useTemplates';
import { getSpeechLanguage } from '@/services/local-storage';
import { AuthGuard } from '@/components/layout/AuthGuard';
import { Save, ChevronDown, Sparkles, Loader2, Upload } from 'lucide-react';
import { ClearTranscriptDialog } from '@/components/recording/ClearTranscriptDialog';
import { useFileUpload } from '@/hooks/useFileUpload';
import { usePageTitle } from '@/hooks/usePageTitle';
import type { Babble } from '@/types';

export function RecordPage() {
  const { babbleId } = useParams<{ babbleId: string }>();
  const navigate = useNavigate();
  const { createBabble, updateBabble, getBabble } = useBabbles();
  const { templates } = useTemplates();
  const { upload, isUploading, error: uploadError } = useFileUpload();
  const fileInputRef = useRef<HTMLInputElement>(null);
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
  const [tags, setTags] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  // Append mode: load existing babble when babbleId is provided
  const isAppendMode = !!babbleId;
  const [existingBabble, setExistingBabble] = useState<Babble | null>(null);
  const [babbleLoading, setBabbleLoading] = useState(isAppendMode);
  const [babbleNotFound, setBabbleNotFound] = useState(false);

  let pageTitle = 'Create a Babble';
  if (isAppendMode) {
    pageTitle = 'Continue Babble';
    if (existingBabble?.title) {
      pageTitle = `Continue: ${existingBabble.title}`;
    } else if (!babbleLoading && babbleNotFound) {
      pageTitle = 'Babble not found';
    }
  }

  usePageTitle(pageTitle);

  useEffect(() => {
    if (!babbleId) return;
    let cancelled = false;
    getBabble(babbleId).then((result) => {
      if (cancelled) return;
      if (result) {
        setExistingBabble(result);
        setTitle(result.title);
      } else {
        setBabbleNotFound(true);
      }
      setBabbleLoading(false);
    }).catch(() => {
      if (!cancelled) {
        setBabbleNotFound(true);
        setBabbleLoading(false);
      }
    });
    return () => { cancelled = true; };
  }, [babbleId, getBabble]);

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
    // Start audio capture and WebSocket connection in parallel.
    // Frames arriving before the WebSocket is open are buffered by TranscriptionStream.
    await Promise.all([
      connect(language || undefined),
      startRecording(),
    ]);
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
    if (isAppendMode && existingBabble && babbleId) {
      // Append mode: concatenate existing text with new transcription
      const combinedText = existingBabble.text
        ? `${existingBabble.text}\n\n${transcribedText}`
        : transcribedText;
      const updated = await updateBabble(babbleId, {
        title: title.trim() || existingBabble.title,
        text: combinedText,
      });
      return updated;
    }
    const babble = await createBabble({
      title: title.trim() || `Babble ${new Date().toLocaleDateString()}`,
      text: transcribedText,
      tags,
    });
    return babble;
  }, [title, tags, transcribedText, createBabble, updateBabble, isAppendMode, existingBabble, babbleId]);

  const handleSave = async () => {
    if (!transcribedText.trim()) {
      toast.error('Nothing to save. Record something first.');
      return;
    }
    try {
      setIsSaving(true);
      const babble = await saveBabble();
      toast.success(isAppendMode ? 'Babble updated!' : 'Babble saved!');
      void navigate(`/babble/${babble.id}`);
    } catch {
      toast.error(isAppendMode ? 'Failed to update babble' : 'Failed to save babble');
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
      toast.success(isAppendMode ? 'Babble updated!' : 'Babble saved!');
      void navigate(`/babble/${babble.id}?autoGenerate=${encodeURIComponent(templateId)}`);
    } catch {
      toast.error(isAppendMode ? 'Failed to update babble' : 'Failed to save babble');
    } finally {
      setIsSaving(false);
    }
  };

  const transcriptionDone = !isRecording && !isConnected;

  const handleClear = useCallback(() => {
    reset();
    if (!isAppendMode) {
      setTitle('');
      setTags([]);
    }
  }, [reset, isAppendMode]);

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    e.target.value = '';

    const nameWithoutExt = file.name.replace(/\.[^/.]+$/, '');
    const uploadTitle = title.trim() || nameWithoutExt;
    if (!title.trim()) {
      setTitle(nameWithoutExt);
    }

    try {
      const babble = await upload(file, uploadTitle);
      toast.success('Audio uploaded and transcribed!');
      void navigate(`/babble/${babble.id}`);
    } catch {
      toast.error('Failed to transcribe audio file.');
    }
  };

  if (babbleLoading) {
    return (
      <div className="flex flex-col items-center gap-4 py-12 text-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
        <p className="text-sm text-muted-foreground">Loading babble…</p>
      </div>
    );
  }

  if (babbleNotFound) {
    return (
      <div className="flex flex-col items-center gap-4 py-12 text-center">
        <h1 className="text-xl font-semibold">Babble not found</h1>
        <Button asChild variant="outline">
          <Link to="/">Go home</Link>
        </Button>
      </div>
    );
  }

  return (
    <AuthGuard message="Sign in with your organizational account to record babbles.">
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">
          {isAppendMode ? 'Continue Babble' : 'Create a Babble'}
        </h1>
        <p className="text-sm text-muted-foreground">
          {isAppendMode
            ? 'Continue recording to add more to this babble.'
            : 'Speak your thoughts freely, or upload an audio file — we\'ll transcribe it for you.'}
        </p>
      </div>

      <div className="space-y-4">
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder={isAppendMode ? 'Babble title' : 'Give your babble a title (optional)'}
        />

        {!isAppendMode && (
          <TagInput
            value={tags}
            onChange={setTags}
            placeholder="Add tags (optional)"
          />
        )}

        <div className="flex items-center gap-4 rounded-lg border px-4 py-3">
          <RecordButton
            isRecording={isRecording}
            onStart={handleStart}
            onStop={handleStop}
          />
          <RecordingIndicator
            isRecording={isRecording}
            duration={duration}
            hasTranscription={!!(transcribedText || existingBabble?.text)}
          />
          {!isAppendMode && !isRecording && (
            <>
              <span className="text-xs text-muted-foreground">or</span>
              <Button
                disabled={isUploading}
                onClick={() => fileInputRef.current?.click()}
              >
                {isUploading ? (
                  <Loader2 className="size-4 animate-spin" />
                ) : (
                  <Upload className="size-4" />
                )}
                Upload audio file
              </Button>
              <input
                ref={fileInputRef}
                type="file"
                accept="audio/mpeg,audio/mp3,audio/wav,audio/webm,audio/ogg,audio/mp4,audio/x-m4a,.m4a"
                className="hidden"
                onChange={(e) => void handleFileSelect(e)}
              />
            </>
          )}
          <WaveformVisualizer
            analyserRef={analyserRef}
            isRecording={isRecording}
          />
        </div>

        {uploadError && <p className="text-sm text-destructive">{uploadError}</p>}

        <TranscriptPreview
          finalText={isAppendMode && existingBabble?.text
            ? (transcribedText ? `${existingBabble.text}\n\n${transcribedText}` : existingBabble.text)
            : transcribedText}
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
            {isAppendMode ? 'Update Babble' : 'Save Babble'}
          </Button>

          <div className="flex">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="secondary"
                  disabled={!transcribedText.trim() || !transcriptionDone || isSaving}
                >
                  <Sparkles className="size-4" />
                  {isAppendMode ? 'Update & Generate Prompt' : 'Save & Generate Prompt'}
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

          <ClearTranscriptDialog
            isAppendMode={isAppendMode}
            disabled={!transcribedText.trim() || !transcriptionDone}
            onConfirm={handleClear}
          />
        </div>
      </div>
    </div>
    </AuthGuard>
  );
}
