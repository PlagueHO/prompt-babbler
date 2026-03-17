import { useState, useCallback, useEffect, useRef } from 'react';
import { useParams, useNavigate, useSearchParams, Link } from 'react-router';
import { toast } from 'sonner';
import { Edit, Trash2, Mic, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { BabbleEditor } from '@/components/babbles/BabbleEditor';
import { DeleteBabbleDialog } from '@/components/babbles/DeleteBabbleDialog';
import { PromptGenerator } from '@/components/prompts/PromptGenerator';
import { PromptDisplay } from '@/components/prompts/PromptDisplay';
import { CopyButton } from '@/components/prompts/CopyButton';
import { PromptHistoryList } from '@/components/prompts/PromptHistoryList';
import { AuthGuard } from '@/components/layout/AuthGuard';
import { useBabbles } from '@/hooks/useBabbles';
import { useTemplates } from '@/hooks/useTemplates';
import { usePromptGeneration } from '@/hooks/usePromptGeneration';
import { useGeneratedPrompts } from '@/hooks/useGeneratedPrompts';
import type { Babble, GeneratedPrompt } from '@/types';
import type { PromptGenerateOptions } from '@/components/prompts/PromptGenerator';

export function BabblePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { updateBabble, deleteBabble, getBabble } = useBabbles();
  const { templates } = useTemplates();
  const { generatedText, isGenerating, error: genError, generate } = usePromptGeneration();
  const {
    prompts,
    loading: promptsLoading,
    error: promptsError,
    hasMore: promptsHasMore,
    loadMore: promptsLoadMore,
    createPrompt,
    deletePrompt,
  } = useGeneratedPrompts(id);

  const [babble, setBabble] = useState<Babble | undefined>(undefined);
  const [babbleLoading, setBabbleLoading] = useState(!!id);
  const [isEditing, setIsEditing] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const autoGenerateTriggered = useRef(false);
  // Track the template used for the current generation so we can auto-save.
  const currentGenRef = useRef<{ templateId: string; templateName: string } | null>(null);

  // Fetch babble from API on mount.
  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    getBabble(id).then((result) => {
      if (!cancelled) {
        setBabble(result ?? undefined);
        setBabbleLoading(false);
      }
    }).catch(() => {
      if (!cancelled) {
        setBabbleLoading(false);
      }
    });
    return () => { cancelled = true; };
  }, [id, getBabble]);

  // Auto-generate prompt when navigated with ?autoGenerate=templateId
  useEffect(() => {
    const autoGenerateId = searchParams.get('autoGenerate');
    if (autoGenerateId && babble && !autoGenerateTriggered.current) {
      autoGenerateTriggered.current = true;
      const template = templates.find((t) => t.id === autoGenerateId);
      if (template) {
        currentGenRef.current = { templateId: template.id, templateName: template.name };
        void generate(babble.text, template.id).then(async (result) => {
          if (result?.name && babble.title.startsWith('Babble ')) {
            const updated = await updateBabble(babble.id, { title: result.name, text: babble.text });
            setBabble(updated);
          }
          // Auto-save the generated prompt.
          if (result?.text) {
            try {
              await createPrompt({
                templateId: template.id,
                templateName: template.name,
                promptText: result.text,
              });
            } catch {
              // Non-critical — user can still copy the text.
            }
          }
          currentGenRef.current = null;
        });
      }
      // Clear the query parameter so refresh doesn't re-trigger
      setSearchParams({}, { replace: true });
    }
  }, [searchParams, setSearchParams, babble, templates, generate, updateBabble, createPrompt]);

  const handleSave = useCallback(
    async (updated: Babble) => {
      try {
        const result = await updateBabble(updated.id, { title: updated.title, text: updated.text });
        setBabble(result);
        setIsEditing(false);
        toast.success('Babble updated');
      } catch {
        toast.error('Failed to update babble');
      }
    },
    [updateBabble],
  );

  const handleDelete = useCallback(async () => {
    if (babble) {
      try {
        await deleteBabble(babble.id);
        toast.success('Babble deleted');
        void navigate('/');
      } catch {
        toast.error('Failed to delete babble');
      }
    }
  }, [babble, deleteBabble, navigate]);

  const handleGenerate = useCallback(
    (options: PromptGenerateOptions) => {
      if (babble) {
        currentGenRef.current = { templateId: options.template.id, templateName: options.template.name };
        void generate(
          babble.text,
          options.template.id,
          options.promptFormat,
          options.allowEmojis,
        ).then(async (result) => {
          if (result?.name && babble.title.startsWith('Babble ')) {
            const updated = await updateBabble(babble.id, { title: result.name, text: babble.text });
            setBabble(updated);
          }
          // Auto-save the generated prompt.
          if (result?.text) {
            try {
              await createPrompt({
                templateId: options.template.id,
                templateName: options.template.name,
                promptText: result.text,
              });
            } catch {
              // Non-critical
            }
          }
          currentGenRef.current = null;
        });
      }
    },
    [babble, generate, updateBabble, createPrompt],
  );

  const handleRegenerate = useCallback(
    (prompt: GeneratedPrompt) => {
      if (!babble) return;
      const template = templates.find((t) => t.id === prompt.templateId);
      if (template) {
        handleGenerate({
          template,
          promptFormat: 'text',
          allowEmojis: false,
        });
      } else {
        toast.error('Template no longer available');
      }
    },
    [babble, templates, handleGenerate],
  );

  const handleDeletePrompt = useCallback(
    async (promptId: string) => {
      try {
        await deletePrompt(promptId);
        toast.success('Prompt deleted');
      } catch {
        toast.error('Failed to delete prompt');
      }
    },
    [deletePrompt],
  );

  if (babbleLoading) {
    return (
      <div className="flex flex-col items-center gap-4 py-12 text-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
        <p className="text-sm text-muted-foreground">Loading babble…</p>
      </div>
    );
  }

  if (!babble) {
    return (
      <div className="flex flex-col items-center gap-4 py-12 text-center">
        <h1 className="text-xl font-semibold">Babble not found</h1>
        <Button asChild variant="outline">
          <Link to="/">Go home</Link>
        </Button>
      </div>
    );
  }

  return (
    <AuthGuard message="Sign in with your organizational account to view babbles.">
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold">{babble.title}</h1>
          <p className="text-sm text-muted-foreground">
            {new Date(babble.updatedAt).toLocaleString()}
          </p>
        </div>
        <div className="flex gap-2">
          <Button asChild size="sm" variant="outline">
            <Link to="/record">
              <Mic className="size-4" />
              Record More
            </Link>
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => setIsEditing(true)}
          >
            <Edit className="size-4" />
            Edit
          </Button>
          <Button
            size="sm"
            variant="destructive"
            onClick={() => setDeleteDialogOpen(true)}
          >
            <Trash2 className="size-4" />
            Delete
          </Button>
        </div>
      </div>

      {isEditing ? (
        <BabbleEditor
          babble={babble}
          onSave={(updated) => void handleSave(updated)}
          onCancel={() => setIsEditing(false)}
        />
      ) : (
        <div className="rounded-lg border p-4">
          <p className="whitespace-pre-wrap text-sm leading-relaxed">
            {babble.text || 'No content.'}
          </p>
        </div>
      )}

      <Separator />

      <div className="space-y-4">
        <h2 className="text-lg font-semibold">Generate Prompt</h2>
        <PromptGenerator
          templates={templates}
          isGenerating={isGenerating}
          onGenerate={handleGenerate}
          disabled={!babble.text.trim()}
        />
        {genError && (
          <p className="text-sm text-destructive">{genError}</p>
        )}
        <PromptDisplay text={generatedText} isGenerating={isGenerating} />
        {generatedText && !isGenerating && (
          <CopyButton text={generatedText} />
        )}
      </div>

      <Separator />

      <PromptHistoryList
        prompts={prompts}
        loading={promptsLoading}
        error={promptsError}
        hasMore={promptsHasMore}
        onLoadMore={promptsLoadMore}
        onDelete={(promptId) => void handleDeletePrompt(promptId)}
        onRegenerate={handleRegenerate}
      />

      <DeleteBabbleDialog
        babble={babble}
        open={deleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        onConfirm={() => void handleDelete()}
      />
    </div>
    </AuthGuard>
  );
}
