import { useState, useCallback, useRef } from 'react';
import * as api from '@/services/api-client';

export function usePromptGeneration() {
  const [generatedText, setGeneratedText] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const generate = useCallback(
    async (babbleText: string, systemPrompt: string) => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setGeneratedText('');
      setIsGenerating(true);
      setError(null);

      try {
        const stream = await api.generatePrompt(babbleText, systemPrompt);
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
                const parsed = JSON.parse(payload) as { text?: string };
                if (parsed.text) {
                  setGeneratedText((prev) => prev + parsed.text);
                }
              } catch {
                // Skip malformed JSON frames
              }
            }
          }
        }
      } catch (err) {
        if (controller.signal.aborted) return;
        setError(
          err instanceof Error ? err.message : 'Prompt generation failed'
        );
      } finally {
        if (!controller.signal.aborted) {
          setIsGenerating(false);
        }
      }
    },
    []
  );

  return { generatedText, isGenerating, error, generate };
}
