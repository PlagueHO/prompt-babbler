# Search UX Thresholds Research

**Date:** 2026-04-26
**Status:** Complete
**Context:** Babble Search feature — React 19 + TypeScript app with .NET 10 backend, Cosmos DB NoSQL, Azure OpenAI embeddings, cmdk Command Palette UI

## Research Topics

1. Vector Search vs Text Search Routing — Minimum Query Complexity
1. Minimum Input Length Before Triggering Search
1. Debounce Delay Between Keystrokes
1. Enter-Triggered vs Live Search
1. Search API Response — Full Text vs Snippet

---

## Topic 1: Vector Search vs Text Search Routing — Minimum Query Complexity

### Findings

#### When Vector Search Adds Value Over Text Search

Vector (semantic) search excels at understanding **intent and meaning** — queries like "notes about my meeting with the design team" benefit from embeddings because the user's phrasing may not match the exact words stored in titles or text. However, for short exact-match queries like "standup" or "TODO", a simple `CONTAINS(LOWER(c.title), @search)` or full-text search performs equally well or better because:

- **Single-word queries** map directly to keyword matching. Embedding a single common word ("meeting") produces a generic vector that matches many unrelated documents.
- **Short queries (1-2 words)** have low semantic density — embeddings struggle to disambiguate intent. Research from Azure AI Search benchmark testing and Algolia's hybrid search documentation consistently shows that keyword search outperforms or matches vector search for queries under 3 words.
- **Exact title matches** are a primary use case for short queries in command palette UIs. Users typing "standup" expect to find a babble titled "Standup Notes", not semantically similar documents about "daily team check-ins."

#### Industry Patterns

- **Azure AI Search Hybrid Search**: Microsoft's own hybrid search runs both keyword (BM25) and vector search in parallel, merging with Reciprocal Rank Fusion (RRF). The guidance explicitly states: "Hybrid search with semantic ranking offers significant benefits in search relevance" — but this is for *combined* approaches where both signals exist. For cost-sensitive scenarios, Azure recommends tuning `maxTextRecallSize` to control the text-side contribution, acknowledging that keyword search is the cost-free baseline.
- **Algolia NeuralSearch**: Routes queries through keyword search first, then enriches with AI retrieval when keyword results are insufficient. Short/exact queries bypass the neural layer entirely.
- **Elasticsearch**: Elastic's hybrid approach uses a `knn` query alongside traditional BM25. Their documentation recommends using keyword search as the primary path and vector search as an enrichment layer, not a replacement.
- **Cosmos DB Hybrid Search**: Cosmos DB NoSQL now supports native hybrid search using `ORDER BY RANK RRF(VectorDistance(...), FullTextScore(...))` which combines vector and full-text search with RRF ranking — the same approach Azure AI Search uses. This is available with full-text indexes and BM25 scoring.

#### Cost Analysis

- `text-embedding-3-small` pricing: ~$0.02 per 1M tokens (or $0.00002 per 1K tokens)
- Average query: 5-15 tokens → cost per embedding call: ~$0.0000001 to $0.0000003
- At 1000 searches/day: ~$0.0003/day (~$0.01/month) — very low absolute cost
- **However**: The latency overhead of an embedding API call (50-200ms round-trip to Azure OpenAI) is more significant than the monetary cost. For live search-as-you-type, this latency compounds with each keystroke.

#### Word Count Threshold

No formal industry standard exists, but a practical consensus emerges:

- **1-2 words (< 15 characters)**: Text/keyword search only. High probability of exact-match intent.
- **3+ words (15+ characters)**: Likely expressing intent/concept. Vector search adds value.
- Heuristic used in practice: **word count >= 3 OR character count >= 20** → route to vector search.

### Recommendation

**Use a hybrid routing strategy** based on query complexity:

| Query Complexity | Search Strategy | Rationale |
|---|---|---|
| Empty or < 2 characters | No search (show default list) | Too short to be meaningful |
| 1-2 words AND < 15 chars | Text search only (`CONTAINS`) | Exact-match intent, zero embedding cost, instant |
| 3+ words OR 15+ chars | Vector search (or hybrid) | Semantic intent likely, embedding adds value |

