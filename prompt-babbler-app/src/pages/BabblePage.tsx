import { useState, useCallback, useEffect, useRef } from 'react';
import { useParams, useNavigate, useSearchParams, Link } from 'react-router';
import { toast } from 'sonner';
import { Pencil, Trash2, Mic, Loader2, Sparkles, Check, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ErrorBanner } from '@/components/ui/error-banner';
import { Separator } from '@/components/ui/separator';
import { TagList } from '@/components/ui/tag-list';
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
import { useAuthToken } from '@/hooks/useAuthToken';
import * as api from '@/services/api-client';
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
    deletePrompt,
    refresh: refreshPrompts,
  } = useGeneratedPrompts(id);
  const getAuthToken = useAuthToken();

  const [babble, setBabble] = useState<Babble | undefined>(undefined);
  const [babbleLoading, setBabbleLoading] = useState(!!id);
  const [isEditing, setIsEditing] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [isGeneratingTitle, setIsGeneratingTitle] = useState(false);
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const [titleInputValue, setTitleInputValue] = useState('');
  const autoGenerateTriggered = useRef(false);

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
        void generate(babble.id, template.id).then(() => {
          void refreshPrompts();
        });
      }
      // Clear the query parameter so refresh doesn't re-trigger
      setSearchParams({}, { replace: true });
    }
  }, [searchParams, setSearchParams, babble, templates, generate, refreshPrompts]);

  const handleSave = useCallback(
    async (updated: Babble) => {
      try {
        const result = await updateBabble(updated.id, { title: updated.title, text: updated.text, tags: updated.tags });
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
        void generate(
          babble.id,
          options.template.id,
          options.promptFormat,
          options.allowEmojis,
        ).then(() => {
          void refreshPrompts();
        });
      }
    },
    [babble, generate, refreshPrompts],
  );

  const handleSaveTitle = useCallback(async () => {
    if (!babble) return;
    try {
      const result = await updateBabble(babble.id, { title: titleInputValue.trim() || babble.title, text: babble.text, tags: babble.tags });
      setBabble(result);
      setIsEditingTitle(false);
      toast.success('Title updated');
    } catch {
      toast.error('Failed to update title');
    }
  }, [babble, titleInputValue, updateBabble]);

  const handleCancelTitle = useCallback(() => {
    setIsEditingTitle(false);
  }, []);

  const handleGenerateTitle = useCallback(async () => {
    if (!babble) return;
    try {
      setIsGeneratingTitle(true);
      const token = await getAuthToken();
      const updated = await api.generateTitle(babble.id, token);
      setBabble(updated);
      toast.success('Title generated');
    } catch {
      toast.error('Failed to generate title');
    } finally {
      setIsGeneratingTitle(false);
    }
  }, [babble, getAuthToken]);

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
          <div className="flex items-center gap-2">
            {isEditingTitle ? (
              <>
                <Input
                  value={titleInputValue}
                  onChange={(e) => setTitleInputValue(e.target.value)}
                  className="text-2xl font-bold h-auto py-1"
                  autoFocus
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') void handleSaveTitle();
                    if (e.key === 'Escape') handleCancelTitle();
                  }}
                />
                <Button
                  size="icon"
                  variant="ghost"
                  className="size-7"
                  onClick={() => void handleSaveTitle()}
                  title="Save title"
                >
                  <Check className="size-4" />
                </Button>
                <Button
                  size="icon"
                  variant="ghost"
                  className="size-7"
                  onClick={handleCancelTitle}
                  title="Cancel editing title"
                >
                  <X className="size-4" />
                </Button>
              </>
            ) : (
              <>
                <h1 className="text-2xl font-bold">{babble.title}</h1>
                <Button
                  size="icon"
                  variant="ghost"
                  className="size-7"
                  onClick={() => { setTitleInputValue(babble.title); setIsEditingTitle(true); }}
                  title="Edit title"
                >
                  <Pencil className="size-4" />
                </Button>
                <Button
                  size="icon"
                  variant="ghost"
                  className="size-7"
                  onClick={() => void handleGenerateTitle()}
                  disabled={isGeneratingTitle || !babble.text.trim()}
                  title="Generate title from babble text"
                >
                  {isGeneratingTitle ? (
                    <Loader2 className="size-4 animate-spin" />
                  ) : (
                    <Sparkles className="size-4" />
                  )}
                </Button>
              </>
            )}
          </div>
          <div className="flex items-center gap-2 mt-2">
            <TagList tags={babble.tags} className="flex-1" />
            <p className="shrink-0 text-sm text-muted-foreground">
              {new Date(babble.updatedAt).toLocaleString()}
            </p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button asChild size="sm">
            <Link to={`/record/${babble.id}`}>
              <Mic className="size-4" />
              Continue Babble
            </Link>
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => setIsEditing(true)}
          >
            <Pencil className="size-4" />
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
        <div className="max-h-[50vh] overflow-y-auto rounded-lg border p-4">
          <p className="whitespace-pre-wrap text-sm leading-relaxed">
            {babble.text || 'No content.'}
          </p>
        </div>
      )}

      <Separator />

      <div className="space-y-4">
        <h2 className="text-lg font-semibold">Generate Prompt</h2>
        <div className="flex flex-wrap items-center justify-between gap-3">
          <PromptGenerator
            templates={templates}
            isGenerating={isGenerating}
            onGenerate={handleGenerate}
            disabled={!babble.text.trim()}
          />
          {generatedText && !isGenerating && (
            <CopyButton text={generatedText} />
          )}
        </div>
        {genError && <ErrorBanner error={genError} />}
        <PromptDisplay text={generatedText} isGenerating={isGenerating} />
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
