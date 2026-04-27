<!-- markdownlint-disable-file -->
# RPI Validation: Phase 3 & 4

**Plan**: `.copilot-tracking/plans/2026-04-26/babble-search-semantic-plan.instructions.md`
**Changes log**: `.copilot-tracking/changes/2026-04-26/babble-search-semantic-changes.md`
**Research**: `.copilot-tracking/research/2026-04-26/babble-search-semantic-research.md`
**Validated**: 2026-04-26

---

## Phase 3: API Endpoint

### Step 3.1: Create `BabbleSearchResponse` API response model

- **Status**: PASS
- **Evidence**: [BabbleSearchResponse.cs](prompt-babbler-service/src/Api/Models/Responses/BabbleSearchResponse.cs) — file exists, 18 lines
- **Findings**:
  - `BabbleSearchResultItem` is a `sealed record` with `required init` properties — matches plan exactly
  - Properties: `Id`, `Title`, `Snippet`, `Tags`, `CreatedAt`, `IsPinned`, `Score` — all present per plan spec (Lines 430–445 of details)
  - Does NOT expose `ContentVector` or full `Text` — correct per research Scenario 11
  - Wrapped in `BabbleSearchResponse` with `Results` array — matches plan
  - Located in `Api/Models/Responses/` namespace — correct location

### Step 3.2: Add `Search` action to `BabbleController`