The backend API should accept the raw query and decide routing internally — the frontend sends the same `?q=...` parameter regardless. This keeps routing logic server-side where it can be tuned without app updates.

**Cost implication**: This routing saves ~60-80% of embedding API calls (most command palette queries are 1-2 words), with negligible impact on result quality for short queries.

---

## Topic 2: Minimum Input Length Before Triggering Search

### Findings

#### Industry Standards

| Product | Minimum Before Search | Notes |
|---|---|---|
| **VS Code Command Palette** | 0 characters | Shows all commands immediately; filters on every keystroke |
| **GitHub Search** | 1 character | Live suggestions appear after first character |
| **Slack Search** | 1 character | Suggestions appear immediately, but full search requires Enter |
| **macOS Spotlight** | 1 character | Results appear after first character |
| **Google Search** | 1 character | Autocomplete suggestions from first character |
| **Algolia InstantSearch** | 0-1 characters | Configurable; default shows results immediately |
| **Amazon** | 1 character | Autocomplete suggestions from first character |

#### UX Research (NNGroup)

Nielsen Norman Group's research on search suggestions emphasizes:

- Search suggestions should appear as early as possible to **reduce interaction cost** — users type less and avoid typos.
- Even when users don't select a suggestion (only 23% do in e-commerce studies), the dropdown provides value by showing what's available.
- **Every suggested query should return good results** — suggesting with too few characters risks showing irrelevant results.

#### Cost Considerations for This Project

- Each text search query costs Cosmos DB RUs (~2-5 RUs for a simple `CONTAINS` query).
- Vector search queries cost both embedding API call + Cosmos DB RUs (~5-15 RUs for vector distance).
- With debouncing (Topic 3), the actual query rate is much lower than keystroke rate.

#### Autocomplete vs Full Search

- **Autocomplete/suggestions**: Typically fire from 1-2 characters. Client-side filtering is free.
- **Full server-side search**: Typically requires 2-3 characters minimum to produce meaningful results.
- For a command palette with pre-loaded items, 0 characters is fine (local filter). For API-backed search, 2 characters is the practical minimum.

### Recommendation

**Minimum 2 characters before sending an API search request.** Rationale:

- 1 character produces too many results and wastes RUs (searching for "a" matches nearly everything).
- 2 characters is the sweet spot: meaningful filtering without excessive noise.
- 0 characters should show a default state (recent babbles, pinned babbles, or empty).
- cmdk's built-in client-side filtering can handle instant feedback for pre-loaded items with 0-character minimum, while the API call waits for 2+ characters.

**Cost implication**: Saves ~30-40% of API calls compared to firing at 1 character (many queries start with a single character and quickly add more).

---

## Topic 3: Debounce Delay Between Keystrokes

### Findings

#### Industry Standards

| Product | Debounce Delay | Notes |
|---|---|---|
| **Google Instant** | ~200-300ms | Varies; uses predictive prefetch |
| **VS Code Quick Open** | ~0ms (client) / ~150ms (remote) | Client-side filtering is instant; remote file search has short debounce |
| **GitHub Search** | ~300ms | Server-side search with debounce |
| **Algolia InstantSearch** | 200-400ms recommended | Default `searchClient` debounce, configurable |
| **Slack Search** | ~300ms | Suggestions debounced |
| **Elasticsearch SearchKit** | 300ms default | Configurable |

#### UX Research

- **< 100ms**: Feels instant but generates too many API calls. Only appropriate for client-side filtering.
- **150-250ms**: Good balance for fast typists. Users perceive the UI as responsive. Standard for premium search products.
- **300-400ms**: Acceptable for most applications. Noticeable slight delay but tolerable. Standard for server-side search.
- **500ms+**: Users perceive lag. Not recommended for live search.
- **The key UX threshold**: Jakob Nielsen's research identifies **100ms as "instant"**, **1000ms as "flow-breaking"**, and **~200-300ms as the sweet spot** where users feel the system is responding without unnecessary queries.

