---
title: Copilot Instructions
description: Coding conventions for GitHub Copilot in the Prompt Babbler repository.
---

See [AGENTS.md](../AGENTS.md) for directory layout, commands, and CI pipeline.

## Purpose

Prompt Babbler is a React 19 + Vite (TypeScript) SPA paired with a .NET 10 ASP.NET Core backend orchestrated by .NET Aspire. Users record voice input that is transcribed and stored as "babbles"; Azure OpenAI generates prompts from those babbles using configurable templates. Data is persisted in Cosmos DB; Microsoft Entra ID handles authentication with an optional access-code bypass.

## Security

- Never log request bodies or transcription content — they may contain PII.
- All API controllers require `[Authorize]` + `[RequiredScope("access_as_user")]`; never remove or weaken these attributes.
- Use `FixedTimeEquals` (constant-time comparison) for access-code or secret validation — never use `==` or `string.Equals`.
- Validate all external input at the controller boundary; reject early with `BadRequest` before calling services.
- Never commit secrets or connection strings; use Aspire configuration or `appsettings.Development.json`.

## ASP.NET Core Patterns

- **Every C# class and record must be `sealed`** unless explicitly designed for inheritance.
- Domain models are immutable `sealed record` types: `required` + `init` properties, `[JsonPropertyName]` attributes.
- Interfaces in `Domain/Interfaces/`, implementations in `Infrastructure/Services/`; controllers depend only on domain interfaces.
- Inject all dependencies via constructor — no service locator, no static access.
- Controllers return `IActionResult`; never expose infrastructure types to the API layer.

```csharp
// Prefer
public sealed class BabbleService : IBabbleService
{
    private readonly IBabbleRepository _babbleRepository;
    public BabbleService(IBabbleRepository babbleRepository) => _babbleRepository = babbleRepository;
}

// Avoid — missing sealed; nullable field; field names not _camelCase
public class BabbleService : IBabbleService
{
    public IBabbleRepository? BabbleRepository;
}
```

## React Patterns

- Custom hooks (`use*`) in `src/hooks/`; all API calls through `src/services/api-client.ts` only.
- All shared types in `src/types/index.ts`; use `interface` for object shapes, `type` for unions/primitives.
- Use the `@/` path alias for all non-relative imports.
- Components use **named exports**, not default exports.
- State is local hook state — no global state library.

```typescript
// Prefer
import { BabbleCard } from '@/components/babbles/BabbleCard';
import type { Babble } from '@/types';

// Avoid
import BabbleCard from '../components/babbles/BabbleCard';
import { Babble } from '../../types/index';
```

## Naming Conventions

| Element              | Convention   | Example                   |
|----------------------|--------------|---------------------------|
| C# types/records     | PascalCase   | `BabbleService`           |
| C# private fields    | \_camelCase  | `_babbleService`          |
| C# interfaces        | I-prefix     | `IBabbleService`          |
| TS components        | PascalCase   | `BabbleCard.tsx`          |
| TS hooks/services    | camelCase    | `useAuthToken.ts`         |
| TS lib/utils         | kebab-case   | `api-client.ts`           |
| TS functions/hooks   | camelCase    | `useBabbles`, `fetchJson` |
| TS types             | PascalCase   | `Babble`, `UserProfile`   |

## Testing

- **Backend**: MSTest + FluentAssertions + NSubstitute. **Every test class must have `[TestCategory("Unit")]` or `[TestCategory("Integration")]`**. Use `Substitute.For<IInterface>()` — not Moq.
- **Frontend**: Vitest + Testing Library. Tests live in `tests/` (mirror `src/` structure — not co-located). Use `describe`/`it` with natural-language names.
- Backend test method names: `MethodName_Condition_ExpectedResult` (e.g. `GetBabble_ExistingId_ReturnsBabble`).
- Frontend test names: plain prose describing observable behaviour (e.g. `it('renders babble title and date')`).
- Mock only at dependency boundaries; test observable outputs, not internal call counts.
