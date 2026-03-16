import { useState, useCallback, useRef, useEffect } from 'react';
import { useMsal } from '@azure/msal-react';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import {
  TranscriptionStream,
  type TranscriptionMessage,
} from '@/services/transcription-stream';
import { loginRequest } from '@/auth/authConfig';

const TOKEN_REFRESH_MS = 55 * 60 * 1000; // Refresh 5 minutes before ~1 hour expiry

/**
 * Hook that manages a real-time WebSocket transcription session.
 *
 * - `connect(language?)` opens the WebSocket stream (acquires token first).
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
  const tokenRefreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const sessionStateRef = useRef<{ language?: string; isActive: boolean }>({
    language: undefined,
    isActive: false,
  });
  const reconnectRef = useRef<() => Promise<void>>(undefined);
  const { instance, accounts } = useMsal();

  const acquireAccessToken = useCallback(async (): Promise<string | null> => {
    if (accounts.length === 0) return null;
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account: accounts[0],
      });
      return response.accessToken;
    } catch (err) {
      if (err instanceof InteractionRequiredAuthError) {
        try {
          const response = await instance.acquireTokenPopup(loginRequest);
          return response.accessToken;
        } catch {
          return null;
        }
      }
      return null;
    }
  }, [instance, accounts]);

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

  const reconnectWithFreshToken = useCallback(async () => {
    if (!sessionStateRef.current.isActive) return;

    streamRef.current?.close();
    streamRef.current = null;

    const newToken = await acquireAccessToken();
    if (!newToken) {
      setError('Unable to refresh token for continued recording');
      return;
    }

    const stream = new TranscriptionStream(handleMessage, handleError);
    streamRef.current = stream;
    stream.open(sessionStateRef.current.language, newToken);
    setIsConnected(true);

    tokenRefreshTimerRef.current = setTimeout(
      () => void reconnectRef.current?.(),
      TOKEN_REFRESH_MS,
    );
  }, [acquireAccessToken, handleMessage, handleError]);

  // Keep reconnect ref in sync — must be in an effect, not during render
  useEffect(() => {
    reconnectRef.current = reconnectWithFreshToken;
  }, [reconnectWithFreshToken]);

  const connect = useCallback(
    async (language?: string) => {
      if (streamRef.current) return;
      setError(null);
      sessionStateRef.current = { language, isActive: true };

      const token = await acquireAccessToken();
      if (!token) {
        setError('Failed to acquire access token');
        return;
      }

      const stream = new TranscriptionStream(handleMessage, handleError);
      streamRef.current = stream;
      stream.open(language, token);
      setIsConnected(true);

      tokenRefreshTimerRef.current = setTimeout(
        () => void reconnectRef.current?.(),
        TOKEN_REFRESH_MS,
      );
    },
    [acquireAccessToken, handleMessage, handleError],
  );

  const sendAudio = useCallback((pcmBuffer: ArrayBuffer) => {
    streamRef.current?.sendAudio(pcmBuffer);
  }, []);

  const disconnect = useCallback(() => {
    sessionStateRef.current.isActive = false;
    streamRef.current?.close();
    streamRef.current = null;
    setIsConnected(false);
    setPartialText('');
    if (tokenRefreshTimerRef.current) {
      clearTimeout(tokenRefreshTimerRef.current);
      tokenRefreshTimerRef.current = null;
    }
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
