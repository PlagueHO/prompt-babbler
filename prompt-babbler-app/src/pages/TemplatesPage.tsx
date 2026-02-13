import { useState, useCallback } from 'react';
import { toast } from 'sonner';
import { TemplateList } from '@/components/templates/TemplateList';
import { TemplateEditor } from '@/components/templates/TemplateEditor';
import { useTemplates } from '@/hooks/useTemplates';
import type { PromptTemplate } from '@/types';

export function TemplatesPage() {
  const { templates, createTemplate, updateTemplate, deleteTemplate } =
    useTemplates();
  const [selected, setSelected] = useState<PromptTemplate | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const handleCreate = useCallback(() => {
    setSelected(null);
    setIsCreating(true);
  }, []);

  const handleSave = useCallback(
    (data: { name: string; description: string; systemPrompt: string }) => {
      if (isCreating) {
        const now = new Date().toISOString();
        createTemplate({
          id: crypto.randomUUID(),
          name: data.name,
          description: data.description,
          systemPrompt: data.systemPrompt,
          isBuiltIn: false,
          createdAt: now,
          updatedAt: now,
        });
        toast.success('Template created');
      } else if (selected) {
        updateTemplate({
          ...selected,
          name: data.name,
          description: data.description,
          systemPrompt: data.systemPrompt,
          updatedAt: new Date().toISOString(),
        });
        toast.success('Template updated');
      }
      setSelected(null);
      setIsCreating(false);
    },
    [isCreating, selected, createTemplate, updateTemplate]
  );

  const handleDelete = useCallback(() => {
    if (selected && !selected.isBuiltIn) {
      deleteTemplate(selected.id);
      toast.success('Template deleted');
      setSelected(null);
    }
  }, [selected, deleteTemplate]);

  const handleCancel = useCallback(() => {
    setSelected(null);
    setIsCreating(false);
  }, []);

  if (selected || isCreating) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">
          {isCreating
            ? 'Create Template'
            : selected?.isBuiltIn
              ? 'View Template'
              : 'Edit Template'}
        </h1>
        <TemplateEditor
          template={selected}
          onSave={handleSave}
          onCancel={handleCancel}
          onDelete={selected && !selected.isBuiltIn ? handleDelete : undefined}
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Templates</h1>
        <p className="text-sm text-muted-foreground">
          Manage prompt templates used to transform your babbles into polished
          prompts.
        </p>
      </div>
      <TemplateList
        templates={templates}
        onSelect={setSelected}
        onCreate={handleCreate}
      />
    </div>
  );
}
