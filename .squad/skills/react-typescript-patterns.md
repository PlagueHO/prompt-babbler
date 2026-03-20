# Skill: React & TypeScript Patterns

## Confidence: medium

## Overview

The prompt-babbler frontend is a React 19 SPA built with TypeScript 5.9 (strict mode), Vite 8, shadcn/ui (New York style), TailwindCSS v4, and MSAL for optional Entra ID authentication. This skill covers the component structure, custom hook patterns, UI conventions, and testing approach.

## Project Structure

```bash
prompt-babbler-app/
├── src/
│   ├── App.tsx                    # Router + layout + providers
│   ├── main.tsx                   # Entry: OTel init → MSAL init → render
│   ├── index.css                  # Tailwind + CSS variables
│   ├── telemetry.ts               # OpenTelemetry setup
│   ├── auth/authConfig.ts         # MSAL configuration
│   ├── components/
│   │   ├── ui/                    # shadcn/ui components (13 installed)
│   │   ├── layout/                # PageLayout, Navigation
│   │   ├── babble/                # Babble-related components
│   │   ├── prompts/               # Prompt generation/history components
│   │   └── ...                    # Feature-organized components
│   ├── hooks/                     # 11 custom hooks
│   ├── services/                  # API client, localStorage, migration
│   ├── pages/                     # Route page components
│   ├── types/index.ts             # All type definitions
│   └── lib/utils.ts               # cn() utility for className merging
├── tests/                         # Test files
├── public/pcm-processor.js        # AudioWorklet for PCM capture
├── vitest.config.ts               # Test configuration
└── vite.config.ts                 # Build configuration
```

## TypeScript Conventions

- **Path alias**: Always use `@/` imports (maps to `./src/`), never deep relative paths
- **Strict mode**: All strict checks enabled (`noUnusedLocals`, `noUnusedParameters`, `erasableSyntaxOnly`, etc.)
- **Types**: All types defined in `src/types/index.ts` — export as named types
- **Named exports**: Prefer named exports over default exports (exception: page components for lazy loading)
- **verbatimModuleSyntax**: Use `import type { X }` for type-only imports

## Custom Hook Patterns

All custom hooks follow these conventions:

1. **Return object with named properties** (not tuple):

   ```typescript
   return { babbles, loading, error, hasMore, loadMore, createBabble, ... };
   ```

1. **Auth-conditional fetching**: Check `isAuthConfigured` before making authenticated requests
1. **useRef for MSAL callbacks**: Stabilize `getAuthToken` with `useRef` to prevent infinite loops
1. **Cleanup on unmount**: Use `useEffect` cleanup functions for WebSockets, AbortControllers
1. **Pagination**: Use continuation tokens from `PagedResponse<T>`, track `hasMore` state

### Key Hooks

| Hook | Purpose |
|------|---------|
| `useAudioRecording` | PCM audio capture via AudioWorklet (16kHz/16-bit/mono) |
| `useTranscription` | WebSocket real-time transcription with token refresh |
| `usePromptGeneration` | SSE streaming prompt generation with TTFT metrics |
| `useBabbles` | Babble CRUD with pagination and localStorage migration |
| `useGeneratedPrompts` | Per-babble prompt history with pagination |
| `useAuthToken` | MSAL token acquisition with silent/popup fallback |
| `useUserSettings` | User profile + settings with localStorage cache |
| `useTemplates` | Prompt template management (built-in + user) |
| `useLocalStorage` | Generic typed localStorage hook |
| `useSettings` | Backend status check |
| `useTheme` | Theme context accessor |

## UI Component Conventions

### shadcn/ui

- **Style**: New York
- **CLI**: `shadcn` v4 for adding components
- **Location**: `src/components/ui/`
- **Config**: `components.json` with path aliases
- **Variants**: Use CVA (class-variance-authority) for component variants
- **Icons**: Lucide React

### Styling

- **TailwindCSS v4** with `@tailwindcss/vite` plugin
- **Class merging**: Use `cn()` from `@/lib/utils` (combines `clsx` + `tailwind-merge`)
- **CSS variables**: Theme colors defined in `src/index.css`
- **Animations**: `tw-animate-css` for animation utilities

### Form Handling

- **React Hook Form** for form state
- **Zod v4** for schema validation
- **@hookform/resolvers** to connect Zod schemas to RHF

### Notifications

- **Sonner** via `<ThemedToaster>` component — use `toast.success()`, `toast.error()`

## Routing

React Router v7 with `BrowserRouter`:

```typescript
<Routes>
  <Route path="/" element={<HomePage />} />
  <Route path="/record" element={<RecordPage />} />
  <Route path="/record/:babbleId" element={<RecordPage />} />
  <Route path="/babble/:id" element={<BabblePage />} />
  <Route path="/templates" element={<TemplatesPage />} />
  <Route path="/settings" element={<SettingsPage />} />
</Routes>
```

## API Client

`src/services/api-client.ts` provides typed functions for all backend endpoints:

- Generic `fetchJson<T>()` helper with auth header injection
- HTML error detection (backend unavailable returns HTML)
- URL encoding for path segments
- SSE streaming via `fetch()` returning `ReadableStream<Uint8Array>`

## Vite Configuration

- Aspire injects env vars → Vite `define` exposes as compile-time constants (`__API_BASE_URL__`, `__MSAL_CLIENT_ID__`, etc.)
- All constants default to empty string when not injected
- Plugins: `@vitejs/plugin-react` + `@tailwindcss/vite`

## Testing

- **Vitest 4.1** with jsdom, globals enabled
- **@testing-library/react** — query by role/text, not test ID
- **jest-axe** for a11y testing
- **vitest.setup.ts** — global MSAL mock with test user `testuser@contoso.com`
- Test location: `tests/**/*.test.{ts,tsx}` or `src/**/*.test.{ts,tsx}`