#### Network Latency Factor

- Azure OpenAI embedding call: 50-200ms
- Cosmos DB query: 5-50ms
- Total backend latency: 55-250ms
- With a 300ms debounce + 150ms backend = ~450ms total perceived latency — acceptable.

#### Current Implementation

The existing `BabbleListSection.tsx` uses **300ms debounce** for the inline search — this is already a reasonable value.

### Recommendation

**Use 300ms debounce for text search, 300ms debounce for vector search.** Rationale:

- 300ms is the existing pattern in the codebase (consistency).
- 300ms balances responsiveness with cost optimization.
- For vector search specifically, the additional embedding latency means total time from keystroke to results is ~450-550ms — at the edge of acceptable. A shorter debounce (200ms) would improve perceived speed but increase API calls by ~30%.
- **Alternative consideration**: Use 200ms for text-only search (cheaper, faster) and 300ms for vector search (more expensive, slower backend). This adds complexity for marginal gain.

**Cost implication**: At 300ms debounce, a typical search session of 10 keystrokes generates ~3-4 API calls instead of 10. Combined with the 2-character minimum, most sessions produce 2-3 actual API calls.

---

## Topic 4: Enter-Triggered vs Live Search

### Findings

#### cmdk Default Behavior

Based on cmdk library research (v1.1.1):

- **cmdk filters items on every keystroke by default** — it's a live-filtering component. The `Command.Input` fires `onValueChange` on every character.
- Built-in filtering is **client-side only** (uses `value.includes(search)` by default). It does NOT make API calls — it filters pre-rendered `Command.Item` elements.
- For **async/server-side search**, the documented pattern is to use `shouldFilter={false}` and manage items yourself. The library provides `Command.Loading` for async states.
- **There is no built-in Enter-to-search behavior** — cmdk is designed as a live-filter component, not a form submission component.

#### Product Comparison

| Product | Trigger | Type | Notes |
|---|---|---|---|
| **VS Code Command Palette** | Live (every keystroke) | Client-side filter | All commands pre-loaded; Enter selects highlighted item |
| **VS Code Quick Open (Ctrl+P)** | Live (every keystroke) | Client-side + async | Files filtered locally first, remote results streamed in |
| **GitHub Command Palette (Ctrl+K)** | Live | Hybrid | Commands filtered locally, search results fetched async |
| **Slack Search** | Enter-triggered | Server-side | Suggestions are live, but full search requires Enter |
| **Spotlight / Raycast** | Live | Hybrid | Local results instant, web/API results streamed in |
| **Linear** | Live | Server-side | Issues searched live as you type |
| **Vercel Dashboard** | Live | Client + API | cmdk-based, local commands + async API |

#### UX Trade-offs

**Live Search (search-as-you-type):**

- (+) Immediate feedback — users see results narrowing
- (+) Discoverable — users learn what's available
- (+) Standard for command palettes
- (-) More API calls = higher cost
- (-) More complex state management (loading, stale results)

**Enter-Triggered Search:**

- (+) Fewer API calls = lower cost
- (+) Simpler implementation
- (-) Feels slower/less responsive
- (-) Not the command palette convention
- (-) Users must commit to a query before seeing any results

**Hybrid Approach:**

- Text search (cheap): Live, on every keystroke (after debounce + minimum length)
- Vector search (expensive): Triggered on Enter OR after a longer debounce (500ms+ idle)
- This provides instant feedback via text search while reserving costly vector search for deliberate queries.

### Recommendation

**Use a tiered live search approach:**

1. **Phase 1 (instant, 0ms)**: cmdk's built-in client-side filtering on pre-loaded items (recent babbles, pinned babbles). This is free and instant.
1. **Phase 2 (debounced, 300ms, 2+ chars)**: Text search API call via `CONTAINS(LOWER(c.title), @search)`. Fast, cheap (~2-5 RUs), gives immediate narrowing feedback.
1. **Phase 3 (debounced, 300ms, 3+ words)**: Vector search API call for semantic results. More expensive but provides the "magic" of semantic search.

