import { useState, useCallback, useEffect } from 'react';
import type { PromptTemplate } from '@/types';
import * as api from '@/services/api-client';

export function useTemplates() {
  const [templates, setTemplates] = useState<PromptTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchTemplates = useCallback(async (forceRefresh = false) => {
    try {
      setLoading(true);
      setError(null);
      const data = await api.getTemplates(forceRefresh);
      setTemplates(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load templates');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTemplates();
  }, [fetchTemplates]);

  const createTemplate = useCallback(
    async (template: { name: string; description: string; systemPrompt: string }): Promise<PromptTemplate> => {
      const created = await api.createTemplate(template);
      await fetchTemplates();
      return created;
    },
    [fetchTemplates]
  );

  const updateTemplate = useCallback(
    async (template: PromptTemplate): Promise<PromptTemplate> => {
      const updated = await api.updateTemplate(template.id, {
        name: template.name,
        description: template.description,
        systemPrompt: template.systemPrompt,
      });
      await fetchTemplates();
      return updated;
    },
    [fetchTemplates]
  );

  const deleteTemplate = useCallback(
    async (id: string): Promise<void> => {
      await api.deleteTemplate(id);
      await fetchTemplates();
    },
    [fetchTemplates]
  );

  return { templates, loading, error, createTemplate, updateTemplate, deleteTemplate, refresh: fetchTemplates };
}
