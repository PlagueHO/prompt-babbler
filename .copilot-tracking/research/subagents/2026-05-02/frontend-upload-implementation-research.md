# Frontend MP3 Upload Implementation Research

## Research Topics

1. HomePage component layout and action button area
1. API client architecture and how to add multipart/form-data upload
1. Type definitions
1. Hooks pattern for a new file upload/transcription hook
1. RecordPage babble creation flow
1. Router configuration
1. UI library stack
1. Existing file input patterns

---

## 1. HomePage Component

**File:** `prompt-babbler-app/src/pages/HomePage.tsx` (98 lines)

### Header/Action Buttons Area (Lines 37–51)

```tsx
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
```

### Key Observations

- The `<div className="flex gap-2">` wrapper already supports multiple action buttons side-by-side.
- An upload button can be added as a sibling `<Button>` inside this flex container.
- Icons come from `lucide-react` — `Upload` icon is available from lucide.
- Navigation uses React Router `<Link>` wrapped in a shadcn `<Button asChild>`.
- The `Plus` icon is imported but only used in the empty state CTA (line 72); `Mic` is the primary "New Babble" icon.

### Upload Button Placement

Add after the existing "New Babble" button, inside `<div className="flex gap-2">`:

```tsx
<div className="flex gap-2">
  <Button asChild>
    <Link to="/record">
      <Mic className="size-4" />
      New Babble
    </Link>
  </Button>
  {/* New upload button goes here */}
  <Button variant="outline" onClick={...}>
    <Upload className="size-4" />
    Upload Audio
  </Button>
</div>
```

---

## 2. API Client

**File:** `prompt-babbler-app/src/services/api-client.ts` (372 lines)

### Architecture

- **Base URL:** Injected at build/dev time via Vite global `__API_BASE_URL__` (from Aspire service discovery).
- **Core helper:** `fetchJson<T>(path, init?, accessToken?)` — handles JSON requests, error handling, backend unavailability detection.
- **Headers:** Built via `buildHeaders(contentType?, accessToken?)` which sets:
  - `Content-Type` (typically `application/json`)
  - `Authorization: Bearer {token}` (if auth configured)
  - `X-Access-Code` (if access code mode active)
- **Module-scoped state:** `currentAccessCode` for access-code bypass mode.

### Authentication Token Pattern

All public API functions accept an optional `accessToken?: string` parameter as their last argument. The calling hooks acquire the token via `useAuthToken()` which uses MSAL (`@azure/msal-react`) or returns undefined in anonymous mode.

### How to Add Multipart/Form-Data Upload

The existing `fetchJson` helper always sets `Content-Type: application/json`. For file uploads, a new helper or direct `fetch` call is needed. Pattern:

```typescript
export async function uploadAudioFile(
  file: File,
  accessToken?: string,
): Promise<Babble> {
  const base = getApiBaseUrl();
  const formData = new FormData();
  formData.append('file', file);

  const headers: Record<string, string> = {};
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }
  if (currentAccessCode) {
    headers['X-Access-Code'] = currentAccessCode;
  }
  // Do NOT set Content-Type — browser sets it with boundary for multipart

  let res: Response;
  try {
    res = await fetch(`${base}/api/babbles/upload`, {
      method: 'POST',
      headers,
      body: formData,
    });
  } catch {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (isHtmlResponse(res)) {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`Upload error ${res.status}: ${text}`);
  }
  return res.json() as Promise<Babble>;
}
```

**Critical:** Do NOT set `Content-Type` header when sending `FormData` — the browser auto-generates the `multipart/form-data` boundary.

### Existing API Functions (for reference)

| Function | Method | Path |
|----------|--------|------|
| `createBabble` | POST | `/api/babbles` |
| `getBabble` | GET | `/api/babbles/:id` |
| `updateBabble` | PUT | `/api/babbles/:id` |
| `deleteBabble` | DELETE | `/api/babbles/:id` |
| `generatePrompt` | POST | `/api/babbles/:id/generate` |
| `generateTitle` | POST | `/api/babbles/:id/generate-title` |
| `searchBabbles` | GET | `/api/babbles/search` |

---

## 3. Type Definitions

**File:** `prompt-babbler-app/src/types/index.ts` (90 lines)