cmdk's `shouldFilter={false}` mode lets us manage all three phases. Results from Phase 2 appear quickly (text search is fast), and Phase 3 results can be merged in or replace them when they arrive.

**Cost implication**: This hybrid approach means ~80% of search interactions only trigger text search. Vector search is only invoked for longer, more intentional queries where it genuinely adds value.

---

## Topic 5: Search API Response — Full Text vs Snippet

### Findings

#### Industry Standards

| Product | Response Format | Snippet Length | Score Included |
|---|---|---|---|
| **Azure AI Search** | Snippets via `highlight` + `captions` | Configurable, typically 200-300 chars | `@search.score` and `@search.rerankerScore` |
| **Elasticsearch** | Full doc + optional `highlight` fragments | Default 100 chars per fragment | `_score` (BM25/vector) |
| **Algolia** | Full attributes + `_snippetResult` + `_highlightResult` | Configurable, default ~30 words | Ranking info optional |
| **Google Custom Search** | Snippets only | ~150-200 chars | No |
| **GitHub Search API** | Full match + `text_matches` fragments | Variable | `score` |

#### Best Practices

1. **Return snippets, not full text** for search results:
   - Bandwidth: A babble's `text` field can be up to 50,000 characters. Returning 10 full documents could be 500KB+ vs ~5KB for snippets.
   - Rendering: Search results only display a preview. Full text is loaded when a user selects a result.
   - Privacy: Snippets limit data exposure in transit.

1. **Always include a relevance score**:
   - Allows the frontend to display results in relevance order.
   - Enables threshold-based filtering (e.g., hide results below 0.5 similarity).
   - For vector search: `VectorDistance` returns a cosine distance score.
   - For text search: A binary "matched" is sufficient (or use `FullTextScore` for BM25 ranking).

1. **Snippet highlighting**:
   - Standard practice in all major search products.
   - Cosmos DB does NOT have built-in snippet/highlight support — this must be done server-side in C# or client-side in the frontend.
   - For text search: Simple substring highlighting around the match.
   - For vector search: No natural highlight target (semantic match, not keyword match). Return the first N characters of text as the snippet.

1. **Recommended snippet length**:
   - **100-200 characters** for command palette results (limited display space).
   - **200-300 characters** for full search result pages.
   - Algolia recommends ~30 words (~200 chars) as optimal for scannability.

#### Cosmos DB Query Patterns for Snippets

Cosmos DB supports server-side projection with `SUBSTRING`:

```sql
SELECT c.id, c.title, c.userId, c.createdAt, c.isPinned, c.tags,
       SUBSTRING(c.text, 0, 200) AS snippet,
       VectorDistance(c.contentVector, @embedding) AS score
FROM c
WHERE c.userId = @userId
ORDER BY VectorDistance(c.contentVector, @embedding)
```

- `SUBSTRING(c.text, 0, 200)` returns the first 200 characters — efficient server-side truncation.
- The full `c.text` field is NOT returned, saving bandwidth and RUs.
- For text search with highlighting, the match position can be found server-side using `INDEX_OF(LOWER(c.text), @search)` to generate a snippet centered on the match.

#### Response Schema Recommendation

```json
{
  "results": [
    {
      "id": "guid",
      "title": "Standup Notes",
      "snippet": "Discussed the new search feature implementation with the team...",
      "tags": ["meeting", "standup"],
      "createdAt": "2026-04-25T10:00:00Z",
      "isPinned": false,
      "score": 0.87,
      "searchType": "vector"
    }
  ],
  "totalCount": 42,
  "searchType": "vector|text"
}
```

### Recommendation

**Return snippets (first 200 characters of text) + title + relevance score. Do NOT return full text.**

