import { useState, useCallback, useEffect } from 'react';
import { useMsal } from '@azure/msal-react';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import type { PromptTemplate } from '@/types';
import * as api from '@/services/api-client';
import { loginRequest } from '@/auth/authConfig';

export function useTemplates() {
  const [templates, setTemplates] = useState<PromptTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
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

  const fetchTemplates = useCallback(async (forceRefresh = false) => {
    try {
      setLoading(true);
      setError(null);
      const token = await getAuthToken();
      if (!token) {
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
    if (accounts.length > 0) {
      fetchTemplates();
    } else {
      setTemplates([]);
      setLoading(false);
    }
  }, [fetchTemplates, accounts.length]);

  const createTemplate = useCallback(
    async (template: { name: string; description: string; systemPrompt: string }): Promise<PromptTemplate> => {
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
        systemPrompt: template.systemPrompt,
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
