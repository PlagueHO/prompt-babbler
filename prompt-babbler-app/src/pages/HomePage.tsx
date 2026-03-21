import { Link } from 'react-router';
import { Plus, Mic, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ErrorBanner } from '@/components/ui/error-banner';
import { BabbleBubbles } from '@/components/babbles/BabbleBubbles';
import { BabbleListSection } from '@/components/babbles/BabbleListSection';
import { AuthGuard } from '@/components/layout/AuthGuard';
import { useBabbles } from '@/hooks/useBabbles';

export function HomePage() {
  const {
    bubbleBabbles,
    bubblesLoading,
    listBabbles,
    listLoading,
    loadingMore,
    loadMore,
    search,
    setSearch,
    sortBy,
    setSortBy,
    sortDirection,
    setSortDirection,
    loading,
    error,
    totalBabbles,
    togglePin,
    refresh,
  } = useBabbles();

  const showEmpty = !loading && totalBabbles === 0 && !error;

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

        {error && <ErrorBanner error={error} onRetry={() => void refresh()} />}

        {loading && totalBabbles === 0 ? (
          <div className="flex flex-col items-center gap-4 py-12 text-center">
            <Loader2 className="size-8 animate-spin text-muted-foreground" />
            <p className="text-sm text-muted-foreground">Loading babbles…</p>
          </div>
        ) : showEmpty ? (
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
          <>
            {!bubblesLoading && bubbleBabbles.length > 0 && (
              <BabbleBubbles babbles={bubbleBabbles} onTogglePin={togglePin} />
            )}
            {(listBabbles.length > 0 || search !== '') && (
              <BabbleListSection
                babbles={listBabbles}
                search={search}
                onSearchChange={setSearch}
                sortBy={sortBy}
                onSortByChange={setSortBy}
                sortDirection={sortDirection}
                onSortDirectionChange={setSortDirection}
                loadMore={loadMore}
                loadingMore={loadingMore}
                loading={listLoading}
                onTogglePin={togglePin}
              />
            )}
          </>
        )}
      </div>
    </AuthGuard>
  );
}
