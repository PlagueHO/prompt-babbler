# Squad Decisions

## Active Decisions

### Clean Architecture layering

**By:** Team (established convention)
**What:** The backend follows strict Clean Architecture: Domain → Infrastructure → Api. Domain contains only models (records) and interfaces with zero external dependencies. Infrastructure implements domain interfaces using Azure SDKs. Api wires DI and hosts controllers.
**Why:** Enforces separation of concerns and testability. Domain models can be tested without any Azure SDK dependency. Infrastructure implementations are swappable.

### Cosmos DB partition key strategy

**By:** Team (established convention)
**What:** Partition keys follow ownership hierarchy: `/userId` for user-owned data (babbles, templates, users), `/babbleId` for child data (generated-prompts). Built-in templates use userId `_builtin`. Anonymous mode uses userId `_anonymous`.
**Why:** Aligns partition boundaries with access patterns — queries always scope to a single user or babble. Prevents cross-partition queries in normal operation.

### LLM integration via Microsoft Agent Framework and IChatClient

**By:** Team (established convention)
**What:** Use Microsoft Agent Framework (`Microsoft.Agents.AI.OpenAI`) where possible for LLM interactions with Azure AI Foundry. Use `IChatClient` from `Microsoft.Extensions.AI` as the abstraction. Obtain via `openAiClient.GetChatClient("chat").AsIChatClient()`. Register as Transient.
**Why:** `IChatClient` provides a vendor-neutral abstraction for chat completions. Transient lifetime ensures each request gets a fresh context. `AsIChatClient()` is the correct adapter method (not `AsChatClient()`).

### SSE streaming format for prompt generation

**By:** Team (established convention)
**What:** All streaming endpoints use Server-Sent Events with the format: `data: {"name":"..."}` (optional) → `data: {"text":"..."}` (repeated) → `data: [DONE]` (terminal). Content-Type is `text/event-stream`.
**Why:** SSE is natively supported by browsers without polyfills. The `[DONE]` sentinel allows clean stream termination. The name/text split enables the frontend to show a title before content arrives.

### MSAL useRef pattern for auth token callbacks

**By:** Team (established convention)
**What:** In React hooks that fetch data on mount, always stabilize the `getAuthToken` callback using `useRef` to prevent infinite re-render loops:

```typescript
const getAuthTokenRef = useRef(getAuthToken);
getAuthTokenRef.current = getAuthToken;
```

**Why:** `useMsal()` returns unstable object references on every render. If `getAuthToken` (derived from MSAL) is used as a `useEffect` dependency, it triggers infinite re-fetching. The ref pattern breaks the cycle.

### OpenTelemetry SDK v2.x spanProcessors constructor pattern

**By:** Team (established convention)
**What:** Use `spanProcessors` array in the `WebTracerProvider` constructor options. Do not call `addSpanProcessor()` — it was removed in SDK v2.6.0.
**Why:** Breaking change in OTel SDK v2.6.0. The constructor-based approach is the only supported pattern.

### Singleton lifetime for Cosmos DB repositories

**By:** Team (established convention)
**What:** All Cosmos DB repositories and their wrapping services are registered as Singletons. `IRealtimeTranscriptionService` is also Singleton (maintains STS token cache). Only `IPromptGenerationService` is Transient.
**Why:** Cosmos DB `Container` is thread-safe and designed for reuse. Creating instances per-request wastes connection overhead. Speech service needs persistent token cache with `SemaphoreSlim` thread safety.

### System.Text.Json for all serialization

**By:** Team (established convention)
**What:** Use `System.Text.Json` with `CamelCase` naming policy for all API and Cosmos DB serialization. Never use `Newtonsoft.Json` for serialization. Domain models use `[JsonPropertyName]` attributes for explicit mapping.
**Why:** System.Text.Json is the native .NET serializer with better performance. Explicit `[JsonPropertyName]` attributes prevent breaking changes from property renames.

### Controller-level validation

