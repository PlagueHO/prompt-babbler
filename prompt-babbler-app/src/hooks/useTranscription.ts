import { useState, useCallback, useRef } from 'react';
import * as api from '@/services/api-client';

export function useTranscription() {
  const [transcribedText, setTranscribedText] = useState('');
  const [isTranscribing, setIsTranscribing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const pendingRef = useRef(0);

  const transcribeChunk = useCallback(
    async (blob: Blob, language?: string) => {
      pendingRef.current += 1;
      setIsTranscribing(true);
      setError(null);
      try {
        const result = await api.transcribeAudio(blob, language);
        if (result.text) {
          setTranscribedText((prev) =>
            prev ? `${prev} ${result.text}` : result.text
          );
        }
      } catch (err) {
        setError(
          err instanceof Error ? err.message : 'Transcription failed'
        );
      } finally {
        pendingRef.current -= 1;
        if (pendingRef.current <= 0) {
          pendingRef.current = 0;
          setIsTranscribing(false);
        }
      }
    },
    []
  );

  const reset = useCallback(() => {
    setTranscribedText('');
    setError(null);
  }, []);

  return { transcribedText, isTranscribing, error, transcribeChunk, reset };
}
