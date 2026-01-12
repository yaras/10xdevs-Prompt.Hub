# Public Prompts implementation plan (MVP)

## Decisions

1. **Scope:** The `Public Prompts` page is **list-only** (browse + open details). Creation and editing reuse the existing flows from `My Prompts`.
2. **Sorting:** MVP supports **newest only**.
3. **Pagination UX:** Use a **Load more** pattern (append results) as long as it remains low-complexity and maps cleanly to Azure Table continuation tokens.
4. **Title search:** MVP ships with **constrained search** (no full contains/indexed search in Table Storage yet). Constraints will be documented and reflected in UX.
5. **Tag filtering:** Implement **strict AND** semantics using **intersection logic** over the `TagIndex` table.
6. **Voting rules:** Users **can vote on their own prompts**, while maintaining **one vote per user per prompt**.
7. **Consistency model:** Use **eventual consistency** for public listing/indexes, while keeping **immediate updates** for the main `Prompts` aggregate counts (`Likes`/`Dislikes`).
8. **Owner actions in Public UI:** **Do not show** owner actions (edit/delete) anywhere in Public UI; these actions remain available only in `My Prompts`.

---

## Goal

Add a new `Public Prompts` page that allows authenticated users to:

- Browse public prompts ordered by **newest first**.
- Filter by **AND** tags (must contain all selected tags).
- Use **constrained title search**.
- Paginate via **Load more**.
- Vote (like/dislike) from public list and/or open details.

Non-goals for MVP:

- No create/edit/delete actions on the Public page.
- No full contains search / no `TitleSearchIndex`.
- No `most liked` ordering.

---

## Storage/query shape (Azure Table Storage)

### Tables involved

- `PublicPromptsNewestIndex`
  - Primary source for listing public prompts by newest.
  - Keys per schema plan:
    - `PartitionKey`: `pub|newest|{yyyyMM}`
    - `RowKey`: `{CreatedAtTicksDesc}|{PromptId}`
- `TagIndex`
  - Used for AND tag filtering.
  - Keys:
    - `PartitionKey`: `t|{tag}`
    - `RowKey`: `{PromptId}`
- `Prompts`
  - Canonical prompt storage; used when hydrating prompt details if index rows don’t contain enough fields.

### Constrained title search definition (MVP)

Since Table Storage cannot do efficient `contains`, constrained title search will be:

- Normalize the query: lower-case + trim.
- Apply the filter **in-memory** after fetching a limited page from `PublicPromptsNewestIndex`.
- UX constraints to keep this viable:
  - Minimum query length (recommend `>= 2` or `>= 3`).
  - While search text is non-empty, “Load more” continues to fetch additional pages and filter client-side.
  - Document that results are “best effort” and may require using “Load more” to discover additional matches.

Rationale: avoids full table scans / avoids adding `TitleSearchIndex` complexity for MVP.

### AND tag filtering approach

Use intersection of prompt ids obtained from `TagIndex` per selected tag:

1. For each selected tag `t`, query `TagIndex` partition `t|{t}` to obtain prompt ids.
2. Intersect prompt ids across all tags.
3. Fetch matching prompts from public newest listing source.

Important note: correct pagination across multiple tag partitions is hard with continuation tokens. MVP strategy:

- Use `PublicPromptsNewestIndex` as the stable ordering source.
- For each fetched page from newest index, filter to those whose `PromptId` exists in the intersected set.
- Continue loading pages until either:
  - enough results are accumulated for the UI page batch, or
  - the newest index runs out.

This keeps ordering correct (newest) and avoids attempting to “merge” multiple tag partitions with tokens.

---

## Implementation steps

### 1) Data contracts / models

1. Confirm or add a list model appropriate for public listing; prefer reusing `PromptSummaryModel` if it already fits:
   - Fields for list card: `PromptId`, `Title`, `Tags`, `CreatedAt`, `Likes`, `Dislikes`, `AuthorId` (optional for display).
2. Ensure tag data model supports:
   - Selected tags list (lower-case), max 10.
   - Allowed tags source (from `TagCatalog` table planned elsewhere).

Deliverables:

- If needed, add/extend `PromptSummaryModel` to include any missing list fields present in the `PublicPromptsNewestIndex` entity.

### 2) Table Storage entities and mapping

1. Ensure `PublicPromptsNewestIndexEntity` exists and matches schema plan.
2. Ensure mapper(s) exist to map index entity -> `PromptSummaryModel`.
3. Confirm `PromptEntityMapper` is not over-coupled to “My prompts” flows; add dedicated mapping methods if separation improves clarity.

Deliverables:

- Mapping function(s) to produce list-ready models from newest index rows.

### 3) Storage read path (query service)

Add a read method to the Table Storage read store layer (likely `TablePromptReadStore`):

- `GetPublicPromptsNewestPageAsync(...)` that:
  - Accepts page size and continuation token.
  - Queries `PublicPromptsNewestIndex` starting from current month bucket and then earlier months as needed.
  - Returns:
    - `IReadOnlyList<PromptSummaryModel>` parsed from index rows.
    - A continuation object that can represent:
      - current month partition being enumerated
      - index continuation token from Table SDK
      - when partition ends, move to previous month

