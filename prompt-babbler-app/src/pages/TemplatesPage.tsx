import { useState, useCallback } from 'react';
import { toast } from 'sonner';
import { TemplateList } from '@/components/templates/TemplateList';
import { TemplateEditor } from '@/components/templates/TemplateEditor';
import { AuthGuard } from '@/components/layout/AuthGuard';
import { ErrorBanner } from '@/components/ui/error-banner';
import { useTemplates } from '@/hooks/useTemplates';
import { useTemplateList } from '@/hooks/useTemplateList';
import { usePageTitle } from '@/hooks/usePageTitle';
import type { PromptTemplate } from '@/types';
import type { TemplateRequest } from '@/services/api-client';

export function TemplatesPage() {
  const { createTemplate, updateTemplate, deleteTemplate } = useTemplates({ lazy: true });
  const {
    templates,
    loading,
    loadingMore,
    error,
    refresh,
    loadMore,
    nameFilter,
    setNameFilter,
    tagFilter,
    setTagFilter,
    order,
    setOrder,
  } = useTemplateList();
  const [selected, setSelected] = useState<PromptTemplate | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  let pageTitle = 'Templates';
  if (isCreating) {
    pageTitle = 'Create Template';
  } else if (selected) {
    if (selected.isBuiltIn) {
      pageTitle = selected.name ? `Template: ${selected.name}` : 'View Template';
    } else {
      pageTitle = selected.name ? `Edit Template: ${selected.name}` : 'Edit Template';
    }
  }

  usePageTitle(pageTitle);

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
      await refresh();
      setSelected(null);
      setIsCreating(false);
    },
    [isCreating, selected, createTemplate, updateTemplate, refresh]
  );

  const handleDelete = useCallback(async () => {
    if (selected && !selected.isBuiltIn) {
      await deleteTemplate(selected.id);
      toast.success('Template deleted');
      await refresh();
      setSelected(null);
    }
  }, [selected, deleteTemplate, refresh]);

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
        <ErrorBanner error={error} onRetry={() => void refresh()} />
      ) : (
        <TemplateList
          templates={templates}
          onSelect={setSelected}
          onCreate={handleCreate}
          nameFilter={nameFilter}
          onNameFilterChange={setNameFilter}
          tagFilter={tagFilter}
          onTagFilterChange={setTagFilter}
          order={order}
          onOrderChange={setOrder}
          loadMore={loadMore}
          loadingMore={loadingMore}
          loading={loading}
        />
      )}
    </div>
    </AuthGuard>
  );
}