### Core Types

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
```

No file/audio related types exist. A new upload feature may need:

```typescript
interface AudioUploadResponse {
  babble: Babble;         // The created babble with transcribed text
}
```

Or it could simply return a `Babble` directly (matching existing `createBabble` pattern).

---

## 4. Hooks Pattern

**Directory:** `prompt-babbler-app/src/hooks/` (13 hooks)

### Established Pattern (from `useBabbles.ts`)

- Hooks are named `use*.ts` in camelCase
- Use `useState`, `useCallback`, `useEffect`, `useRef`
- Acquire auth token via `useAuthToken()` hook, stored in a stable ref (`getAuthTokenRef`)
- Call API client functions with the token
- Return an object with state values and action callbacks
- Error state is `string | null`
- Loading states are boolean flags

### `useTranscription.ts` Pattern

- Manages WebSocket connection lifecycle (connect/send/disconnect/reset)
- Tracks `transcribedText`, `partialText`, `isConnected`, `error`
- Uses OpenTelemetry spans for tracing
- Acquires token via `useAuthToken()` before connecting

### Recommended Hook Structure for Upload

```typescript
// prompt-babbler-app/src/hooks/useFileUpload.ts
export function useFileUpload() {
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const getAuthToken = useAuthToken();
  const getAuthTokenRef = useRef(getAuthToken);
  getAuthTokenRef.current = getAuthToken;

  const upload = useCallback(async (file: File): Promise<Babble> => {
    setIsUploading(true);
    setError(null);
    try {
      const authToken = await getAuthTokenRef.current();
      const babble = await api.uploadAudioFile(file, authToken);
      return babble;
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Upload failed';
      setError(msg);
      throw err;
    } finally {
      setIsUploading(false);
    }
  }, []);

  return { upload, isUploading, error };
}
```

---

## 5. RecordPage — Babble Creation Flow

**File:** `prompt-babbler-app/src/pages/RecordPage.tsx` (260 lines)

### Flow

1. User enters optional title in `<Input>` field
1. User clicks record → `handleStart()` opens WebSocket transcription + starts audio capture in parallel
1. Real-time transcription accumulates in `transcribedText` state
1. User stops recording → `handleStop()` → `stopRecording()` + `disconnect()`
1. User clicks "Save Babble" → `handleSave()`:
   - Calls `createBabble({ title, text: transcribedText })` (from `useBabbles` hook)
   - On success: navigates to `/babble/${babble.id}` via `useNavigate()`
   - Shows toast notification via `sonner`
1. Alternative: "Save & Generate Prompt" → saves then navigates to `/babble/${babble.id}?autoGenerate=${templateId}`

### Append Mode

If route is `/record/:babbleId`, it loads the existing babble and appends new transcription text.

### Post-Upload Navigation Pattern

After a file upload transcription completes, follow the same pattern:

```typescript
void navigate(`/babble/${babble.id}`);
```

---

## 6. Router Configuration

**File:** `prompt-babbler-app/src/App.tsx` (Lines 45–52)

```tsx
<BrowserRouter>
  <Routes>
    <Route path="/" element={<HomePage />} />
    <Route path="/record" element={<RecordPage />} />
    <Route path="/record/:babbleId" element={<RecordPage />} />
    <Route path="/babble/:id" element={<BabblePage />} />
    <Route path="/templates" element={<TemplatesPage />} />
    <Route path="/settings" element={<SettingsPage />} />
  </Routes>
</BrowserRouter>
```

- Uses `react-router` v7 (package: `react-router@^7.13.1`)
- `BrowserRouter` + `Routes` + `Route` pattern
- No lazy loading or code splitting
- A new upload page (if needed) would be a new `<Route>` here

**Decision:** Upload may not need its own route — it could be a dialog/modal triggered from HomePage, or reuse the RecordPage with a different mode.

---

## 7. UI Library Stack

### Component Library: shadcn/ui

- **Package:** `shadcn@^4.0.8` (devDependency for CLI generation)
- **CSS framework:** Tailwind CSS v4 (`tailwindcss@^4.2.4`, `@tailwindcss/vite@^4.2.4`)
- **Class utilities:** `clsx@^2.1.1`, `tailwind-merge@^3.5.0`, `class-variance-authority@^0.7.1`
- **Primitives:** `radix-ui@^1.4.3` (underlying primitives for shadcn)
- **Icons:** `lucide-react@^1.14.0`
- **Toasts:** `sonner@^2.0.7`
- **Forms:** `react-hook-form@^7.73.1`, `@hookform/resolvers@^5.2.2`, `zod@^4.3.6`
- **Command palette:** `cmdk@^1.1.1`

### Available UI Components (in `src/components/ui/`)

alert-dialog, badge, button, card, checkbox, command, dialog, dropdown-menu, error-banner, input, label, scroll-area, select, separator, skeleton, tag-input, tag-list, textarea

### Relevant for Upload Feature

- `button.tsx` — Button with variants (default, outline, secondary, ghost, etc.)
- `dialog.tsx` — Modal dialog (useful for upload progress or file selection)
- `input.tsx` — Already has file input styling (`file:` Tailwind utilities)
- `label.tsx` — Form labels

---

## 8. Existing File Input Patterns

### Search Results

**No existing `<input type="file">` usage** in application code. The only file-related styling is in the generic `input.tsx` component which pre-styles file inputs with Tailwind `file:` utilities:

```text
file:text-foreground file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium
```

This means the shadcn `<Input>` component already supports `type="file"` with appropriate styling.

### Implementation Approaches

**Option A: Hidden file input + Button trigger (recommended)**

```tsx
const fileInputRef = useRef<HTMLInputElement>(null);

