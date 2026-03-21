import { useForm, useFieldArray, useWatch, Controller } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Input } from '@/components/ui/input';
import { TagInput } from '@/components/ui/tag-input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Save, X, Trash2, Plus } from 'lucide-react';
import type { PromptTemplate } from '@/types';
import type { TemplateRequest } from '@/services/api-client';

const exampleSchema = z.object({
  input: z.string().min(1, 'Input is required').max(5000),
  output: z.string().min(1, 'Output is required').max(10000),
});

const templateSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100),
  description: z.string().min(1, 'Description is required').max(500),
  instructions: z.string().min(1, 'Instructions are required').max(10000),
  outputDescription: z.string().max(2000).optional().or(z.literal('')),
  outputTemplate: z.string().max(10000).optional().or(z.literal('')),
  examples: z.array(exampleSchema).max(10).optional(),
  guardrails: z.string().max(10000).optional().or(z.literal('')),
  defaultOutputFormat: z.enum(['text', 'markdown']).optional(),
  defaultAllowEmojis: z.boolean().optional(),
  tags: z.array(z.string().max(50)).max(20).optional(),
});

type TemplateFormData = z.infer<typeof templateSchema>;

interface TemplateEditorProps {
  template: PromptTemplate | null;
  onSave: (data: TemplateRequest) => void;
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
    control,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<TemplateFormData>({
    resolver: zodResolver(templateSchema),
    defaultValues: {
      name: template?.name ?? '',
      description: template?.description ?? '',
      instructions: template?.instructions ?? '',
      outputDescription: template?.outputDescription ?? '',
      outputTemplate: template?.outputTemplate ?? '',
      examples: template?.examples ?? [],
      guardrails: template?.guardrails?.join('\n') ?? '',
      defaultOutputFormat: template?.defaultOutputFormat ?? 'text',
      defaultAllowEmojis: template?.defaultAllowEmojis ?? false,
      tags: template?.tags ?? [],
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'examples',
  });

  const isBuiltIn = template?.isBuiltIn ?? false;

  const defaultOutputFormat = useWatch({ control, name: 'defaultOutputFormat' });
  const defaultAllowEmojis = useWatch({ control, name: 'defaultAllowEmojis' });

  const handleFormSubmit = (data: TemplateFormData) => {
    const request: TemplateRequest = {
      name: data.name,
      description: data.description,
      instructions: data.instructions,
      outputDescription: data.outputDescription || undefined,
      outputTemplate: data.outputTemplate || undefined,
      examples: data.examples && data.examples.length > 0 ? data.examples : undefined,
      guardrails: data.guardrails
        ? data.guardrails.split('\n').map(s => s.trim()).filter(Boolean)
        : undefined,
      defaultOutputFormat: data.defaultOutputFormat,
      defaultAllowEmojis: data.defaultAllowEmojis,
      tags: data.tags && data.tags.length > 0 ? data.tags : undefined,
    };
    onSave(request);
  };

  return (
    <form onSubmit={(e) => void handleSubmit(handleFormSubmit)(e)} className="space-y-6">
      {/* Basic fields */}
      <div className="space-y-4">
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
      </div>

      {/* Instructions */}
      <div className="space-y-2">
        <label htmlFor="instructions" className="text-sm font-medium">
          Instructions
        </label>
        <Textarea
          id="instructions"
          {...register('instructions')}
          disabled={isBuiltIn}
          placeholder="Core instructions for the LLM when generating prompts"
          className="min-h-[200px] font-mono text-sm"
        />
        {errors.instructions && (
          <p className="text-sm text-destructive">
            {errors.instructions.message}
          </p>
        )}
      </div>

      {/* Output Description */}
      <div className="space-y-2">
        <label htmlFor="outputDescription" className="text-sm font-medium">
          Output Description <span className="text-muted-foreground">(optional)</span>
        </label>
        <Textarea
          id="outputDescription"
          {...register('outputDescription')}
          disabled={isBuiltIn}
          placeholder="Describe what the generated output should look like"
          className="min-h-[80px] text-sm"
        />
      </div>

      {/* Output Template */}
      <div className="space-y-2">
        <label htmlFor="outputTemplate" className="text-sm font-medium">
          Output Template <span className="text-muted-foreground">(optional)</span>
        </label>
        <Textarea
          id="outputTemplate"
          {...register('outputTemplate')}
          disabled={isBuiltIn}
          placeholder="A structural template the output should follow"
          className="min-h-[100px] font-mono text-sm"
        />
      </div>

      {/* Examples */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <label className="text-sm font-medium">
            Examples <span className="text-muted-foreground">(optional, max 10)</span>
          </label>
          {!isBuiltIn && fields.length < 10 && (
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={() => append({ input: '', output: '' })}
            >
              <Plus className="size-4" />
              Add Example
            </Button>
          )}
        </div>
        {fields.map((field, index) => (
          <div key={field.id} className="rounded-md border p-3 space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-muted-foreground">
                Example {index + 1}
              </span>
              {!isBuiltIn && (
                <Button
                  type="button"
                  size="sm"
                  variant="ghost"
                  onClick={() => remove(index)}
                >
                  <Trash2 className="size-3" />
                </Button>
              )}
            </div>
            <Textarea
              {...register(`examples.${index}.input`)}
              disabled={isBuiltIn}
              placeholder="Example input (babble text)"
              className="min-h-[60px] text-sm"
            />
            {errors.examples?.[index]?.input && (
              <p className="text-sm text-destructive">
                {errors.examples[index].input?.message}
              </p>
            )}
            <Textarea
              {...register(`examples.${index}.output`)}
              disabled={isBuiltIn}
              placeholder="Expected output"
              className="min-h-[60px] text-sm"
            />
            {errors.examples?.[index]?.output && (
              <p className="text-sm text-destructive">
                {errors.examples[index].output?.message}
              </p>
            )}
          </div>
        ))}
      </div>

      {/* Guardrails */}
      <div className="space-y-2">
        <label htmlFor="guardrails" className="text-sm font-medium">
          Guardrails <span className="text-muted-foreground">(optional, one per line)</span>
        </label>
        <Textarea
          id="guardrails"
          {...register('guardrails')}
          disabled={isBuiltIn}
          placeholder="Things the LLM must NOT do, one per line"
          className="min-h-[80px] text-sm"
        />
      </div>

      {/* Defaults */}
      <div className="flex flex-wrap gap-6">
        <div className="space-y-2">
          <label className="text-sm font-medium">Default Output Format</label>
          <Select
            value={defaultOutputFormat ?? 'text'}
            onValueChange={(v) => setValue('defaultOutputFormat', v as 'text' | 'markdown')}
            disabled={isBuiltIn}
          >
            <SelectTrigger className="w-[160px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="text">Text</SelectItem>
              <SelectItem value="markdown">Markdown</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex items-center gap-2 pt-6">
          <Checkbox
            id="defaultAllowEmojis"
            checked={defaultAllowEmojis ?? false}
            onCheckedChange={(v) => setValue('defaultAllowEmojis', v === true)}
            disabled={isBuiltIn}
          />
          <label htmlFor="defaultAllowEmojis" className="text-sm font-medium">
            Allow Emojis
          </label>
        </div>
      </div>

      {/* Tags */}
      <div className="space-y-2">
        <label htmlFor="tags" className="text-sm font-medium">
          Tags <span className="text-muted-foreground">(optional)</span>
        </label>
        <Controller
          control={control}
          name="tags"
          render={({ field }) => (
            <TagInput
              id="tags"
              value={field.value ?? []}
              onChange={field.onChange}
              disabled={isBuiltIn}
              maxTags={20}
              maxTagLength={50}
              placeholder="Add a tag…"
            />
          )}
        />
      </div>

      {/* Actions */}
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