- **Status**: PASS (with minor observations)
- **Evidence**: [BabbleController.cs](prompt-babbler-service/src/Api/Controllers/BabbleController.cs#L78-L114)
- **Findings**:
  - **Route**: `[HttpGet("search")]` → resolves to `GET /api/babbles/search` — matches plan
  - **Query parameters**: `[FromQuery(Name = "q")] string query` and `[FromQuery] int topK = 10` — matches plan
  - **Input validation**: `query.Length < 2 || query.Length > 200` (2–200 chars) and `topK is < 1 or > 50` — matches plan details exactly
  - **User scoping**: `User.GetUserIdOrAnonymous()` used — matches plan and research multi-user requirement
  - **CancellationToken**: propagated through `SearchAsync(userId, query, topK, cancellationToken)` — correct
  - **Route ordering**: `Search` at line 78 is BEFORE `GetBabble` (`[HttpGet("{id}")]`) at line 117 — matches plan requirement to avoid route conflicts
  - **`[Authorize]` / `[RequiredScope("access_as_user")]`**: applied at class level (lines 14–15) — all endpoints including Search inherit these attributes. Correct.
  - **`[ProducesResponseType]`**: `typeof(BabbleSearchResponse), 200` and `400` — present, good for OpenAPI docs
  - **Response mapping**: `BabbleSearchResultItem` populated from `BabbleSearchResult` domain model with `Snippet = r.Babble.Text` (truncated by repository) — matches plan
  - **Return type**: `Task<IActionResult>` — matches plan specification and is consistent with all other actions in the controller (all 8 actions use `IActionResult`). The `copilot-instructions.md` states "Return `ActionResult<T>`" but the plan deliberately specified `IActionResult` to match the existing codebase pattern. No severity assigned — codebase consistency is appropriate.

### Step 3.3: Register `IEmbeddingGenerator` in `Program.cs`

- **Status**: PASS
- **Evidence**: [Program.cs](prompt-babbler-service/src/Api/Program.cs#L110-L113)
- **Findings**:
  - Registration: `openAiClient.GetEmbeddingClient(embeddingDeploymentName).AsIEmbeddingGenerator()` — matches plan exactly
  - Lifetime: `AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>` — matches plan (singleton)
  - Deployment name: configurable via `MicrosoftFoundry:embeddingDeploymentName` with `"embedding"` default — matches plan
  - `using Microsoft.Extensions.AI;` present at line 5 — correct
  - Placed inside the same `if` block as `IChatClient` registration, after the `AzureOpenAIClient` is created — correct; follows existing `IChatClient` pattern

### Step 3.4: Validate builds

- **Status**: PASS
- **Evidence**: Changes log states "208 backend unit tests pass" and "dotnet format clean"
- **Findings**: Plan required `dotnet build PromptBabbler.slnx` to pass. Changes log confirms builds and tests succeeded during final validation (Phase 7).

---

## Phase 4: Infrastructure — Aspire and Bicep

### Step 4.1: Add embedding model deployment to Aspire AppHost

- **Status**: PASS
- **Evidence**: [AppHost.cs](prompt-babbler-service/src/Orchestration/AppHost/AppHost.cs#L25-L34)
- **Findings**:
  - `foundryProject.AddModelDeployment("embedding", ...)` — name matches plan
  - Model: `"text-embedding-3-small"` with version `"1"` and format `"OpenAI"` — matches plan and research
  - Model name configurable via `MicrosoftFoundry:embeddingModelName` — matches plan pattern (follows `chatModelName` convention)
  - SKU: `GlobalStandard` with capacity `50` — matches plan exactly
  - **API service wiring**: `apiService` at line 62 has `.WithReference(embeddingDeployment)` and `.WaitFor(embeddingDeployment)` — correctly wired. Matches plan requirement that the API service references the embedding deployment.

### Step 4.2: Add embedding model to `model-deployments.json`

- **Status**: PASS
- **Evidence**: [model-deployments.json](infra/model-deployments.json#L13-L23)
- **Findings**:
  - Model entry: `"name": "text-embedding-3-small"`, `"version": "1"`, `"format": "OpenAI"` — matches plan
  - Deployment name: `"embedding"` — matches plan
  - SKU: `"GlobalStandard"` with capacity `50` — matches plan
  - No `raiPolicyName` on embedding model (correct — plan notes embedding models don't use content policies)
  - Existing `gpt-5.3-chat` entry preserved with its `raiPolicyName` — no unintended modifications

### Step 4.3: Add `EnableNoSQLVectorSearch` capability and vector container module to `main.bicep`

- **Status**: PASS
- **Evidence**: [main.bicep](infra/main.bicep#L376-L377) (capability), [main.bicep](infra/main.bicep#L460-L468) (module reference), [cosmos-babbles-vector-container.bicep](infra/cosmos-babbles-vector-container.bicep) (module definition)
- **Findings**:
  - **Capability**: `'EnableNoSQLVectorSearch'` added to `capabilitiesToAdd` array alongside existing `'EnableServerless'` — matches plan
  - **Vector container module**: Separate Bicep module `cosmos-babbles-vector-container.bicep` deployed as `babblesVectorContainer` — matches plan's DR-02 fallback (AVM module doesn't support `vectorEmbeddingPolicy`, so a raw resource is used)
  - **Module parameters**: `cosmosDbAccountName`, `databaseName: 'prompt-babbler'`, `location` — correct
  - **Module depends on**: `cosmosDbAccount` — ensures account exists before container override
  - **Vector container module contents** (verified in `cosmos-babbles-vector-container.bicep`):
    - Container name: `babbles`, partition key: `/userId` with Hash v2 — matches existing container config
    - `vectorEmbeddingPolicy`: path `/contentVector`, `Float32`, `Cosine`, 1536 dimensions — matches plan and research exactly
    - `vectorIndexes`: path `/contentVector`, type `quantizedFlat` — matches plan and research
    - `excludedPaths`: includes `/contentVector/*` and `/"_etag"/?` — matches plan (vector path excluded from regular indexing for insert performance)
    - `includedPaths`: `/*` — correct
    - API version: `2024-11-15` — current and supports vector search
  - **Dual container note**: The `babbles` container is still defined in the AVM module's `sqlDatabases.containers` array AND overridden by the raw resource module. The comments explain this is intentional — the AVM creates a basic container, then the module replaces it with vector-enabled properties. This is a valid Bicep pattern (last-write-wins for ARM resources).

### Step 4.4: Validate Bicep builds

- **Status**: PASS
- **Evidence**: Changes log states "Bicep builds" in validation summary. Terminal output from `infra: build (bicep)` task shows exit code 0.
- **Findings**: `az bicep build --file infra/main.bicep` succeeded.

---

## Summary

| Severity | Count | Details |
|---|---|---|
| Critical | 0 | — |
| Major | 0 | — |
| Minor | 0 | — |

**Overall Status**: **PASSED**

All 8 steps across Phase 3 and Phase 4 are implemented as specified in the plan. No deviations from the plan or research recommendations were found.

### Observations (informational, no severity)

1. **`IActionResult` vs `ActionResult<T>`**: The `Search` action returns `Task<IActionResult>` rather than `Task<ActionResult<BabbleSearchResponse>>`. This is consistent with the plan specification and all other controller actions in the file. The `copilot-instructions.md` prefers `ActionResult<T>`, but the plan deliberately chose `IActionResult` to match the existing codebase. No remediation needed.

2. **Duplicate Bicep comments**: Lines 452–454 and 456–458 in `main.bicep` contain two nearly identical comment blocks above the vector container module. Cosmetic only.

3. **Dual container definition**: The `babbles` container exists both in the AVM module's `sqlDatabases` array (basic) and as a separate raw resource module (vector-enabled). This works due to ARM last-write-wins semantics but could confuse future maintainers. A `// TODO:` comment or removing the basic definition from the AVM array when AVM adds vector support would improve clarity.

### Coverage Assessment

| Plan Item | Status |
|---|---|
| Step 3.1: `BabbleSearchResponse` model | Implemented exactly as planned |
| Step 3.2: `Search` controller action | Implemented exactly as planned |
| Step 3.3: `IEmbeddingGenerator` DI registration | Implemented exactly as planned |
| Step 3.4: Build validation | Confirmed passing |
| Step 4.1: Aspire embedding deployment | Implemented exactly as planned |
| Step 4.2: `model-deployments.json` entry | Implemented exactly as planned |
| Step 4.3: Bicep vector search config | Implemented exactly as planned |
| Step 4.4: Bicep build validation | Confirmed passing |

**Phase 3 coverage**: 100% — all plan items fully implemented
**Phase 4 coverage**: 100% — all plan items fully implemented
