import { useState, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router';
import { toast } from 'sonner';
import { Edit, Trash2, Mic } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { BabbleEditor } from '@/components/babbles/BabbleEditor';
import { DeleteBabbleDialog } from '@/components/babbles/DeleteBabbleDialog';
import { PromptGenerator } from '@/components/prompts/PromptGenerator';
import { PromptDisplay } from '@/components/prompts/PromptDisplay';
import { CopyButton } from '@/components/prompts/CopyButton';
import { useBabbles } from '@/hooks/useBabbles';
import { useTemplates } from '@/hooks/useTemplates';
import { usePromptGeneration } from '@/hooks/usePromptGeneration';
import { getBabble } from '@/services/local-storage';
import type { Babble, PromptTemplate } from '@/types';

export function BabblePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { updateBabble, deleteBabble } = useBabbles();
  const { templates } = useTemplates();
  const { generatedText, isGenerating, error: genError, generate } = usePromptGeneration();

  const [babble, setBabble] = useState<Babble | undefined>(() =>
    id ? getBabble(id) : undefined
  );
  const [isEditing, setIsEditing] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  const handleSave = useCallback(
    (updated: Babble) => {
      updateBabble(updated);
      setBabble(updated);
      setIsEditing(false);
      toast.success('Babble updated');
    },
    [updateBabble]
  );

  const handleDelete = useCallback(() => {
    if (babble) {
      deleteBabble(babble.id);
      toast.success('Babble deleted');
      void navigate('/');
    }
  }, [babble, deleteBabble, navigate]);

  const handleGenerate = useCallback(
    (template: PromptTemplate) => {
      if (babble) {
        void generate(babble.text, template.systemPrompt);
      }
    },
    [babble, generate]
  );

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
          onSave={handleSave}
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

      <DeleteBabbleDialog
        babble={babble}
        open={deleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        onConfirm={handleDelete}
      />
    </div>
  );
}
