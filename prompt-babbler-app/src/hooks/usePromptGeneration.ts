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

        let done = false;
        while (!done) {
          if (controller.signal.aborted) break;
          const result = await reader.read();
          done = result.done;
          if (result.value) {
            const chunk = decoder.decode(result.value, { stream: !done });
            setGeneratedText((prev) => prev + chunk);
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
