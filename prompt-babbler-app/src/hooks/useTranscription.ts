import { useState, useCallback, useRef, useEffect } from 'react';
import { SpanStatusCode, type Span } from '@opentelemetry/api';
import {
  TranscriptionStream,
  type TranscriptionMessage,
} from '@/services/transcription-stream';
import { useAuthToken } from '@/hooks/useAuthToken';
import { isAuthConfigured } from '@/auth/authConfig';
import { tracer, meter } from '@/telemetry';

const TOKEN_REFRESH_MS = 55 * 60 * 1000; // Refresh 5 minutes before ~1 hour expiry

const ttfwHistogram = meter.createHistogram('transcription.ttfw_ms', {
  description: 'Time from connect() to first transcription word (ms)',
  unit: 'ms',
});
const wsConnectHistogram = meter.createHistogram('transcription.ws_connect_ms', {
  description: 'WebSocket connection establishment time (ms)',
  unit: 'ms',
});

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
  const partialTextRef = useRef('');
  const acquireAccessToken = useAuthToken();

  // OTEL tracing refs
  const sessionSpanRef = useRef<Span | null>(null);
  const ttfwSpanRef = useRef<Span | null>(null);
  const connectStartRef = useRef(0);
  const firstWordReceivedRef = useRef(false);

  const handleMessage = useCallback((msg: TranscriptionMessage) => {
    // Record TTFW on the very first transcription message (partial or final)
    if (!firstWordReceivedRef.current && connectStartRef.current > 0) {
      firstWordReceivedRef.current = true;
      const ttfwMs = performance.now() - connectStartRef.current;
      ttfwHistogram.record(ttfwMs);
      if (ttfwSpanRef.current) {
        ttfwSpanRef.current.setAttribute('ttfw_ms', ttfwMs);
        ttfwSpanRef.current.setAttribute('first_text', msg.text.slice(0, 80));
        ttfwSpanRef.current.end();
        ttfwSpanRef.current = null;
      }
    }

    if (msg.isFinal) {
      setTranscribedText((prev) =>
        prev ? `${prev} ${msg.text}` : msg.text,
      );
      setPartialText('');
      partialTextRef.current = '';
    } else {
      setPartialText(msg.text);
      partialTextRef.current = msg.text;
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
    if (isAuthConfigured && !newToken) {
      setError('Unable to refresh token for continued recording');
      return;
    }

    const stream = new TranscriptionStream(handleMessage, handleError);
    streamRef.current = stream;
    await stream.open(sessionStateRef.current.language, newToken);
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
      firstWordReceivedRef.current = false;
      connectStartRef.current = performance.now();

      // Start OTEL spans for the transcription session and TTFW measurement
      const sessionSpan = tracer.startSpan('transcription.session', {
        attributes: { 'transcription.language': language ?? 'default' },
      });
      sessionSpanRef.current = sessionSpan;
      ttfwSpanRef.current = tracer.startSpan('transcription.time-to-first-word', {
        attributes: { 'transcription.language': language ?? 'default' },
      });

      const token = await acquireAccessToken();
      if (isAuthConfigured && !token) {
        setError('Failed to acquire access token');
        sessionSpan.setStatus({ code: SpanStatusCode.ERROR, message: 'Token acquisition failed' });
        sessionSpan.end();
        ttfwSpanRef.current?.end();
        ttfwSpanRef.current = null;
        sessionSpanRef.current = null;
        return;
      }

      const wsStart = performance.now();
      const stream = new TranscriptionStream(handleMessage, handleError);
      streamRef.current = stream;
      await stream.open(language, token);
      const wsConnectMs = performance.now() - wsStart;
      wsConnectHistogram.record(wsConnectMs);
      sessionSpan.setAttribute('ws_connect_ms', wsConnectMs);
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

    // End any open OTEL spans
    if (ttfwSpanRef.current) {
      ttfwSpanRef.current.setAttribute('cancelled', true);
      ttfwSpanRef.current.end();
      ttfwSpanRef.current = null;
    }
    if (sessionSpanRef.current) {
      if (connectStartRef.current > 0) {
        sessionSpanRef.current.setAttribute(
          'session_duration_ms',
          performance.now() - connectStartRef.current,
        );
      }
      sessionSpanRef.current.end();
      sessionSpanRef.current = null;
    }

    // Promote any pending partial text to final before clearing
    if (partialTextRef.current) {
      const pending = partialTextRef.current;
      setTranscribedText((prev) =>
        prev ? `${prev} ${pending}` : pending,
      );
      partialTextRef.current = '';
    }
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
