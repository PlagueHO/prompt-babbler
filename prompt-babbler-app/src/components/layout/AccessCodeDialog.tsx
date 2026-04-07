import { useState, type FormEvent, type KeyboardEvent } from 'react';
import { Lock } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';

interface AccessCodeDialogProps {
  open: boolean;
  onSubmit: (code: string) => void;
  isLoading: boolean;
  error: string | null;
}

export function AccessCodeDialog({ open, onSubmit, isLoading, error }: AccessCodeDialogProps) {
  const [code, setCode] = useState('');

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (code.trim()) {
      onSubmit(code.trim());
    }
  }

  function handleKeyDown(e: KeyboardEvent) {
    if (e.key === 'Enter' && code.trim() && !isLoading) {
      e.preventDefault();
      onSubmit(code.trim());
    }
  }

  return (
    <Dialog open={open}>
      <DialogContent
        showCloseButton={false}
        onPointerDownOutside={(e) => e.preventDefault()}
        onEscapeKeyDown={(e) => e.preventDefault()}
      >
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Lock className="size-5" />
              Access Code Required
            </DialogTitle>
            <DialogDescription>
              Enter the access code to use this application.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Label htmlFor="access-code">Access Code</Label>
            <Input
              id="access-code"
              type="password"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Enter access code"
              disabled={isLoading}
              autoFocus
              className="mt-2"
            />
            {error && (
              <p className="text-sm text-destructive mt-2" role="alert">
                {error}
              </p>
            )}
          </div>
          <DialogFooter>
            <Button type="submit" disabled={!code.trim() || isLoading}>
              {isLoading ? 'Verifying...' : 'Submit'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
