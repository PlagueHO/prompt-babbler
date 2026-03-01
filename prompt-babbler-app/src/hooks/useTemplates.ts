import { useState, useCallback } from 'react';
import type { PromptTemplate } from '@/types';
import * as storage from '@/services/local-storage';

export function useTemplates() {
  const [templates, setTemplates] = useState<PromptTemplate[]>(() =>
    storage.getTemplates()
  );

  const refresh = useCallback(() => {
    setTemplates(storage.getTemplates());
  }, []);

  const createTemplate = useCallback(
    (template: PromptTemplate): PromptTemplate => {
      storage.createTemplate(template);
      refresh();
      return template;
    },
    [refresh]
  );

  const updateTemplate = useCallback(
    (template: PromptTemplate): PromptTemplate => {
      storage.updateTemplate(template);
      refresh();
      return template;
    },
    [refresh]
  );

  const deleteTemplate = useCallback(
    (id: string): void => {
      storage.deleteTemplate(id);
      refresh();
    },
    [refresh]
  );

  return { templates, createTemplate, updateTemplate, deleteTemplate };
}