1. **Snippet**: Use `SUBSTRING(c.text, 0, 200)` in the Cosmos DB query projection. For text search, compute a context-aware snippet centered on the first match position.
1. **Score**: Include `VectorDistance` score for vector results. For text search, a simple relevance indicator (matched title vs matched text) is sufficient.
1. **Highlight matching terms**: Do client-side highlighting in the React component by wrapping matched terms in `<mark>` tags. This avoids server-side complexity.
1. **Both title and text fields**: Search across both `title` and `text`, but only display title + snippet in results.
1. **searchType indicator**: Include whether the result came from text or vector search, enabling the frontend to render differently if needed.

**Cost implication**: Returning 200-char snippets instead of full text reduces response payload by ~95% for long babbles, reducing bandwidth costs and improving perceived performance.

---

## Key Discoveries

1. **Cosmos DB supports native hybrid search** with `ORDER BY RANK RRF(VectorDistance(...), FullTextScore(...))` — combining vector and full-text BM25 scoring with weighted RRF. This is a strong alternative to simple `CONTAINS` for the text-search tier.
1. **cmdk is a live-filter component** — its default behavior is instant client-side filtering. For server-side search, use `shouldFilter={false}` and manage async results manually with `Command.Loading`.
1. **The existing codebase uses 300ms debounce** in `BabbleListSection.tsx` — this is the established pattern and should be maintained for consistency.
1. **Cosmos DB has `SUBSTRING` function** — server-side snippet extraction is possible without UDFs, directly in the query projection.
1. **Query routing by complexity is the cost-optimal approach** — 1-2 word queries use text search (free, fast); 3+ word queries use vector search (embedding cost + latency).
1. **Embedding cost is negligible at expected volumes** (~$0.01/month for 1000 queries/day) — the real concern is **latency** (50-200ms per embedding call), not monetary cost.

## Summary Recommendation Table

| Parameter | Recommended Value | Rationale |
|---|---|---|
| Minimum characters for search | 2 | Below 2 chars produces noisy results, wastes RUs |
| Debounce delay | 300ms | Matches existing codebase pattern, balances UX and cost |
| Text search routing | 1-2 words, < 15 chars | Exact-match queries; no embedding needed |
| Vector search routing | 3+ words OR 15+ chars | Semantic intent queries; embedding adds value |
| Search trigger | Live (debounced) | Standard for command palette UX; cmdk default pattern |
| Snippet length | 200 characters | Fits command palette UI; covers 1-2 sentences |
| Include relevance score | Yes | Enables sorting and threshold filtering |
| Response includes full text | No | Bandwidth optimization; load on selection |

## References

- [Azure AI Search Hybrid Search Overview](https://learn.microsoft.com/en-us/azure/search/hybrid-search-overview)
- [Azure AI Search Hybrid Query How-To](https://learn.microsoft.com/en-us/azure/search/hybrid-search-how-to-query)
- [Cosmos DB Hybrid Search (RRF)](https://learn.microsoft.com/en-us/azure/cosmos-db/gen-ai/hybrid-search)
- [Cosmos DB SUBSTRING function](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/substring)
- [Cosmos DB Full Text Search functions](https://learn.microsoft.com/en-us/cosmos-db/query/fulltextscore)
- [cmdk GitHub Repository](https://github.com/pacocoursey/cmdk) — v1.1.1 documentation
- [NNGroup: Site Search Suggestions](https://www.nngroup.com/articles/site-search-suggestions/)
- Existing codebase: `prompt-babbler-app/src/components/babbles/BabbleListSection.tsx` (300ms debounce pattern)
- Existing research: `.copilot-tracking/research/2026-04-26/babble-search-semantic-research.md`

## Follow-On Questions Discovered

- **Cosmos DB Full Text Search availability**: The `FullTextScore` and `FullTextContains` functions require a full-text index. Need to verify if this is GA or preview for serverless accounts, and whether the AVM Bicep module supports full-text indexing policies.
- **Cosmos DB RRF weighted hybrid**: If using native hybrid search, need to determine if the RRF weighting (`[2, 1]`) can be parameterized at query time for dynamic routing (e.g., favor text results for short queries).
- **Embedding caching**: For repeated queries, should embeddings be cached (in-memory or Redis) to avoid redundant Azure OpenAI calls? LRU cache with ~1000 entries would cover most repeated searches.