MVP simplification options:

- Option A (recommended): store a composite continuation state as a serialized string (JSON) so the UI can pass it back.
- Option B: keep continuation state only in-memory in the component (works but breaks deep-linking / refresh scenarios).

Given “Load more” should be viable, prefer Option A.

Deliverables:

- A stable “public newest” query that does not scan and uses continuation tokens.

### 4) Tag index support (read)

Add read methods:

- `GetPublicPromptIdsByTagAsync(tag, max)`
  - Query `TagIndex` partition `t|{tag}`.
  - Return prompt ids.

Intersection strategy:

- For selected tags, query each partition up to some cap (e.g., `max = 1000`) and intersect in memory.
- Document the cap as an MVP tradeoff.

Deliverables:

- Efficient prompt-id retrieval per tag partition.

### 5) Public listing feature/service orchestration

Create a feature/service that orchestrates:

- base listing (newest)
- constrained search (client-side filter)
- tag AND filter (intersect prompt ids, then filter newest pages)

Suggested interface:

- `IPublicPromptsQuery`
  - `Task<PublicPromptsPageResult> GetPageAsync(PublicPromptsQuery query, CancellationToken ct)`

Where `PublicPromptsQuery` includes:

- `string? SearchText`
- `IReadOnlyList<string> Tags`
- `int PageSize`
- `string? Continuation`

And result includes:

- `IReadOnlyList<PromptSummaryModel> Items`
- `string? Continuation`
- `bool HasMore`

Deliverables:

- One cohesive “query” entry point for the page.

### 6) Write path updates (index maintenance)

Public listing relies on `PublicPromptsNewestIndex` being up-to-date.

Update the prompt write workflow (likely `TablePromptWriteStore`) so that on prompt create/update/soft-delete/visibility change it maintains:

- `PublicPromptsNewestIndex`
  - On create and `Visibility=public` and `IsDeleted=false`: upsert index row.
  - On update:
    - If still public and not deleted: update index row fields.
    - If switching to private or deleted: delete index row.
  - Note: `CreatedAt` is immutable; `UpdatedAt` changes should update row properties but not key.

Also ensure `TagIndex` maintenance aligns with tag add/remove and delete semantics (remove index rows on delete).

Deliverables:

- Public index stays correct without scans.

### 7) UI components

Add a new page (Blazor component), use `MyPrompts` as reference:

- File: `PromptHub.Web/Components/Pages/PublicPrompts.razor` (+ `.razor.cs` if needed)
- Route: `/public-prompts`

UI elements (MudBlazor):

- Search input (debounced) for constrained title search.
- Tag filter picker (predefined tags only).
- Prompt list rendering using existing `PromptCard` (ensure it hides owner actions).
- “Load more” button that calls query and appends results.
- Empty state:
  - No prompts at all.
  - No results for current search/filter.

State handling:

- Store `Continuation` returned by query.
- Reset list + continuation when search text or tags change.

Deliverables:

- Functional Public Prompts page with UX parity to My Prompts where appropriate.

### 8) Navigation

Add a nav link to Public Prompts in `MainLayout`/nav menu.

Deliverables:

- User can reach Public Prompts easily.

### 9) Voting integration

Ensure voting can be triggered from prompt cards on the public page:

- If `PromptCard` already supports voting callbacks/state, reuse.
- Otherwise:
  - add an abstraction to submit vote changes
  - update UI counts optimistically or after refresh

Rules:

- Allow self-voting.
- Enforce one vote per user per prompt (via `PromptVotes` keying).
- Update `Prompts` aggregates immediately with ETag concurrency + retry.
- Public indexes may lag (eventual consistency).

Deliverables:

- Like/dislike works from public catalog.

### 10) Error handling + resilience

- All storage calls should include:
  - retry with exponential backoff on 429
  - structured logging
  - user-friendly, non-sensitive error messages

UI:

- Show an inline error banner/snackbar when loading fails.
- Keep previous list visible if “Load more” fails.

Deliverables:

- Robust behavior under transient failures.

### 11) Testing

Unit tests (xUnit + FluentAssertions):

- `IPublicPromptsQuery`:
  - constrained search filters in-memory correctly
  - tag AND intersection behavior
  - continuation handling (basic)

Storage tests:

- In-memory/mocked Table abstractions for:
  - newest index paging
  - tag index prompt-id reads

Optional UI tests (bUnit):

- empty/no-results states
- Load more appends
- changing tags resets paging

---

## Acceptance criteria (implementation completeness)

- Authenticated users can navigate to Public Prompts.
- List loads public prompts ordered newest-first.
- Search works with documented constraints.
- Selecting multiple tags filters with AND semantics.
- “Load more” fetches additional items without reloading the page.
- No edit/delete controls appear anywhere in Public UI.
- Voting works and respects one-vote-per-user-per-prompt.

---

## Open points to confirm during implementation

- Route name (final): `/public-prompts`
- Search min length: `3`.
- Tag prompt-id cap per tag query (default `1000`).
