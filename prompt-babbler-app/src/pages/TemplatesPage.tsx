import { useState, useCallback } from 'react';
import { toast } from 'sonner';
import { TemplateList } from '@/components/templates/TemplateList';
import { TemplateEditor } from '@/components/templates/TemplateEditor';
import { AuthGuard } from '@/components/layout/AuthGuard';
import { useTemplates } from '@/hooks/useTemplates';
import type { PromptTemplate } from '@/types';
import type { TemplateRequest } from '@/services/api-client';

export function TemplatesPage() {
  const { templates, loading, error, createTemplate, updateTemplate, deleteTemplate } =
    useTemplates();
  const [selected, setSelected] = useState<PromptTemplate | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const handleCreate = useCallback(() => {
    setSelected(null);
    setIsCreating(true);
  }, []);

  const handleSave = useCallback(
    async (data: TemplateRequest) => {
      if (isCreating) {
        await createTemplate(data);
        toast.success('Template created');
      } else if (selected) {
        await updateTemplate({
          ...selected,
          ...data,
          updatedAt: new Date().toISOString(),
        });
        toast.success('Template updated');
      }
      setSelected(null);
      setIsCreating(false);
    },
    [isCreating, selected, createTemplate, updateTemplate]
  );

  const handleDelete = useCallback(async () => {
    if (selected && !selected.isBuiltIn) {
      await deleteTemplate(selected.id);
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
      <AuthGuard message="Sign in with your organizational account to manage templates.">
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
      </AuthGuard>
    );
  }

  return (
    <AuthGuard message="Sign in with your organizational account to manage templates.">
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Templates</h1>
        <p className="text-sm text-muted-foreground">
          Manage prompt templates used to transform your babbles into polished
          prompts.
        </p>
      </div>
      {loading ? (
        <p className="text-sm text-muted-foreground">Loading templates...</p>
      ) : error ? (
        <p className="text-sm text-destructive">Error: {error}</p>
      ) : (
        <TemplateList
          templates={templates}
          onSelect={setSelected}
          onCreate={handleCreate}
        />
      )}
    </div>
    </AuthGuard>
  );
}
