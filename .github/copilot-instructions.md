# Copilot Instructions for prompt-babbler

## Project Overview

prompt-babbler is a speech-to-prompt web application that captures stream-of-consciousness speech, transcribes it using Azure AI Speech Service, and generates structured prompts for target systems like GitHub Copilot, general AI assistants, and image generators.

The application runs in two deployment modes:

- **Single-user mode** (default): No authentication. A synthetic `_anonymous` user identity is injected via middleware. All data is shared under a single user context.
- **Entra ID authentication mode**: Multi-user with Microsoft Entra ID (Azure AD). Users authenticate via MSAL in the browser, and the backend validates JWT Bearer tokens with `access_as_user` scope.

## Architecture

### Clean Architecture (.NET backend)

The backend follows Clean Architecture with strict dependency direction:

- **Domain** (`src/Domain/`): Business models (records with `init` properties), interfaces. No external dependencies.
- **Infrastructure** (`src/Infrastructure/`): Azure service implementations (Cosmos DB repositories, Azure OpenAI, Azure Speech SDK). Depends on Domain.
- **Api** (`src/Api/`): ASP.NET Core controllers, middleware, DI registration. Depends on Domain and Infrastructure.
- **Orchestration** (`src/Orchestration/`): .NET Aspire AppHost and ServiceDefaults. Orchestrates all resources for local development.

### Frontend (React SPA)

Single-page application built with React 19, served by Vite in development and deployed as an Azure Static Web App in production. Communicates with the backend via REST API and WebSocket.

### Aspire Orchestration

.NET Aspire orchestrates local development:

- Azure AI Foundry (cloud resource for LLM + Speech)
- Cosmos DB (preview emulator with Data Explorer — requires Docker)
- API service (ASP.NET Core project)
- Frontend SPA (Vite dev server via `AddViteApp` with pnpm)

Environment variables are injected by Aspire into both the API and frontend (via Vite `define` constants).

## Frontend Conventions

### TypeScript

- **Version**: TypeScript 5.9 with full strict mode enabled
- **Strict checks**: `strict`, `noUnusedLocals`, `noUnusedParameters`, `erasableSyntaxOnly`, `noFallthroughCasesInSwitch`, `noUncheckedSideEffectImports`
- **Module**: ESNext with bundler resolution (`verbatimModuleSyntax` enabled)
- **Target**: ES2022
- **Path alias**: `@/*` maps to `./src/*` — always use `@/` imports, never relative paths from deeply nested files

### React

- React 19 with hooks only — no class components
- React Router v7 for routing (`BrowserRouter` with `<Routes>`)
- React Hook Form + Zod v4 for form handling and validation
- Conditional `<MsalProvider>` wrapping based on `isAuthConfigured` flag
- `React.StrictMode` enabled in production

### UI Components

- **shadcn/ui** (New York style, v4 CLI) — installed components in `src/components/ui/`
- **Radix UI** primitives for accessible headless components
- **Lucide React** for icons
- **TailwindCSS v4** with `@tailwindcss/vite` plugin for styling
- **CVA** (class-variance-authority) for component variant management
- **Sonner** for toast notifications (themed via `ThemedToaster`)
- CSS variables for theming (`src/index.css`)

### MSAL Authentication Pattern

When using MSAL hooks, stabilize the `getAuthToken` callback with `useRef` to prevent infinite re-render loops caused by unstable object references from `useMsal()`:

```typescript
const getAuthTokenRef = useRef(getAuthToken);
getAuthTokenRef.current = getAuthToken;
```

This is critical in any hook that fetches data on mount with auth tokens as a dependency.

### OpenTelemetry (Browser)

- OpenTelemetry SDK v2.x — use `spanProcessors` array in `WebTracerProvider` constructor (the `addSpanProcessor` method was removed in SDK v2.6.0)
- `initTelemetry()` is called before React renders in `main.tsx`
- No-ops when OTEL endpoint is not configured (Aspire injects env vars)
- Custom metrics: `transcription.ttfw_ms`, `transcription.ws_connect_ms`, `recording.audio_init_ms`, `prompt.ttft_ms`, `prompt.duration_ms`

### Testing (Frontend)

- **Vitest 4.1** with jsdom environment, global test/expect
- **@testing-library/react** for component testing — query by role, not by test ID
- **jest-axe** for accessibility testing
- Setup file (`vitest.setup.ts`) provides global MSAL mocks
- Test files: `tests/**/*.test.{ts,tsx}` or `src/**/*.test.{ts,tsx}`
- ESLint 10 with typescript-eslint, React Hooks rules, React Refresh

## Backend Conventions

### Language & Framework

