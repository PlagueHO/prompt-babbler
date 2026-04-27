# Frontend Live Search Component Design Research

## Research Topics and Questions

1. Current frontend structure (router, layout, components, services, hooks, types, pages, auth)
1. Existing search implementation (debouncing, API calls, components)
1. Component library inventory (shadcn/ui components installed, animation libraries)
1. Data fetching and design patterns
1. Evaluate search UI approaches (Command Palette, Inline Dropdown, Full-page)

---

## 1. Current Frontend Structure

### Router and Layout (`prompt-babbler-app/src/App.tsx`)

- **Router:** React Router v7 (`react-router`) with `BrowserRouter`
- **Routes:**
  - `/` — `HomePage`
  - `/record` — `RecordPage`
  - `/record/:babbleId` — `RecordPage` (edit mode)
  - `/babble/:id` — `BabblePage`
  - `/templates` — `TemplatesPage`
  - `/settings` — `SettingsPage`
- **Layout hierarchy:**

  ```
  App
  └── ErrorBoundary
      └── ThemeProvider
          └── AppContent (access code gate)
              └── BrowserRouter
                  ├── BrowserCheck
                  ├── PageLayout
                  │   ├── Header (nav + UserMenu)
                  │   └── <main> (route content)
                  └── ThemedToaster (sonner)
  ```

- **Access code gate:** `useAccessCode` hook checks `/api/config/access-status`. If access code required and not verified, shows `AccessCodeDialog`.

### Components Directory

```
components/
├── babbles/
│   ├── BabbleBubbles.tsx      — Grid of pinned/recent cards (top section)
│   ├── BabbleCard.tsx         — Card for BabbleList (unused?)
│   ├── BabbleEditor.tsx       — Babble editing form
│   ├── BabbleList.tsx         — Grid list with load-more button
│   ├── BabbleListItem.tsx     — Row item for list section
│   ├── BabbleListSection.tsx  — ** Contains search input + sort + infinite scroll **
│   └── DeleteBabbleDialog.tsx
├── layout/
│   ├── AccessCodeDialog.tsx
│   ├── AuthGuard.tsx
│   ├── BrowserCheck.tsx
│   ├── ErrorBoundary.tsx
│   ├── Header.tsx             — Top nav bar with logo, nav links, UserMenu
│   ├── PageLayout.tsx         — Header + <main> wrapper (max-w-5xl)
│   ├── StorageWarning.tsx
│   ├── ThemeContext.ts
│   ├── ThemeProvider.tsx
│   └── UserMenu.tsx           — Auth dropdown (sign in/out, settings)
├── prompts/
│   ├── CopyButton.tsx
│   ├── PromptDisplay.tsx
│   ├── PromptGenerator.tsx
│   ├── PromptHistoryCard.tsx
│   ├── PromptHistoryList.tsx
│   └── TemplatePicker.tsx
├── recording/
│   ├── ClearTranscriptDialog.tsx
│   ├── RecordButton.tsx
│   ├── RecordingIndicator.tsx
│   ├── TranscriptPreview.tsx
│   └── WaveformVisualizer.tsx
├── settings/
│   ├── LanguageSelector.tsx
│   └── ThemeSelector.tsx
├── templates/
│   ├── TemplateCard.tsx
│   ├── TemplateEditor.tsx
│   └── TemplateList.tsx
└── ui/ (shadcn/ui primitives)
    ├── alert-dialog.tsx
    ├── badge.tsx
    ├── button.tsx
    ├── card.tsx
    ├── checkbox.tsx
    ├── dialog.tsx
    ├── dropdown-menu.tsx
    ├── error-banner.tsx
    ├── input.tsx
    ├── label.tsx
    ├── scroll-area.tsx
    ├── select.tsx
    ├── separator.tsx
    ├── skeleton.tsx
    ├── tag-input.tsx
    ├── tag-list.tsx
    └── textarea.tsx
```

### Services

```
services/
├── api-client.ts            — All API calls (babbles, templates, prompts, user)
├── default-templates.ts     — Default prompt template definitions
├── local-storage.ts         — Local storage helpers
├── migration.ts             — Local→server babble migration
└── transcription-stream.ts  — Speech-to-text streaming
```

### Hooks

