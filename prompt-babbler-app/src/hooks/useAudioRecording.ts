import { useState, useRef, useCallback, useEffect } from 'react';

/**
 * Audio recording hook that captures raw 16 kHz / 16-bit / mono PCM
 * via an AudioWorklet and delivers each worklet frame to the caller.
 */

interface UseAudioRecordingOptions {
  /** Called with each Int16 PCM buffer from the worklet. */
  onPcmFrame?: (buffer: ArrayBuffer) => void;
}

export function useAudioRecording(options: UseAudioRecordingOptions = {}) {
  const { onPcmFrame } = options;
  const [isRecording, setIsRecording] = useState(false);
  const [duration, setDuration] = useState(0);
  const audioContextRef = useRef<AudioContext | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const onPcmFrameRef = useRef(onPcmFrame);

  useEffect(() => {
    onPcmFrameRef.current = onPcmFrame;
  }, [onPcmFrame]);

  const start = useCallback(async () => {
    // Capture mic at 16 kHz mono
    const stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        sampleRate: 16000,
        channelCount: 1,
        echoCancellation: true,
        noiseSuppression: true,
      },
    });
    streamRef.current = stream;

    const audioContext = new AudioContext({ sampleRate: 16000 });
    audioContextRef.current = audioContext;
    console.info('[AudioRecording] AudioContext created — actual sampleRate:', audioContext.sampleRate);

    await audioContext.audioWorklet.addModule('/pcm-processor.js');
    const source = audioContext.createMediaStreamSource(stream);
    const workletNode = new AudioWorkletNode(audioContext, 'pcm-processor');

    workletNode.port.onmessage = (event: MessageEvent<ArrayBuffer>) => {
      onPcmFrameRef.current?.(event.data);
    };

    source.connect(workletNode);
    // Do NOT connect workletNode to destination — it causes echo/feedback.
    // The worklet only needs to process audio, not play it back.
    // workletNode.connect(audioContext.destination);

    // Create an AnalyserNode for waveform visualization.
    // Connect source → analyser (parallel to the worklet path).
    const analyser = audioContext.createAnalyser();
    analyser.fftSize = 256;
    source.connect(analyser);
    analyserRef.current = analyser;

    setIsRecording(true);
    setDuration(0);
    timerRef.current = setInterval(() => {
      setDuration((d) => d + 1);
    }, 1000);
  }, []);

  const stop = useCallback(() => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
    analyserRef.current = null;
    if (audioContextRef.current) {
      void audioContextRef.current.close();
      audioContextRef.current = null;
    }
    if (streamRef.current) {
      streamRef.current.getTracks().forEach((t) => t.stop());
      streamRef.current = null;
    }
    setIsRecording(false);
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
      if (audioContextRef.current) {
        void audioContextRef.current.close();
      }
      if (streamRef.current) {
        streamRef.current.getTracks().forEach((t) => t.stop());
      }
    };
  }, []);

  return { isRecording, duration, start, stop, analyserRef };
}
