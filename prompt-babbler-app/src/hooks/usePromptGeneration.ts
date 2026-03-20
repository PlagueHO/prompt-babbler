import { useState, useCallback, useRef } from 'react';
import { SpanStatusCode } from '@opentelemetry/api';
import * as api from '@/services/api-client';
import { useAuthToken } from '@/hooks/useAuthToken';
import { tracer, meter } from '@/telemetry';
import type { PromptFormat } from '@/types';

const ttftHistogram = meter.createHistogram('prompt.ttft_ms', {
  description: 'Time from generate() call to first SSE token (ms)',
  unit: 'ms',
});
const durationHistogram = meter.createHistogram('prompt.duration_ms', {
  description: 'Total prompt generation duration (ms)',
  unit: 'ms',
});

export function usePromptGeneration() {
  const [generatedText, setGeneratedText] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const getAuthToken = useAuthToken();

  const generate = useCallback(
    async (
      babbleId: string,
      templateId: string,
      promptFormat: PromptFormat = 'text',
      allowEmojis: boolean = false
    ): Promise<{ text: string; promptId: string | null }> => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setGeneratedText('');
      setIsGenerating(true);
      setError(null);

      let localText = '';
      let localPromptId: string | null = null;

      const genStart = performance.now();
      const span = tracer.startSpan('prompt.generate', {
        attributes: {
          'prompt.template_id': templateId,
          'prompt.format': promptFormat,
          'prompt.allow_emojis': allowEmojis,
        },
      });
      let firstTokenRecorded = false;

      try {
        const token = await getAuthToken();
        const stream = await api.generatePrompt(babbleId, templateId, promptFormat, allowEmojis, token);
        const reader = stream.getReader();
        const decoder = new TextDecoder();

        let buffer = '';
        let done = false;
        while (!done) {
          if (controller.signal.aborted) break;
          const result = await reader.read();
          done = result.done;
          if (result.value) {
            buffer += decoder.decode(result.value, { stream: !done });

            // Parse SSE frames from the buffer
            const parts = buffer.split('\n\n');
            // Keep the last part as incomplete buffer
            buffer = parts.pop() ?? '';
            for (const part of parts) {
              const line = part.trim();
              if (!line.startsWith('data: ')) continue;
              const payload = line.slice(6);
              if (payload === '[DONE]') break;
              try {
                const parsed = JSON.parse(payload) as { text?: string; promptId?: string };
                if (parsed.promptId) {
                  localPromptId = parsed.promptId;
                }
                if (parsed.text) {
                  if (!firstTokenRecorded) {
                    firstTokenRecorded = true;
                    const ttftMs = performance.now() - genStart;
                    ttftHistogram.record(ttftMs);
                    span.setAttribute('prompt.ttft_ms', ttftMs);
                  }
                  localText += parsed.text;
                  setGeneratedText((prev) => prev + parsed.text);
                }
              } catch {
                // Skip malformed JSON frames
              }
            }
          }
        }
      } catch (err) {
        if (controller.signal.aborted) {
          span.setAttribute('cancelled', true);
          span.end();
          return { text: localText, promptId: localPromptId };
        }
        const message = err instanceof Error ? err.message : 'Prompt generation failed';
        setError(message);
        span.setStatus({ code: SpanStatusCode.ERROR, message });
      } finally {
        if (!controller.signal.aborted) {
          setIsGenerating(false);
          const durationMs = performance.now() - genStart;
          durationHistogram.record(durationMs);
          span.setAttribute('prompt.duration_ms', durationMs);
          span.setAttribute('prompt.output_length', localText.length);
          span.end();
        }
      }

      return { text: localText, promptId: localPromptId };
    },
    [getAuthToken]
  );

  return { generatedText, isGenerating, error, generate };
}