**By:** Team (established convention)
**What:** Input validation is performed in controllers via explicit string length and enum checks. We do not use FluentValidation or DataAnnotations.
**Why:** Keeps validation visible and co-located with the endpoint. Avoids hidden validation pipeline magic. Controllers return `BadRequest()` with clear error messages.

### Test pyramid with AAA pattern

**By:** Team (established convention)
**What:** Follow the test pyramid: many unit tests, few integration tests, minimal E2E. All backend unit tests use the AAA (Arrange-Act-Assert) pattern. Unit tests use MSTest SDK 4.1 + FluentAssertions + NSubstitute. Integration tests use Aspire AppHost test harness (requires Docker for Cosmos DB emulator). Frontend tests use Vitest + Testing Library + jest-axe.
**Why:** Unit tests are fast and isolated — they form the base of the pyramid. Integration tests verify real Azure SDK interactions but are slower and require Docker. E2E tests are expensive (ephemeral Azure infra) and run only in CI/CD.

### Dual deployment mode (anonymous and Entra ID)

**By:** Team (established convention)
**What:** The application supports two deployment modes controlled by `AzureAd:ClientId` config. When not set: anonymous single-user mode with synthetic `_anonymous` ClaimsPrincipal. When set: Entra ID multi-user with JWT Bearer tokens and `access_as_user` scope.
**Why:** Anonymous mode enables zero-config local development — no Entra ID app registrations needed. The same codebase serves both modes via conditional middleware in `Program.cs`.

### Azure Speech STS token exchange

**By:** Team (established convention)
**What:** `SpeechConfig.FromAuthorizationToken()` does NOT accept raw AAD JWT tokens. First exchange via POST to `{aiServicesEndpoint}/sts/v1.0/issueToken` with `Authorization: Bearer {aadToken}`. Cache the STS token for 10 minutes with a 1-minute safety margin.
**Why:** Azure Speech SDK requires Cognitive Services STS tokens, not AAD tokens. Caching avoids per-request token exchange overhead. The 1-minute margin prevents edge-case expiry during long recognition sessions.

### Infrastructure as Code with Bicep and Azure Verified Modules

**By:** Team (established convention)
**What:** All Azure infrastructure is defined in Bicep using Azure Verified Modules (AVM) as the primary source. When an AVM module is not available or not up-to-date for the required resource type, fall back to pure Bicep. The custom `cognitive-services/accounts/main.bicep` module is a temporary fallback pending AVM support for AI Foundry V2. Microsoft Graph Bicep extension is used for Entra ID app registrations.
**Why:** AVM modules provide validated, tested, and well-documented infrastructure patterns maintained by Microsoft. Falling back to pure Bicep ensures we aren't blocked by AVM gaps while maintaining consistency in tooling.

### VNET Integration with Private Endpoints

**By:** Wash (DevOps/Infra)  
**Date:** 2026-03-19  
**Status:** Implemented

**What:** Added Azure Virtual Network (`10.0.0.0/16`) with two subnets: ACA infrastructure subnet (`10.0.0.0/23`), private endpoint subnet (`10.0.2.0/24`). Created private DNS zones for Cosmos DB (`privatelink.documents.azure.com`), Cognitive Services (`privatelink.cognitiveservices.azure.com`), and OpenAI (`privatelink.openai.azure.com`). Configured private endpoints for both Cosmos DB and Foundry. Container Apps Environment remains publicly accessible via `internal: false`.

**Why:** Private endpoints improve security by eliminating direct internet exposure for Cosmos DB and Foundry while keeping data on the Azure backbone. The existing `enablePublicNetworkAccess` parameter enables dev/test hybrid mode (public + private) and production private-only mode. Container Apps must remain public to serve Static Web App frontend.

**Key Learning:** AVM managed environment uses `infrastructureSubnetId` (not `infrastructureSubnetResourceId`). Container Apps Consumption environments do NOT require subnet delegation. Minimum /23 subnet. Private endpoint network policies must be disabled. Foundry requires both cognitiveservices and openai DNS zones.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