```
hooks/
├── useAccessCode.ts       — Access code verification
├── useAudioRecording.ts   — Microphone recording
├── useAuthToken.ts        — MSAL auth token acquisition
├── useBabbles.ts          — ** Core data hook: fetch, search, sort, paginate, pin **
├── useGeneratedPrompts.ts — Prompt generation history
├── useLocalStorage.ts     — Local storage state hook
├── usePromptGeneration.ts — Prompt generation streaming
├── useSettings.ts         — App settings
├── useTemplates.ts        — Template CRUD
├── useTheme.ts            — Theme management
├── useTranscription.ts    — Real-time transcription
└── useUserSettings.ts     — User profile settings
```

### Types (`prompt-babbler-app/src/types/index.ts`)

```typescript
interface Babble {
  id: string;
  title: string;
  text: string;
  tags?: string[];
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
}

interface PagedResponse<T> {
  items: T[];
  continuationToken: string | null;
}

interface PromptTemplate { id, name, description, instructions, ... }
interface GeneratedPrompt { id, babbleId, templateId, templateName, promptText, generatedAt }
interface StatusResponse { status: string }
interface AccessControlStatus { accessCodeRequired: boolean }
interface UserProfile { id, displayName, email, settings, createdAt, updatedAt }
interface UserSettings { theme: ThemeMode, speechLanguage: string }
type PromptFormat = 'text' | 'markdown'
type ThemeMode = 'light' | 'dark' | 'system'
```

### Auth (`prompt-babbler-app/src/auth/authConfig.ts`)

- Uses `@azure/msal-browser` and `@azure/msal-react`
- Auth is optional — `isAuthConfigured` flag based on Vite build-time env var `__MSAL_CLIENT_ID__`
- Login scopes: `api://prompt-babbler-api/access_as_user`
- Anonymous mode supported (access-code-only or fully open)

### Pages

| Page | File | Purpose |
|------|------|---------|
| Home | `HomePage.tsx` | Babble bubbles + searchable list |
| Record | `RecordPage.tsx` | Audio recording + transcription |
| Babble | `BabblePage.tsx` | Individual babble view + prompt gen |
| Templates | `TemplatesPage.tsx` | Template management |
| Settings | `SettingsPage.tsx` | User preferences |

---

## 2. Existing Search Implementation

### Current Search Component: `BabbleListSection.tsx`

- **Location:** Inline within the "Older Babbles" list section on the HomePage
- **Input:** Standard `<Input type="search">` with search icon, placeholder "Filter babbles…"
- **Debouncing:** Manual `setTimeout`-based debounce (300ms) using `useRef` for timer
- **No dropdown/overlay** — results appear in the same list below the input
- **Sort controls:** Dropdown for sort-by (Date Created / Title) and direction (Asc / Desc)
- **Infinite scroll:** `IntersectionObserver` on a sentinel div for pagination

### API Call: `getBabbles` in `api-client.ts`

```typescript
interface GetBabblesOptions {
  continuationToken?: string | null;
  pageSize?: number;
  search?: string;
  sortBy?: 'createdAt' | 'title';
  sortDirection?: 'desc' | 'asc';
  isPinned?: boolean;
}

// Calls: GET /api/babbles?search=...&sortBy=...&sortDirection=...&pageSize=...&continuationToken=...
async function getBabbles(options, accessToken?): Promise<PagedResponse<Babble>>
```

- Server-side search via `search` query parameter
- Continuation-token-based pagination
- Returns `{ items: Babble[], continuationToken: string | null }`

### Data Flow

```
HomePage
  └── useBabbles() hook
        ├── [search, setSearch] state
        ├── [sortBy, setSortBy] state
        ├── [sortDirection, setSortDirection] state
        ├── fetchList(search, sortBy, sortDir, append?, token?)
        │     └── api.getBabbles({ search, sortBy, sortDirection, pageSize: 20, continuationToken })
        ├── useEffect on [search, sortBy, sortDirection] → fetchList(...)
        └── loadMore → fetchList(..., true, continuationToken)
  └── BabbleListSection
        ├── debounced <Input> → onSearchChange(value) → setSearch(value)
        ├── sort dropdown → onSortByChange / onSortDirectionChange
        ├── BabbleListItem[] — renders results
        └── IntersectionObserver sentinel — triggers loadMore
```

---

## 3. Component Library Inventory

