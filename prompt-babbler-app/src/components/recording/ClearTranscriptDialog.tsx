import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import { Button } from '@/components/ui/button';
import { Trash2 } from 'lucide-react';

interface ClearTranscriptDialogProps {
  isAppendMode: boolean;
  disabled: boolean;
  onConfirm: () => void;
}

export function ClearTranscriptDialog({
  isAppendMode,
  disabled,
  onConfirm,
}: ClearTranscriptDialogProps) {
  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant="ghost" disabled={disabled}>
          <Trash2 className="size-4" />
          Clear
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Clear transcript?</AlertDialogTitle>
          <AlertDialogDescription>
            {isAppendMode
              ? 'This will clear the new transcript. The existing babble text will not be affected.'
              : 'This will clear the entire transcript. This action cannot be undone.'}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm}>Clear</AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
