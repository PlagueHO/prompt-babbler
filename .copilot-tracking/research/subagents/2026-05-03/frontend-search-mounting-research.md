# Frontend Search Component Mounting Research

## Research Questions

1. What is the full `SearchCommand.tsx` implementation?
1. Where in `App.tsx` should `SearchCommand` be mounted?
1. What is the `searchBabbles` API client function and expected endpoint/response?
1. What are the TypeScript types for search results?
1. Does a search trigger button exist in the Header?
1. What exact code changes are needed to mount `SearchCommand`?

## Findings

### 1. SearchCommand Component (Complete Code)

File: `prompt-babbler-app/src/components/search/SearchCommand.tsx`

```tsx
import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router';
import {
  CommandDialog,
  CommandInput,
  CommandList,
  CommandItem,
  CommandEmpty,
  CommandGroup,
} from '@/components/ui/command';
import { useSemanticSearch } from '@/hooks/useSemanticSearch';
import { Badge } from '@/components/ui/badge';
import { Loader2 } from 'lucide-react';

export function SearchCommand() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const { results, loading } = useSemanticSearch(query);
  const navigate = useNavigate();

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        setOpen((prev) => !prev);
      }
    };
    document.addEventListener('keydown', down);
    return () => document.removeEventListener('keydown', down);
  }, []);

  const handleSelect = (babbleId: string) => {
    setOpen(false);
    setQuery('');
    navigate(`/babble/${babbleId}`);
  };

  return (
    <CommandDialog open={open} onOpenChange={setOpen} shouldFilter={false}>
      <CommandInput
        placeholder="Search babbles..."
        value={query}
        onValueChange={setQuery}
      />
      <CommandList>
        {loading && (
          <div className="flex items-center justify-center py-6">
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
          </div>
        )}
        <CommandEmpty>
          {query.length < 2 ? 'Type to search...' : 'No results found.'}
        </CommandEmpty>
        {results.length > 0 && (
          <CommandGroup heading="Babbles">
            {results.map((result) => (
              <CommandItem
                key={result.id}
                value={result.id}
                onSelect={() => handleSelect(result.id)}
              >
                <div className="flex flex-col gap-1">
                  <span className="font-medium">{result.title}</span>
                  <span className="text-muted-foreground text-sm line-clamp-2">
                    {result.snippet}
                  </span>
                  {result.tags && result.tags.length > 0 && (
                    <div className="flex gap-1 mt-1">
                      {result.tags.slice(0, 3).map((tag) => (
                        <Badge key={tag} variant="secondary" className="text-xs">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  )}
                </div>
              </CommandItem>
            ))}
          </CommandGroup>
        )}
      </CommandList>
    </CommandDialog>
  );
}
```

**Key observations:**

- Uses `useNavigate` from `react-router` — MUST be mounted inside `<BrowserRouter>`.
- Listens for global `Ctrl+K` / `Cmd+K` keyboard shortcut to toggle open.
- Uses `useSemanticSearch` hook for debounced search with 300ms delay.
- On selection, navigates to `/babble/${babbleId}`.
- Renders a `CommandDialog` (modal overlay) — positioning is independent of DOM mount point.

### 2. App.tsx Component Tree

File: `prompt-babbler-app/src/App.tsx`

```tsx
function AppContent() {
  // ... access code gating ...
  return (
    <BrowserRouter>
      <BrowserCheck />
      <PageLayout>
        <Routes>...</Routes>
      </PageLayout>
      <ThemedToaster />
    </BrowserRouter>
  );
}

function App() {
  return (
    <ErrorBoundary>
      <ThemeProvider>
        <AppContent />
      </ThemeProvider>
    </ErrorBoundary>
  );
}
```

**Where to mount:**

`SearchCommand` uses `useNavigate()` so it MUST be inside `<BrowserRouter>`. It should be a sibling of `<PageLayout>` (or inside it), placed after `<ThemedToaster />` or right before it. Since it renders a portal dialog, exact sibling position doesn't affect layout.

**Recommended mount point:**

```tsx
<BrowserRouter>
  <BrowserCheck />
  <PageLayout>
    <Routes>...</Routes>
  </PageLayout>
  <SearchCommand />
  <ThemedToaster />
</BrowserRouter>
```

### 3. searchBabbles API Client Function

File: `prompt-babbler-app/src/services/api-client.ts` (line 355)

```typescript
export async function searchBabbles(
  query: string,
  topK: number = 10,
  signal?: AbortSignal,
  accessToken?: string,
): Promise<BabbleSearchResponse> {
  const params = new URLSearchParams();
  params.set('query', query);
  params.set('topK', String(topK));
  return fetchJson<BabbleSearchResponse>(
    `/api/babbles/search?${params.toString()}`,
    { signal },
    accessToken,
  );
}
```

**API endpoint:** `GET /api/babbles/search?query={query}&topK={topK}`

**Response type:** `BabbleSearchResponse`

### 4. TypeScript Types for Search Results

File: `prompt-babbler-app/src/types/index.ts`

