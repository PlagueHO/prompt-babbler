import { useRef, useEffect, type MutableRefObject } from 'react';

interface WaveformVisualizerProps {
  analyserRef: MutableRefObject<AnalyserNode | null>;
  isRecording: boolean;
}

/**
 * Renders a real-time audio waveform using a canvas element.
 * Reads frequency data from the provided AnalyserNode and draws
 * animated bars that respond to the microphone input.
 */
export function WaveformVisualizer({
  analyserRef,
  isRecording,
}: WaveformVisualizerProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const animationRef = useRef<number>(0);

  useEffect(() => {
    if (!isRecording) {
      cancelAnimationFrame(animationRef.current);
      // Clear canvas when not recording
      const canvas = canvasRef.current;
      if (canvas) {
        const ctx = canvas.getContext('2d');
        if (ctx) {
          ctx.clearRect(0, 0, canvas.width, canvas.height);
        }
      }
      return;
    }

    const draw = () => {
      const canvas = canvasRef.current;
      const analyser = analyserRef.current;
      if (!canvas || !analyser) {
        animationRef.current = requestAnimationFrame(draw);
        return;
      }

      const ctx = canvas.getContext('2d');
      if (!ctx) return;

      // Use device pixel ratio for crisp rendering
      const dpr = window.devicePixelRatio || 1;
      const rect = canvas.getBoundingClientRect();
      canvas.width = rect.width * dpr;
      canvas.height = rect.height * dpr;
      ctx.scale(dpr, dpr);

      const width = rect.width;
      const height = rect.height;

      const bufferLength = analyser.frequencyBinCount;
      const dataArray = new Uint8Array(bufferLength);
      analyser.getByteFrequencyData(dataArray);

      ctx.clearRect(0, 0, width, height);

      // Draw frequency bars
      const barCount = Math.min(bufferLength, 64);
      const gap = 2;
      const barWidth = (width - gap * (barCount - 1)) / barCount;
      const centerY = height / 2;

      for (let i = 0; i < barCount; i++) {
        const value = dataArray[i] / 255;
        const barHeight = Math.max(2, value * centerY * 0.9);

        const x = i * (barWidth + gap);

        // Gradient from primary color at low amplitude to destructive at high
        const hue = 200 - value * 160; // 200 (blue) → 40 (warm) → 0 (red)
        const saturation = 70 + value * 30;
        const lightness = 50 + value * 10;
        ctx.fillStyle = `hsl(${hue}, ${saturation}%, ${lightness}%)`;

        // Draw mirrored bars around the center
        const radius = Math.min(barWidth / 2, 3);
        drawRoundedRect(ctx, x, centerY - barHeight, barWidth, barHeight * 2, radius);
      }

      animationRef.current = requestAnimationFrame(draw);
    };

    animationRef.current = requestAnimationFrame(draw);

    return () => {
      cancelAnimationFrame(animationRef.current);
    };
  }, [isRecording, analyserRef]);

  return (
    <canvas
      ref={canvasRef}
      className="h-10 w-full min-w-30 flex-1"
      style={{ display: isRecording ? 'block' : 'none' }}
    />
  );
}

function drawRoundedRect(
  ctx: CanvasRenderingContext2D,
  x: number,
  y: number,
  w: number,
  h: number,
  r: number,
) {
  r = Math.min(r, w / 2, h / 2);
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.lineTo(x + w - r, y);
  ctx.quadraticCurveTo(x + w, y, x + w, y + r);
  ctx.lineTo(x + w, y + h - r);
  ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
  ctx.lineTo(x + r, y + h);
  ctx.quadraticCurveTo(x, y + h, x, y + h - r);
  ctx.lineTo(x, y + r);
  ctx.quadraticCurveTo(x, y, x + r, y);
  ctx.closePath();
  ctx.fill();
}
