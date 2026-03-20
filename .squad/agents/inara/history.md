# Project Context

- **Owner:** Daniel Scott-Raynsford
- **Project:** Prompt Babbler — speech-to-prompt web application
- **Stack:** React 19.2.0, TypeScript ~5.9.3 (strict mode), Vite 8.0.0, shadcn/ui v4 (New York style), TailwindCSS 4.2.1, React Router 7.13.1, MSAL React 3.0.28, OpenTelemetry SDK 2.6.0
- **Created:** 2026-03-19

## Core Context

### Frontend Architecture

- **Path alias:** `@/*` → `./src/*`
- **Components:** shadcn/ui v4 (New York style, 13 components installed), Radix UI 1.4.3, Lucide React for icons
- **Styling:** TailwindCSS 4.2.1 with `@tailwindcss/vite` plugin, CVA 0.7.1, tailwind-merge 3.5.0, `cn()` from `@/lib/utils`
- **Forms:** React Hook Form 7.71.2 + Zod 4.3.6
- **Auth:** MSAL `@azure/msal-browser` 4.29.1 + `@azure/msal-react` 3.0.28 — useRef pattern for stable token callbacks
- **Toasts:** Sonner 2.0.7
- **OTel:** SDK v2.6.0 — `spanProcessors` in constructor (NOT `addSpanProcessor`). Custom metrics: `transcription.ttfw_ms`, `recording.audio_init_ms`, `prompt.ttft_ms`, `prompt.duration_ms`
- **Vite constants:** `__API_BASE_URL__`, `__MSAL_CLIENT_ID__`, `__MSAL_TENANT_ID__`, `__OTEL_*__`
- **Dual deployment:** Anonymous mode (no auth) + Entra ID multi-user

### Key Hooks

- `useTranscription` — WebSocket transcription session management
- `useAudioRecording` — AudioWorklet PCM capture (public/pcm-processor.js)
- `useBabbles` — CRUD + pagination via continuation tokens
- `useGeneratedPrompts` — per-babble prompt history
- `useUserSettings` — profile fetch, optimistic localStorage cache
- `usePromptGeneration` — SSE streaming prompt generation

### Key Files

- `prompt-babbler-app/src/hooks/` — all custom hooks
- `prompt-babbler-app/src/pages/` — page components (BabblePage, SettingsPage, etc.)
- `prompt-babbler-app/src/components/` — UI components
- `prompt-babbler-app/src/services/` — API client, transcription-stream, migration
- `prompt-babbler-app/src/config/` — MSAL auth configuration
- `prompt-babbler-app/vite.config.ts` — build config + OTEL env forwarding

### Testing

- Vitest 4.1.0 + @testing-library/react 16.3.2 + jest-axe 10.0.0
- ESLint 10.0.3 + typescript-eslint 8.57.1
- Run: `pnpm test`

## Learnings

📌 Team initialized on 2026-03-19 — cast from Firefly universe
📌 Role: Frontend Dev — React components, hooks, UI, MSAL, OTel browser
