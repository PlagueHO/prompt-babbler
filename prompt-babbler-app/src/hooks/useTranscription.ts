import { useState, useCallback, useRef } from 'react';
import {
  TranscriptionStream,
  type TranscriptionMessage,
} from '@/services/transcription-stream';

/**
 * Hook that manages a real-time WebSocket transcription session.
 *
 * - `connect(language?)` opens the WebSocket stream.
 * - `sendAudio(pcmBuffer)` feeds raw PCM audio to the backend.
 * - `disconnect()` closes the stream.
 * - `transcribedText` accumulates the finalised transcript.
 * - `partialText` holds the latest interim (non-final) result.
 */
export function useTranscription() {
  const [transcribedText, setTranscribedText] = useState('');
  const [partialText, setPartialText] = useState('');
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const streamRef = useRef<TranscriptionStream | null>(null);

  const handleMessage = useCallback((msg: TranscriptionMessage) => {
    if (msg.isFinal) {
      setTranscribedText((prev) =>
        prev ? `${prev} ${msg.text}` : msg.text,
      );
      setPartialText('');
    } else {
      setPartialText(msg.text);
    }
  }, []);

  const handleError = useCallback((evt: Event | string) => {
    const message =
      typeof evt === 'string' ? evt : 'WebSocket transcription error';
    setError(message);
  }, []);

  const connect = useCallback(
    (language?: string) => {
      if (streamRef.current) return;
      setError(null);
      const stream = new TranscriptionStream(handleMessage, handleError);
      streamRef.current = stream;
      stream.open(language);
      setIsConnected(true);
    },
    [handleMessage, handleError],
  );

  const sendAudio = useCallback((pcmBuffer: ArrayBuffer) => {
    streamRef.current?.sendAudio(pcmBuffer);
  }, []);

  const disconnect = useCallback(() => {
    streamRef.current?.close();
    streamRef.current = null;
    setIsConnected(false);
    setPartialText('');
  }, []);

  const reset = useCallback(() => {
    disconnect();
    setTranscribedText('');
    setPartialText('');
    setError(null);
  }, [disconnect]);

  return {
    transcribedText,
    partialText,
    isConnected,
    error,
    connect,
    sendAudio,
    disconnect,
    reset,
  };
}