- **.NET 10** with ASP.NET Core
- **C#** with nullable reference types (`<Nullable>enable</Nullable>`)
- `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are both enabled
- All code must pass `dotnet format --verify-no-changes`

### Domain Models

- Use C# `record` types with `required` and `init` properties
- Always include `[JsonPropertyName("camelCase")]` attributes for explicit JSON mapping
- Use `System.Text.Json` with `CamelCase` naming policy — never Newtonsoft.Json for serialization
- Validation constants live on the model or are checked in controllers

### AI/LLM Integration

- Use **Microsoft Agent Framework** (`Microsoft.Agents.AI.OpenAI`) where possible for LLM interactions in Azure AI Foundry
- Use `IChatClient` from `Microsoft.Extensions.AI` as the abstraction layer
- Get `IChatClient` via `openAiClient.GetChatClient("chat").AsIChatClient()` — note: the method is `AsIChatClient()`, NOT `AsChatClient()`
- Streaming uses `chatClient.GetStreamingResponseAsync(messages)` returning `ChatResponseUpdate` objects with `.Text` property

### Dependency Injection

- **Singleton** for Cosmos DB repositories and services (Cosmos `Container` is thread-safe, no per-request state)
- **Singleton** for `IRealtimeTranscriptionService` (maintains STS token cache with SemaphoreSlim)
- **Transient** for `IPromptGenerationService` (each request needs fresh LLM context)
- Registration via `AddInfrastructure()` extension method in `DependencyInjection.cs`
- `HostedService` pattern for async initialization (e.g., `BuiltInTemplateSeedingService`)

### API Design

- SSE streaming format: `data: {"text":"..."}` chunks followed by `data: [DONE]`
- Structured prompt responses include name: `data: {"name":"..."}` then `data: {"text":"..."}` then `data: [DONE]`
- Pagination: Cosmos DB continuation tokens via `PagedResponse<T>` with `items` and `continuationToken`
- Controller-level validation (string length checks, enum validation) — not FluentValidation or DataAnnotations
- WebSocket endpoint (`/api/transcribe/stream`) accepts JWT via `?access_token=` query parameter

### Azure Speech SDK

- Token exchange: AAD token → POST to `{aiServicesEndpoint}/sts/v1.0/issueToken` → short-lived STS token → `SpeechConfig.FromAuthorizationToken()`
- STS tokens are cached with 10-minute validity and 1-minute safety margin
- Audio format: PCM 16kHz, 16-bit, mono via `PushAudioInputStream`
- Continuous recognition with `Channel<TranscriptionEvent>` for async event streaming

### Data Patterns

- **Cosmos DB** (serverless) with 4 containers in database `prompt-babbler`:
  - `babbles` — partition key `/userId`
  - `generated-prompts` — partition key `/babbleId`
  - `prompt-templates` — partition key `/userId` (built-in templates use `_builtin` userId)
  - `users` — partition key `/userId`
- Cascade delete: deleting a babble also deletes all its generated prompts
- User ID extraction: `ClaimsPrincipalExtensions.GetUserIdOrAnonymous()` — returns `_anonymous` in single-user mode
- In-memory cache for templates with 5-minute sliding expiration, invalidated on write

## Testing Strategy

### Test Pyramid

Follow the test pyramid — many unit tests, fewer integration tests, minimal E2E tests:

- **Unit tests** (fast, isolated): The majority of test coverage. Mock external dependencies with NSubstitute. No Docker or Azure resources required.
- **Integration tests** (slower, real dependencies): Small number using Aspire AppHost test harness (`Aspire.Hosting.Testing`). Requires Docker for Cosmos DB emulator. Tests real controller-to-database flows.
- **E2E tests** (slowest, full stack): Run in CI/CD pipeline only. Provision ephemeral Azure infrastructure, run tests, tear down.

### Backend Testing

- **Framework**: MSTest SDK 4.1 with `Microsoft.Testing.Platform`
- **Assertions**: FluentAssertions 8.9 — use `.Should()` style assertions
- **Mocking**: NSubstitute 5.3 — use `Substitute.For<T>()` for interface mocks
- **Pattern**: AAA (Arrange-Act-Assert) for all unit tests
- **Test categories**: Use `[TestCategory("Unit")]` or `[TestCategory("Integration")]` attributes
- **Coverage**: coverlet.collector with cobertura output
- **Integration tests**: Use `DistributedApplicationTestingBuilder` from Aspire. Requires Docker running for Cosmos DB preview emulator.

### Frontend Testing

- **Framework**: Vitest 4.1 with jsdom, globals enabled
- **Component testing**: @testing-library/react — prefer `getByRole`, `getByText` over `getByTestId`
- **Accessibility**: jest-axe for automated a11y checks
- **Mocking**: Vitest built-in mocks, MSAL mocked globally in `vitest.setup.ts`

## Infrastructure

- **Azure Bicep** with Azure Verified Modules (AVM) for resource provisioning
- **Naming**: `{abbreviation}-${environmentName}` (abbreviations in `infra/abbreviations.json`)
- **bicepconfig.json**: Microsoft Graph Bicep extension enabled (`microsoftgraph/v1.0:0.2.0-preview`)
- **Entra ID app registrations**: Graph Bicep extension, with preprovision hook scripts
- **CI/CD**: GitHub Actions with federated OIDC authentication (no client secrets)
- **Container images**: Published to `ghcr.io/plagueho/prompt-babbler-api` on version tags
- **GitVersion**: Determines SemVer automatically from git history

## Security Rules

- Never commit secrets, API keys, connection strings, or tokens to source code
- Always validate user input on the backend (controller-level validation)
- Use RBAC roles for Azure resource access — never access keys
- Bearer tokens via MSAL (frontend) and Microsoft.Identity.Web (backend)
- WebSocket auth uses query-string token extraction (configured in `Program.cs`)
- Federated OIDC for CI/CD pipelines — no client secrets in GitHub
- `DefaultAzureCredential` with tenant scoping for local development