```typescript
export interface BabbleSearchResultItem {
  id: string;
  title: string;
  snippet: string;
  tags?: string[];
  createdAt: string;
  isPinned: boolean;
  score: number;
}

export interface BabbleSearchResponse {
  results: BabbleSearchResultItem[];
}
```

### 5. Search Trigger Button in Header

File: `prompt-babbler-app/src/components/layout/Header.tsx`

**Finding: NO search trigger button exists in the Header.** The header contains:

- Brand link ("Prompt Babbler")
- Nav items: Home, New Babble, Templates
- `<UserMenu />` on the right

There is no search icon, button, or keyboard shortcut hint displayed in the Header. The `SearchCommand` currently relies solely on the `Ctrl+K` / `Cmd+K` keyboard shortcut for activation.

### 6. useSemanticSearch Hook

File: `prompt-babbler-app/src/hooks/useSemanticSearch.ts`

```typescript
import { useState, useEffect, useRef } from 'react';
import { searchBabbles } from '@/services/api-client';
import type { BabbleSearchResultItem } from '@/types';

export function useSemanticSearch(query: string, topK: number = 10) {
  const [results, setResults] = useState<BabbleSearchResultItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    if (query.trim().length < 2) {
      setResults([]);
      setLoading(false);
      return;
    }

    setLoading(true);

    const timeoutId = setTimeout(async () => {
      abortControllerRef.current?.abort();
      abortControllerRef.current = new AbortController();

      try {
        const response = await searchBabbles(query, topK, abortControllerRef.current.signal);
        setResults(response.results);
        setError(null);
      } catch (err) {
        if (err instanceof DOMException && err.name === 'AbortError') return;
        setError(err instanceof Error ? err.message : 'Search failed');
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, 300);

    return () => {
      clearTimeout(timeoutId);
      abortControllerRef.current?.abort();
    };
  }, [query, topK]);

  return { results, loading, error };
}
```

- 300ms debounce before firing API call.
- Minimum 2 characters required before searching.
- Cancels in-flight requests on new input (AbortController).

### 7. UI Command Component

File: `prompt-babbler-app/src/components/ui/command.tsx`

- Based on `cmdk` library (`CommandPrimitive`).
- Exports: `Command`, `CommandDialog`, `CommandInput`, `CommandList`, `CommandItem`, `CommandEmpty`, `CommandGroup` (and likely more).
- `CommandDialog` wraps in a `<Dialog>` from `@/components/ui/dialog` — renders as a modal overlay with portal.

## Exact Code Changes Needed to Mount SearchCommand

### Change 1: Mount in App.tsx

Add import and render `<SearchCommand />` inside `<BrowserRouter>`:

```tsx
// Add import at top of App.tsx
import { SearchCommand } from '@/components/search/SearchCommand';

// In AppContent, add <SearchCommand /> inside BrowserRouter
return (
  <BrowserRouter>
    <BrowserCheck />
    <PageLayout>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/record" element={<RecordPage />} />
        <Route path="/record/:babbleId" element={<RecordPage />} />
        <Route path="/babble/:id" element={<BabblePage />} />
        <Route path="/templates" element={<TemplatesPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Routes>
    </PageLayout>
    <SearchCommand />
    <ThemedToaster />
  </BrowserRouter>
);
```

### Change 2 (Optional): Add Search Trigger Button in Header

To improve discoverability, add a search button to the Header that shows the keyboard shortcut:

```tsx
// In Header.tsx, add import
import { Search } from 'lucide-react';
import { Button } from '@/components/ui/button';

// In the div.ml-auto section, add before <UserMenu />:
<Button
  variant="outline"
  size="sm"
  className="hidden sm:inline-flex items-center gap-2 text-muted-foreground"
  onClick={() => document.dispatchEvent(new KeyboardEvent('keydown', { key: 'k', ctrlKey: true }))}
>
  <Search className="size-4" />
  <kbd className="pointer-events-none text-xs">⌘K</kbd>
</Button>
```

**Note:** The dispatched keyboard event approach is fragile. A cleaner approach would be to lift `open` state or use a context/event bus so the Header button can directly control the dialog. Alternatively, expose a `setOpen` callback from `SearchCommand` via a ref or context.

## Summary

| Concern | Status |
|---------|--------|
| SearchCommand component | Complete, ready to mount |
| useSemanticSearch hook | Complete, calls `searchBabbles` |
| searchBabbles API client | Complete, calls `GET /api/babbles/search` |
| Types (BabbleSearchResponse, BabbleSearchResultItem) | Defined in `src/types/index.ts` |
| UI command primitives (`cmdk`) | Available at `@/components/ui/command` |
| Mounted in App.tsx | **NOT MOUNTED** — needs import + render |
| Search trigger in Header | **DOES NOT EXIST** — keyboard-only activation |

## Follow-on Questions

- Should the `SearchCommand` accept an `accessToken` prop (from auth context) to pass through to `searchBabbles`? Currently `useSemanticSearch` does not forward an access token.
- Should a visible search trigger button be added to the Header for discoverability?
- Does the backend `GET /api/babbles/search` endpoint already exist and function correctly?