### shadcn/ui Configuration (`components.json`)

- **Style:** new-york
- **RSC:** false
- **TSX:** true
- **Base color:** neutral
- **CSS variables:** enabled
- **Icon library:** lucide
- **Aliases:** `@/components`, `@/components/ui`, `@/lib`, `@/hooks`

### Installed shadcn/ui Components

| Component | File | Notes |
|-----------|------|-------|
| AlertDialog | `alert-dialog.tsx` | Confirmation dialogs |
| Badge | `badge.tsx` | Tags display |
| Button | `button.tsx` | Primary action component |
| Card | `card.tsx` | Babble cards |
| Checkbox | `checkbox.tsx` | Form inputs |
| Dialog | `dialog.tsx` | Modal dialogs |
| DropdownMenu | `dropdown-menu.tsx` | Sort controls, user menu |
| ErrorBanner | `error-banner.tsx` | Custom error display |
| Input | `input.tsx` | Text inputs |
| Label | `label.tsx` | Form labels |
| ScrollArea | `scroll-area.tsx` | Scrollable containers |
| Select | `select.tsx` | Select dropdowns |
| Separator | `separator.tsx` | Visual dividers |
| Skeleton | `skeleton.tsx` | Loading placeholders |
| TagInput | `tag-input.tsx` | Custom tag input |
| TagList | `tag-list.tsx` | Custom tag display |
| Textarea | `textarea.tsx` | Multi-line input |

### NOT Installed (would need to add)

- **Command (cmdk)** — Not present. Would need `npx shadcn@latest add command`
- **Popover** — Not present. Would need `npx shadcn@latest add popover`
- **Tooltip** — Not present
- **NavigationMenu** — Not present

### Dependencies (from `package.json`)

