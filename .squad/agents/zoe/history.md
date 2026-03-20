# Project Context

- **Owner:** Daniel Scott-Raynsford
- **Project:** Prompt Babbler — speech-to-prompt web application
- **Stack:** MSTest SDK 4.1.0, FluentAssertions 8.9.0, NSubstitute 5.3.0 (backend); Vitest 4.1.0, @testing-library/react 16.3.2, jest-axe 10.0.0 (frontend)
- **Created:** 2026-03-19

## Core Context

### Test Strategy

- **Test pyramid:** Many unit tests (fast, isolated) > Few integration tests (Docker + Cosmos emulator) > Minimal E2E
- **AAA pattern** for ALL backend tests: `// Arrange`, `// Act`, `// Assert` — clearly separated

### Backend Testing

- Framework: MSTest SDK 4.1.0
- Assertions: FluentAssertions 8.9.0 (never raw `Assert.*`)
- Mocking: NSubstitute 5.3.0 (never Moq)
- Categories: `[TestCategory("Unit")]`, `[TestCategory("Integration")]`
- Location: `prompt-babbler-service/tests/` — mirrors source structure
- Integration tests require Docker for Cosmos emulator
- Run all: `dotnet test --solution PromptBabbler.slnx`
- Run unit only: `dotnet test --solution PromptBabbler.slnx --filter "TestCategory=Unit"`

### Frontend Testing

- Framework: Vitest 4.1.0 with jsdom 29.0.0 environment
- Component testing: @testing-library/react 16.3.2 — test behavior, not implementation
- Accessibility: jest-axe 10.0.0
- ESLint 10.0.3 + typescript-eslint 8.57.1 for static analysis
- Location: `prompt-babbler-app/tests/` — mirrors source structure
- Run: `pnpm test`

### Key Patterns to Test

- Dual deployment (anonymous vs Entra ID) — both paths need coverage
- SSE streaming: `data: {"text":"..."}` chunks + `data: [DONE]` terminator
- WebSocket transcription: PCM audio → recognition results
- Cosmos DB repositories: CRUD, pagination with continuation tokens, cascade deletes
- MSAL useRef pattern: stable token callbacks
- Controller validation: invalid input → proper error responses
- Clean Architecture: verify layer boundaries (no Infrastructure types in Domain)

### Key Files

- `prompt-babbler-service/tests/` — backend test projects
- `prompt-babbler-app/tests/` — frontend test files
- `prompt-babbler-app/vitest.config.ts` — Vitest configuration

## Learnings

📌 Team initialized on 2026-03-19 — cast from Firefly universe
📌 Role: Tester — unit tests, integration tests, quality gate, a11y testing