<Button variant="outline" onClick={() => fileInputRef.current?.click()}>
  <Upload className="size-4" />
  Upload Audio
</Button>
<input
  ref={fileInputRef}
  type="file"
  accept="audio/mpeg,audio/mp3,audio/wav,audio/webm"
  className="hidden"
  onChange={handleFileSelect}
/>
```

**Option B: shadcn Input with type="file"**

```tsx
<Input type="file" accept="audio/mpeg,audio/mp3" onChange={handleFileSelect} />
```

Option A is more common for styled button triggers.

---

## Implementation Summary

### Minimal Integration Path

1. **API client** (`api-client.ts`): Add `uploadAudioFile(file: File, accessToken?: string): Promise<Babble>` using FormData
1. **Hook** (`hooks/useFileUpload.ts`): Wrap upload with loading/error state and auth token acquisition
1. **HomePage** (`pages/HomePage.tsx`): Add Upload button in the `flex gap-2` container, with hidden `<input type="file">`, trigger upload on file selection
1. **Navigation**: After successful upload → navigate to `/babble/${babble.id}` (same as RecordPage pattern)
1. **Toast**: Use `sonner` toast for success/error feedback

### File Validation (client-side)

```typescript
const ACCEPTED_AUDIO_TYPES = ['audio/mpeg', 'audio/mp3', 'audio/wav', 'audio/webm', 'audio/ogg'];
const MAX_FILE_SIZE_MB = 25;

function validateAudioFile(file: File): string | null {
  if (!ACCEPTED_AUDIO_TYPES.includes(file.type)) {
    return 'Unsupported audio format. Please upload MP3, WAV, WebM, or OGG.';
  }
  if (file.size > MAX_FILE_SIZE_MB * 1024 * 1024) {
    return `File too large. Maximum size is ${MAX_FILE_SIZE_MB}MB.`;
  }
  return null;
}
```

---

## References

| Item | File Path | Lines |
|------|-----------|-------|
| HomePage header area | `prompt-babbler-app/src/pages/HomePage.tsx` | 37–51 |
| API client fetchJson helper | `prompt-babbler-app/src/services/api-client.ts` | 53–74 |
| API client buildHeaders | `prompt-babbler-app/src/services/api-client.ts` | 30–41 |
| Base URL injection | `prompt-babbler-app/src/services/api-client.ts` | 14–18 |
| Babble type definition | `prompt-babbler-app/src/types/index.ts` | 1–9 |
| useBabbles hook | `prompt-babbler-app/src/hooks/useBabbles.ts` | 1–270 |
| useTranscription hook | `prompt-babbler-app/src/hooks/useTranscription.ts` | 1–150 |
| useAuthToken hook | `prompt-babbler-app/src/hooks/useAuthToken.ts` | 1–50 |
| RecordPage save flow | `prompt-babbler-app/src/pages/RecordPage.tsx` | 113–140 |
| Router config | `prompt-babbler-app/src/App.tsx` | 45–52 |
| Package dependencies | `prompt-babbler-app/package.json` | 1–75 |
| Input component (file styling) | `prompt-babbler-app/src/components/ui/input.tsx` | 1–22 |

---

## Follow-on Questions (Relevant to Scope)

1. **Backend endpoint:** Does `/api/babbles/upload` already exist, or does it need to be created? (requires backend research)
1. **Transcription strategy:** Should the upload return a fully-transcribed `Babble` (server-side transcription), or should the client receive audio back for client-side streaming? Server-side is simpler.
1. **File size limit:** What file size limit should be enforced? Azure Speech SDK batch transcription has different limits than real-time.
1. **Progress indication:** Should the upload show progress (for large files)? Would need `XMLHttpRequest` or `fetch` with `ReadableStream` for upload progress.

---

## Clarifying Questions

- Should the upload button open a dialog with progress, or simply navigate to the babble detail page after completion?
- Should the upload support drag-and-drop in addition to the file picker?
- Is there a preference for the upload API endpoint path (e.g., `/api/babbles/upload` vs `/api/transcribe/file`)?
