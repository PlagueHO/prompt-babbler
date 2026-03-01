import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { Save, X, Trash2 } from 'lucide-react';
import type { PromptTemplate } from '@/types';

const templateSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100),
  description: z.string().min(1, 'Description is required').max(500),
  systemPrompt: z.string().min(1, 'System prompt is required').max(10000),
});

type TemplateFormData = z.infer<typeof templateSchema>;

interface TemplateEditorProps {
  template: PromptTemplate | null;
  onSave: (data: TemplateFormData) => void;
  onCancel: () => void;
  onDelete?: () => void;
}

export function TemplateEditor({
  template,
  onSave,
  onCancel,
  onDelete,
}: TemplateEditorProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<TemplateFormData>({
    resolver: zodResolver(templateSchema),
    defaultValues: {
      name: template?.name ?? '',
      description: template?.description ?? '',
      systemPrompt: template?.systemPrompt ?? '',
    },
  });

  const isBuiltIn = template?.isBuiltIn ?? false;

  return (
    <form onSubmit={(e) => void handleSubmit(onSave)(e)} className="space-y-4">
      <div className="space-y-2">
        <label htmlFor="name" className="text-sm font-medium">
          Name
        </label>
        <Input
          id="name"
          {...register('name')}
          disabled={isBuiltIn}
          placeholder="Template name"
        />
        {errors.name && (
          <p className="text-sm text-destructive">{errors.name.message}</p>
        )}
      </div>
      <div className="space-y-2">
        <label htmlFor="description" className="text-sm font-medium">
          Description
        </label>
        <Input
          id="description"
          {...register('description')}
          disabled={isBuiltIn}
          placeholder="What this template does"
        />
        {errors.description && (
          <p className="text-sm text-destructive">
            {errors.description.message}
          </p>
        )}
      </div>
      <div className="space-y-2">
        <label htmlFor="systemPrompt" className="text-sm font-medium">
          System Prompt
        </label>
        <Textarea
          id="systemPrompt"
          {...register('systemPrompt')}
          disabled={isBuiltIn}
          placeholder="The system prompt to use when generating"
          className="min-h-[200px] font-mono text-sm"
        />
        {errors.systemPrompt && (
          <p className="text-sm text-destructive">
            {errors.systemPrompt.message}
          </p>
        )}
      </div>
      <div className="flex gap-2">
        {!isBuiltIn && (
          <Button type="submit" size="sm" disabled={isSubmitting}>
            <Save className="size-4" />
            Save
          </Button>
        )}
        <Button type="button" size="sm" variant="outline" onClick={onCancel}>
          <X className="size-4" />
          {isBuiltIn ? 'Close' : 'Cancel'}
        </Button>
        {!isBuiltIn && onDelete && template && (
          <Button
            type="button"
            size="sm"
            variant="destructive"
            onClick={onDelete}
          >
            <Trash2 className="size-4" />
            Delete
          </Button>
        )}
      </div>
    </form>
  );
}