| Category | Library | Version |
|----------|---------|---------|
| UI Framework | React 19.2, React DOM 19.2 | Latest |
| Routing | react-router 7.13 | v7 |
| Styling | Tailwind CSS 4.2, tw-animate-css | Latest |
| Component lib | radix-ui 1.4, shadcn 4.0 | Latest |
| Icons | lucide-react 1.6 | Latest |
| Forms | react-hook-form 7.73, @hookform/resolvers, zod 4.3 | Latest |
| CSS utils | class-variance-authority, clsx, tailwind-merge | Standard |
| Auth | @azure/msal-browser 4.30, @azure/msal-react 3.0 | Latest |
| Telemetry | @opentelemetry/* suite | Full OTEL stack |
| Toast | sonner 2.0 | Latest |
| Animation | **tw-animate-css** (Tailwind plugin) | No framer-motion |

**Key finding:** No framer-motion. Animations are Tailwind CSS-based via `tw-animate-css`.

---

## 4. Design Patterns

### Data Fetching

- **Pattern:** Custom hooks with `useState` + `useCallback` + `useEffect`
- **No React Query/SWR/TanStack Query** — all data fetching is manual
- **Auth token:** obtained via `useAuthToken()` → `getAuthTokenRef.current()`
- **Pagination:** Continuation-token-based (Cosmos DB style)
- **Loading states:** Separate `loading`, `loadingMore` booleans
- **Error handling:** `try/catch` → `setError(err.message)`

### Error Handling Pattern

```typescript
try {
  setLoading(true);
  setError(null);
  const data = await api.someCall(authToken);
  setData(data);
} catch (err) {
  setError(err instanceof Error ? err.message : 'Failed to ...');
} finally {
  setLoading(false);
}
```

Displayed via `<ErrorBanner error={error} onRetry={refresh} />`.

### Loading State Pattern

- Spinner: `<Loader2 className="size-5 animate-spin text-muted-foreground" />`
- Skeleton placeholders available but not widely used
- Full-page loading: centered spinner div

### Header Structure

```
<header className="border-b bg-background">
  <div className="mx-auto flex h-14 max-w-5xl items-center gap-6 px-4">
    ├── Logo link ("Prompt Babbler")
    ├── <nav> with NavLink items (Home, Record, Templates)
    └── <div className="ml-auto"> with UserMenu
  </div>
</header>
```

**Open slot:** Between the nav and `ml-auto` UserMenu area — natural place for a search trigger button.

---

## 5. Search UI Approach Evaluation

### Option A: Command Palette (cmdk) — RECOMMENDED

**Description:** A `Cmd+K` / `Ctrl+K` keyboard-triggered overlay dialog (like VS Code, GitHub, Linear) that provides a global search experience.

**Pros:**

- Modern, familiar UX pattern (developers love it)
- Global — accessible from any page, not just HomePage
- Doesn't disrupt current page layout
- Keyboard-first with mouse support
- shadcn/ui has a `Command` component wrapping `cmdk` — well-maintained
- Can show babble results with preview text, tags, and timestamps
- Easy to extend later (search templates, navigate pages, etc.)
- Accessibility: built-in keyboard navigation, ARIA roles

**Cons:**

- Requires adding 2 new shadcn/ui components (`command`, `dialog` — dialog already exists)
- Users need to discover the shortcut (mitigated by a search button in header)
- Slightly more complex implementation than inline search

**Implementation outline:**

1. Add shadcn/ui `command` component (`npx shadcn@latest add command`)
1. Create `SearchCommand.tsx` component using `CommandDialog` (wraps Dialog + Command)
1. Add a search trigger button in `Header.tsx` (search icon + `Ctrl+K` hint)
1. Register `Ctrl+K` / `Cmd+K` keyboard shortcut globally
1. On input change (debounced 300ms), call `api.getBabbles({ search, pageSize: 10 })`
1. Display results as `CommandItem` entries — clicking navigates to `/babble/:id`
1. Show empty state, loading spinner, and "no results" appropriately

**Complexity:** Medium — mostly composition of existing patterns

### Option B: Inline Search with Dropdown

**Description:** A search input in the header that shows a floating dropdown with results.

**Pros:**

- Always visible, discoverable
- Traditional web search pattern
- Can reuse existing `Input` component

**Cons:**

- Requires `Popover` component (not installed)
- More complex positioning/z-index management
- Takes up header space permanently
- Dropdown needs careful scroll handling
- Less elegant than command palette for this type of app
- Not keyboard-shortcut-friendly by default

**Complexity:** Medium-High

### Option C: Full-page Search

**Description:** Dedicated `/search` route with results page.

**Pros:**

- Simple to implement
- Plenty of space for results
- URL-based, sharable search queries

**Cons:**

- Navigation overhead (leaves current page)
- Feels dated for a modern SPA
- Already have search in BabbleListSection — would overlap
- Poor discovery from other pages

**Complexity:** Low but poorest UX

### Recommendation: Option A — Command Palette

**Rationale:**

1. **Fits the app identity:** Prompt Babbler targets developers/power users. Command palette is their native interface pattern.
1. **Global access:** Works from any page (Record, Templates, Babble detail), unlike the current search which is HomePage-only.
1. **Non-disruptive:** Doesn't change existing UI — additive overlay. Current BabbleListSection search stays intact for in-page filtering.
1. **Extensible:** Can later add template search, page navigation, quick actions.
1. **Low risk:** shadcn/ui `Command` component is well-tested, accessible, and follows the existing component patterns.
1. **Existing patterns match:** The existing `Dialog` component is already installed. The debounce pattern already exists in `BabbleListSection.tsx`. The API already supports `search` parameter.

---

## 6. Draft Component Structure

### New Files to Create

```
components/
└── search/
    └── SearchCommand.tsx     — Main command palette component
hooks/
└── useSearch.ts              — Search hook with debounce + API call
```

### New shadcn/ui Components to Install

```bash
npx shadcn@latest add command
# This will also add cmdk as a dependency
# Dialog is already installed
```

### `SearchCommand.tsx` — Draft Structure

```tsx
// Uses: CommandDialog from shadcn/ui command
// Props: open, onOpenChange
// Internal state: query (string), results (Babble[]), loading (boolean)
// On query change (debounced 300ms): call api.getBabbles({ search: query, pageSize: 10 })
// Renders: CommandInput, CommandList, CommandEmpty, CommandGroup, CommandItem
// CommandItem click: navigate to /babble/:id, close dialog

import { useState, useEffect, useCallback, useRef } from 'react';
import { useNavigate } from 'react-router';
import { Search } from 'lucide-react';
import {
  CommandDialog,
  CommandInput,
  CommandList,
  CommandEmpty,
  CommandGroup,
  CommandItem,
} from '@/components/ui/command';
import { useSearch } from '@/hooks/useSearch';

interface SearchCommandProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function SearchCommand({ open, onOpenChange }: SearchCommandProps) {
  const navigate = useNavigate();
  const { query, setQuery, results, loading } = useSearch();

  const handleSelect = (babbleId: string) => {
    onOpenChange(false);
    navigate(`/babble/${babbleId}`);
  };

  return (
    <CommandDialog open={open} onOpenChange={onOpenChange}>
      <CommandInput
        placeholder="Search babbles..."
        value={query}
        onValueChange={setQuery}
      />
      <CommandList>
        {loading && <div>Loading...</div>}
        <CommandEmpty>No babbles found.</CommandEmpty>
        <CommandGroup heading="Babbles">
          {results.map((babble) => (
            <CommandItem key={babble.id} onSelect={() => handleSelect(babble.id)}>
              <div>
                <span className="font-medium">{babble.title}</span>
                <span className="text-xs text-muted-foreground ml-2">
                  {new Date(babble.createdAt).toLocaleDateString()}
                </span>
              </div>
            </CommandItem>
          ))}
        </CommandGroup>
      </CommandList>
    </CommandDialog>
  );
}
```

### `useSearch.ts` — Draft Structure

```tsx
// Debounced search hook — reuses existing api.getBabbles({ search })
// Returns: { query, setQuery, results, loading, error }
// Debounce: 300ms (matching existing pattern)
// Auto-fetches on query change
// Clears results when query is empty

import { useState, useEffect, useRef } from 'react';
import type { Babble } from '@/types';
import * as api from '@/services/api-client';
import { useAuthToken } from '@/hooks/useAuthToken';

export function useSearch() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<Babble[]>([]);
  const [loading, setLoading] = useState(false);
  const getAuthToken = useAuthToken();
  const debounceRef = useRef<ReturnType<typeof setTimeout>>();

  useEffect(() => {
    if (!query.trim()) {
      setResults([]);
      return;
    }

    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(async () => {
      setLoading(true);
      try {
        const token = await getAuthToken();
        const data = await api.getBabbles({ search: query, pageSize: 10 }, token);
        setResults(data.items);
      } catch {
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, 300);

    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [query, getAuthToken]);

  return { query, setQuery, results, loading };
}
```

### Header Integration — Draft

Add a search button between nav and UserMenu in `Header.tsx`:

```tsx
// In Header.tsx, add:
const [searchOpen, setSearchOpen] = useState(false);

// Keyboard shortcut registration
useEffect(() => {
  const handler = (e: KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
      e.preventDefault();
      setSearchOpen(true);
    }
  };
  document.addEventListener('keydown', handler);
  return () => document.removeEventListener('keydown', handler);
}, []);

// Button in header (between nav and ml-auto div):
<Button variant="outline" size="sm" onClick={() => setSearchOpen(true)}
  className="hidden sm:inline-flex gap-2 text-muted-foreground">
  <Search className="size-4" />
  <span>Search...</span>
  <kbd className="text-[10px] bg-muted px-1.5 rounded">⌘K</kbd>
</Button>

// Dialog component:
<SearchCommand open={searchOpen} onOpenChange={setSearchOpen} />
```

---

## 7. API Integration Summary

### Existing API Endpoint

- **Endpoint:** `GET /api/babbles?search={query}&pageSize={n}`
- **Auth:** Optional Bearer token (via `useAuthToken`)
- **Response:** `{ items: Babble[], continuationToken: string | null }`
- **Search is server-side** — backend handles text matching

### Key API Client Functions

| Function | File | Signature |
|----------|------|-----------|
| `getBabbles` | `prompt-babbler-app/src/services/api-client.ts` | `(options: GetBabblesOptions, accessToken?) => Promise<PagedResponse<Babble>>` |
| `getBabble` | same | `(id: string, accessToken?) => Promise<Babble>` |

### No New API Endpoints Required

The existing `getBabbles` with `search` parameter is sufficient for the command palette search.

---

## Follow-on Questions (Discovered During Research)

1. Does the backend search support fuzzy/semantic matching, or is it exact substring? This affects whether client-side result highlighting is feasible.
1. Should the command palette also search templates (for a unified search experience)?
1. Should search results show babble text preview, or only title + date + tags?

---

## Clarifying Questions

None — all research topics were answerable through codebase inspection.
