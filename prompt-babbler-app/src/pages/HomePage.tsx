import { Link } from 'react-router';
import { Plus, Mic } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { BabbleList } from '@/components/babbles/BabbleList';
import { StorageWarning } from '@/components/layout/StorageWarning';
import { AuthGuard } from '@/components/layout/AuthGuard';
import { useBabbles } from '@/hooks/useBabbles';

export function HomePage() {
  const { babbles } = useBabbles();

  return (
    <AuthGuard message="Sign in with your organizational account to record babbles and generate prompts.">
      <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Your Babbles</h1>
          <p className="text-sm text-muted-foreground">
            Record your thoughts and turn them into polished prompts.
          </p>
        </div>
        <div className="flex gap-2">
          <Button asChild>
            <Link to="/record">
              <Mic className="size-4" />
              New Babble
            </Link>
          </Button>
        </div>
      </div>

      <StorageWarning />

      {babbles.length === 0 ? (
        <div className="flex flex-col items-center gap-4 rounded-lg border border-dashed p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Mic className="size-8 text-muted-foreground" />
          </div>
          <div>
            <h2 className="text-lg font-semibold">No babbles yet</h2>
            <p className="text-sm text-muted-foreground">
              Start by recording your first stream-of-consciousness babble.
            </p>
          </div>
          <Button asChild>
            <Link to="/record">
              <Plus className="size-4" />
              Record your first babble
            </Link>
          </Button>
        </div>
      ) : (
        <BabbleList babbles={babbles} />
      )}
      </div>
    </AuthGuard>
  );
}
