import { useState, useCallback, useEffect } from 'react';
import { useNavigate } from 'react-router';
import { toast } from 'sonner';
import { RecordButton } from '@/components/recording/RecordButton';
import { RecordingIndicator } from '@/components/recording/RecordingIndicator';
import { TranscriptPreview } from '@/components/recording/TranscriptPreview';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useAudioRecording } from '@/hooks/useAudioRecording';
import { useTranscription } from '@/hooks/useTranscription';
import { useBabbles } from '@/hooks/useBabbles';
import { getSpeechLanguage } from '@/services/local-storage';
import type { Babble } from '@/types';
import { Save } from 'lucide-react';

export function RecordPage() {
  const navigate = useNavigate();
  const { createBabble } = useBabbles();
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
  const [babbleId] = useState(() => crypto.randomUUID());

  const language = getSpeechLanguage();

  const onPcmFrame = useCallback(
    (buffer: ArrayBuffer) => {
      sendAudio(buffer);
    },
    [sendAudio],
  );

  const { isRecording, duration, start: startRecording, stop: stopRecording } = useAudioRecording({
    onPcmFrame,
  });

  const handleStart = useCallback(async () => {
    connect(language || undefined);
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

  const handleSave = () => {
    if (!transcribedText.trim()) {
      toast.error('Nothing to save. Record something first.');
      return;
    }
    const now = new Date().toISOString();
    const babble: Babble = {
      id: babbleId,
      title: title.trim() || `Babble ${new Date().toLocaleDateString()}`,
      text: transcribedText,
      createdAt: now,
      updatedAt: now,
      lastGeneratedPrompt: null,
    };
    createBabble(babble);
    toast.success('Babble saved!');
    void navigate(`/babble/${babble.id}`);
  };

  // Display text: final text + partial result
  const displayText = partialText
    ? transcribedText ? `${transcribedText} ${partialText}` : partialText
    : transcribedText;

  return (
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

        <div className="flex flex-col items-center gap-6 rounded-lg border p-8">
          <RecordButton
            isRecording={isRecording}
            onStart={handleStart}
            onStop={handleStop}
          />
          <RecordingIndicator
            isRecording={isRecording}
            duration={duration}
            onStart={handleStart}
            onStop={handleStop}
          />
        </div>

        <TranscriptPreview
          text={displayText}
          isTranscribing={isConnected}
        />

        {transcriptionError && (
          <p className="text-sm text-destructive">{transcriptionError}</p>
        )}

        <div className="flex gap-2">
          <Button
            onClick={handleSave}
            disabled={!transcribedText.trim()}
          >
            <Save className="size-4" />
            Save Babble
          </Button>
        </div>
      </div>
    </div>
  );
}
