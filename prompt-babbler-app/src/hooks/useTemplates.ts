import { useState, useCallback, useEffect } from 'react';
import type { PromptTemplate } from '@/types';
import * as api from '@/services/api-client';
import { isAuthConfigured } from '@/auth/authConfig';
import { useAuthToken, useAccountCount } from '@/hooks/useAuthToken';

export function useTemplates() {
  const [templates, setTemplates] = useState<PromptTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const getAuthToken = useAuthToken();
  const accountCount = useAccountCount();

  const fetchTemplates = useCallback(async (forceRefresh = false) => {
    try {
      setLoading(true);
      setError(null);
      const token = await getAuthToken();
      // In authenticated mode, skip fetch until user is signed in.
      if (isAuthConfigured && !token) {
        setTemplates([]);
        return;
      }
      const data = await api.getTemplates(forceRefresh, token);
      setTemplates(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load templates');
    } finally {
      setLoading(false);
    }
  }, [getAuthToken]);

  useEffect(() => {
    if (isAuthConfigured && accountCount === 0) {
      setTemplates([]);
      setLoading(false);
    } else {
      fetchTemplates();
    }
  }, [fetchTemplates, accountCount]);

  const createTemplate = useCallback(
    async (template: api.TemplateRequest): Promise<PromptTemplate> => {
      const token = await getAuthToken();
      const created = await api.createTemplate(template, token);
      await fetchTemplates();
      return created;
    },
    [fetchTemplates, getAuthToken]
  );

  const updateTemplate = useCallback(
    async (template: PromptTemplate): Promise<PromptTemplate> => {
      const token = await getAuthToken();
      const updated = await api.updateTemplate(template.id, {
        name: template.name,
        description: template.description,
        instructions: template.instructions,
        outputDescription: template.outputDescription,
        outputTemplate: template.outputTemplate,
        examples: template.examples,
        guardrails: template.guardrails,
        defaultOutputFormat: template.defaultOutputFormat,
        defaultAllowEmojis: template.defaultAllowEmojis,
        tags: template.tags,
        additionalProperties: template.additionalProperties,
      }, token);
      await fetchTemplates();
      return updated;
    },
    [fetchTemplates, getAuthToken]
  );

  const deleteTemplate = useCallback(
    async (id: string): Promise<void> => {
      const token = await getAuthToken();
      await api.deleteTemplate(id, token);
      await fetchTemplates();
    },
    [fetchTemplates, getAuthToken]
  );

  return { templates, loading, error, createTemplate, updateTemplate, deleteTemplate, refresh: fetchTemplates };
}
