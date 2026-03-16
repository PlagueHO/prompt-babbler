import { useState, useCallback, useRef } from 'react';
import { useMsal } from '@azure/msal-react';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import * as api from '@/services/api-client';
import { loginRequest } from '@/auth/authConfig';
import type { PromptFormat } from '@/types';

export function usePromptGeneration() {
  const [generatedText, setGeneratedText] = useState('');
  const [generatedName, setGeneratedName] = useState<string | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const { instance, accounts } = useMsal();

  const getAuthToken = useCallback(async (): Promise<string | undefined> => {
    if (accounts.length === 0) return undefined;
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account: accounts[0],
      });
      return response.accessToken;
    } catch (err) {
      if (err instanceof InteractionRequiredAuthError) {
        const response = await instance.acquireTokenPopup(loginRequest);
        return response.accessToken;
      }
      throw err;
    }
  }, [instance, accounts]);

  const generate = useCallback(
    async (
      babbleText: string,
      templateId: string,
      promptFormat: PromptFormat = 'text',
      allowEmojis: boolean = false
    ): Promise<{ name: string | null }> => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setGeneratedText('');
      setGeneratedName(null);
      setIsGenerating(true);
      setError(null);

      let localName: string | null = null;

      try {
        const token = await getAuthToken();
        const stream = await api.generatePrompt(babbleText, templateId, promptFormat, allowEmojis, token);
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
                const parsed = JSON.parse(payload) as { text?: string; name?: string };
                if (parsed.name) {
                  setGeneratedName(parsed.name);
                  localName = parsed.name;
                }
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

      return { name: localName };
    },
    [getAuthToken]
  );

  return { generatedText, generatedName, isGenerating, error, generate };
}
