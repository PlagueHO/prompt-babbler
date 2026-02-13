import { useState, useRef, useCallback, useEffect } from 'react';

interface UseAudioRecordingOptions {
  onChunk?: (blob: Blob) => void;
  chunkIntervalMs?: number;
}

export function useAudioRecording(options: UseAudioRecordingOptions = {}) {
  const { onChunk, chunkIntervalMs = 5000 } = options;
  const [isRecording, setIsRecording] = useState(false);
  const [duration, setDuration] = useState(0);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const chunkTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const onChunkRef = useRef(onChunk);
  useEffect(() => {
    onChunkRef.current = onChunk;
  }, [onChunk]);

  const stopMediaRecorder = useCallback(() => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
      mediaRecorderRef.current.stop();
    }
  }, []);

  const startMediaRecorder = useCallback((stream: MediaStream) => {
    const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
      ? 'audio/webm;codecs=opus'
      : 'audio/webm';
    const recorder = new MediaRecorder(stream, { mimeType });
    const chunks: Blob[] = [];

    recorder.ondataavailable = (e) => {
      if (e.data.size > 0) {
        chunks.push(e.data);
      }
    };

    recorder.onstop = () => {
      if (chunks.length > 0) {
        const blob = new Blob(chunks, { type: mimeType });
        onChunkRef.current?.(blob);
        chunks.length = 0;
      }
    };

    recorder.start();
    mediaRecorderRef.current = recorder;
  }, []);

  const start = useCallback(async () => {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    streamRef.current = stream;
    setIsRecording(true);
    setDuration(0);

    startMediaRecorder(stream);

    timerRef.current = setInterval(() => {
      setDuration((d) => d + 1);
    }, 1000);

    // Cycle: stop recorder, then start a new one to emit chunks
    chunkTimerRef.current = setInterval(() => {
      if (mediaRecorderRef.current && mediaRecorderRef.current.state === 'recording') {
        mediaRecorderRef.current.stop();
        // Start a new recorder on the same stream
        if (streamRef.current) {
          startMediaRecorder(streamRef.current);
        }
      }
    }, chunkIntervalMs);
  }, [chunkIntervalMs, startMediaRecorder]);

  const stop = useCallback(() => {
    if (chunkTimerRef.current) {
      clearInterval(chunkTimerRef.current);
      chunkTimerRef.current = null;
    }
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
    stopMediaRecorder();
    if (streamRef.current) {
      streamRef.current.getTracks().forEach((t) => t.stop());
      streamRef.current = null;
    }
    mediaRecorderRef.current = null;
    setIsRecording(false);
  }, [stopMediaRecorder]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (chunkTimerRef.current) clearInterval(chunkTimerRef.current);
      if (timerRef.current) clearInterval(timerRef.current);
      if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
        mediaRecorderRef.current.stop();
      }
      if (streamRef.current) {
        streamRef.current.getTracks().forEach((t) => t.stop());
      }
    };
  }, []);

  return { isRecording, duration, start, stop };
}
