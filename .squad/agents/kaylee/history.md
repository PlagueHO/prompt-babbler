# Project Context

- **Owner:** Daniel Scott-Raynsford
- **Project:** Prompt Babbler — speech-to-prompt web application
- **Stack:** .NET 10.0.100 SDK, ASP.NET Core, C# (nullable reference types, TreatWarningsAsErrors), Azure.AI.OpenAI 2.1.0, Microsoft.Extensions.AI.OpenAI 10.3.0, Microsoft.Azure.Cosmos 3.57.1, Microsoft.CognitiveServices.Speech 1.48.2
- **Created:** 2026-03-19

## Core Context

### Backend Architecture (Clean Architecture)

- **Domain:** Records with init properties, interfaces only. Zero NuGet dependencies.
  - Models: `Babble`, `GeneratedPrompt`, `PromptTemplate`, `UserProfile` (with nested `UserSettings`)
  - Interfaces: `IBabbleRepository`, `IGeneratedPromptRepository`, `IUserRepository`, `IBabbleService`, `IGeneratedPromptService`, `IUserService`, `IRealtimeTranscriptionService`, `IPromptGenerationService`
- **Infrastructure:** Azure SDK implementations
  - `CosmosBabbleRepository`, `CosmosGeneratedPromptRepository`, `CosmosUserRepository` — all singletons
  - `AzureOpenAiPromptGenerationService` — IChatClient streaming, Transient
  - `AzureSpeechTranscriptionService` — PushAudioInputStream, SpeechRecognizer continuous recognition
  - `FileSettingsService` — `~/.prompt-babbler/settings.json`
- **Api:** Controllers + middleware + DI
  - `BabbleController`, `GeneratedPromptController`, `PromptController`, `UserController`, `TranscriptionWebSocketController`, `SettingsController`, `PromptTemplateController`
  - `DependencyInjection.cs` — service registration

### Key Patterns

- IChatClient: `openAiClient.GetChatClient("chat").AsIChatClient()` (NOT `.AsChatClient()`)
- Microsoft Agent Framework for complex LLM interactions
- SSE streaming: `data: {"name":"..."}` → `data: {"text":"..."}` → `data: [DONE]`
- Speech STS token: POST `{endpoint}/sts/v1.0/issueToken` with Bearer AAD token → 10-min cache
- System.Text.Json with CamelCase (never Newtonsoft)
- Controller-level validation (no FluentValidation/DataAnnotations)
- Cascade delete: babble deletion removes all generated prompts
- `PagedResponse<T>` with Cosmos continuation tokens
- Dual deployment: anonymous (`_anonymous` userId) vs Entra ID multi-user

### Cosmos DB Containers

- `babbles` — partition key `/userId`
- `generated-prompts` — partition key `/babbleId`
- `prompt-templates` — partition key `/userId` (built-in templates: `_builtin`)
- `users` — partition key `/userId`

### Key Files

- `prompt-babbler-service/src/Domain/Models/` — Babble.cs, GeneratedPrompt.cs, PromptTemplate.cs, UserProfile.cs
- `prompt-babbler-service/src/Domain/Interfaces/` — all I* interfaces
- `prompt-babbler-service/src/Infrastructure/Services/` — all implementations
- `prompt-babbler-service/src/Api/Controllers/` — all controllers
- `prompt-babbler-service/src/Api/Program.cs` — app startup, DI, middleware
- `prompt-babbler-service/Directory.Packages.props` — central package management

### Testing

- MSTest SDK 4.1.0 + FluentAssertions 8.9.0 + NSubstitute 5.3.0
- AAA pattern with comments: `// Arrange`, `// Act`, `// Assert`
- `[TestCategory("Unit")]`, `[TestCategory("Integration")]`
- Run: `dotnet test --solution PromptBabbler.slnx`

## Learnings

📌 Team initialized on 2026-03-19 — cast from Firefly universe
📌 Role: Backend Dev — .NET, Azure AI, Cosmos DB, APIs, Clean Architecture
📌 **Babble pinning/search/sort/filter (2026-03-21):** Added `IsPinned` (bool, default false) to `Babble` domain model. Updated `IBabbleRepository` and `IBabbleService` `GetByUserAsync` signatures with `search`, `sortBy`, `sortDirection`, `isPinned` optional params. `CosmosBabbleRepository` builds dynamic query using `StringBuilder` — appends `CONTAINS(LOWER(c.title), @search)` and `c.isPinned = @isPinned` conditionally; ORDER BY field/direction mapped from validated params. Controller validates `sortBy` ∈ {`createdAt`,`title`} and `sortDirection` ∈ {`desc`,`asc`}, search max 200 chars. UpdateBabble preserves `IsPinned` if not supplied (`request.IsPinned ?? existing.IsPinned`). All test mocks updated to new 8-arg signature.

**Follow-up (2026-03-21):** Home redesign session complete. Frontend implemented with dual-section UI (6 pinned bubbles + infinite-scroll list). Backend tested and validated for search/sort/filter. Coordinator validated test suite. Full stack ready for integration.

📌 **PATCH /api/babbles/{id}/pin endpoint:** Added dedicated `SetPinAsync` to `IBabbleRepository`, `IBabbleService`, `CosmosBabbleRepository`, and `BabbleService`. New `PinBabbleRequest` model with single `isPinned` bool. `PATCH {id}/pin` controller endpoint catches `InvalidOperationException` for 404. Repository does read-modify-write (read existing → update IsPinned + UpdatedAt → ReplaceItemAsync). PUT endpoint still supports isPinned for backward compatibility. Pattern: use PATCH for single-field mutations to avoid stale-data overwrites.
