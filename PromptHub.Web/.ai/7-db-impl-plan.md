# Database implementation plan (MVP) — Azure Table Storage

This document turns the schema in `PromptHub.Web/.ai/4-db-plan.md` (and the decisions summary in `PromptHub.Web/.ai/3-database.md`) into an implementable plan for the current codebase.

## Scope / decisions (from prior docs + your answers)

- Storage: **Azure Table Storage**.
- MVP speed: **single project** (`PromptHub.Web`) but with clear directories/namespaces.
- Storage entities are **1:1 with table rows** and may directly use Azure Table Storage shapes.
- Application uses **domain/application models**; mapping exists between app models and storage entities.
- Architecture inside the project follows a **Clean Architecture-inspired folder layout**:
  - entities, features (CQRS), interfaces, frameworks (storage).
- Patterns:
  - **CQRS**: feature-oriented write workflows (commands/handlers) + simple read repositories (queries).
  - Index tables updated **synchronously in the same request** (MVP simplicity).
  - Retry/backoff, continuation token pagination, ETag helpers live in an **infrastructure module**.
- Client setup: shared `TableServiceClient` factory + **per-table wrappers**.
- Tests: **pure unit tests** using **in-memory fakes**.

---

## 1) Directory (folder) structure — single project

All paths below are within `PromptHub.Web/`.

```
PromptHub.Web/
  Application/
    Models/
      Prompts/
      Tags/
      Votes/
    Abstractions/
      Persistence/
      Clock/
      Auth/
    Features/
      Prompts/
        Commands/
        Queries/
      Votes/
        Commands/
        Queries/
      Tags/
        Queries/
    Mapping/
  Infrastructure/
    TableStorage/
      Configuration/
      Client/
      Tables/
        Prompts/
        PromptVotes/
        TagIndex/
        PublicPromptsNewestIndex/
        PublicPromptsMostLikedIndex/
        TitleSearchIndex/
        TagCatalog/
      Entities/
      Mapping/
      Pagination/
      Concurrency/
      Retry/
    DI/
  Components/
    Pages/
    ...
```

### Notes
- `Infrastructure/TableStorage/Entities` contains the 1:1 row entities (Azure Table SDK shapes).
- `Application/Models` contains app-facing models (what UI and features use).
- `Application/Features` contains CQRS handlers (commands for writes, queries for reads).
- `Application/Abstractions/Persistence` contains interfaces that features depend on.
- `Infrastructure/DI` contains extension methods to register storage services.

---

## 2) Table naming / configuration

Create options bound from configuration:

- `TableStorageOptions`
  - `ConnectionString`
  - `PromptsTableName` (default: `Prompts`)
  - `PromptVotesTableName` (default: `PromptVotes`)
  - `TagIndexTableName` (default: `TagIndex`)
  - `PublicPromptsNewestIndexTableName` (default: `PublicPromptsNewestIndex`)
  - `PublicPromptsMostLikedIndexTableName` (default: `PublicPromptsMostLikedIndex`)
  - `TitleSearchIndexTableName` (default: `TitleSearchIndex`)
  - `TagCatalogTableName` (default: `TagCatalog`)

Plan: keep names configurable to support dev/prod separation and allow table suffixing.

---

## 3) Storage entities (1:1 with tables)

Implement one storage entity per table. These are “database entities” and should live under:

- `Infrastructure/TableStorage/Entities/`

Suggested naming convention: `*Entity` for Table Storage rows.

### `Prompts` table
- `PromptEntity`
  - `PartitionKey = "u|{AuthorId}"`
  - `RowKey = "{PromptId}"`
  - `PromptId`, `AuthorId`, `Title`, `TitleNormalized`, `PromptText`, `Tags`, `Visibility`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `Likes`, `Dislikes`, plus Azure `Timestamp`, `ETag`.

### `PromptVotes` table
- `PromptVoteEntity`
  - `PartitionKey = "p|{PromptId}"`
  - `RowKey = "u|{VoterId}"`
  - `PromptId`, `VoterId`, `VoteValue`, `UpdatedAt`, `Timestamp`, `ETag`.

### `TagIndex` table
- `TagIndexEntity`
  - `PartitionKey = "t|{Tag}"`
  - `RowKey = "{PromptId}"`
  - minimal denormalized fields: `Tag`, `PromptId`, `AuthorId`, `Visibility`, `CreatedAt`, `Likes`, `Dislikes`, `IsDeleted`.

### `PublicPromptsNewestIndex` table
- `PublicPromptsNewestIndexEntity`
  - `PartitionKey = "pub|newest|{yyyyMM}"`
  - `RowKey = "{CreatedAtTicksDesc}|{PromptId}"`
  - denormalized fields for list display.

### `PublicPromptsMostLikedIndex` table
- `PublicPromptsMostLikedIndexEntity`
  - `PartitionKey = "pub|liked|{ScoreBucket}"`
  - `RowKey = "{ScoreDesc}|{CreatedAtTicksDesc}|{PromptId}"`
  - `Score` plus denormalized fields.

### `TitleSearchIndex` table (optional but recommended)
- `TitleSearchIndexEntity`
  - `PartitionKey = "q|{Token}"`
  - `RowKey = "{PromptId}"`
  - `Token`, `PromptId`, `Visibility`, `AuthorId`, `IsDeleted`, `CreatedAt`.

### `TagCatalog` table
- `TagCatalogEntity`
  - `PartitionKey = "tagcatalog"`
  - `RowKey = "{Tag}"`
  - `Tag`, `IsActive`, optional `DisplayName`, `SortOrder`.

---

## 4) Application models (UI/feature-facing)

Models should be storage-agnostic and live under:

- `Application/Models/`

Suggested models (minimal MVP):

- `Prompt`
  - `PromptId`, `AuthorId`, `Title`, `PromptText`, `Tags` (as `IReadOnlyList<string>`), `Visibility`, `CreatedAt`, `UpdatedAt`, `Likes`, `Dislikes`.
- `PromptSummary`
  - used for lists (public newest/most liked, tag listings).
- `VoteState`
  - `PromptId`, `VoterId`, `VoteValue`, `UpdatedAt`.
- `TagCatalogItem`
  - `Tag`, `DisplayName?`, `IsActive`, `SortOrder?`.

---

## 5) Mapping strategy

Because storage entities are 1:1 with tables while application models are domain-oriented:

- Place mapping helpers in:
  - `Application/Mapping/` (app-side normalization helpers)
  - `Infrastructure/TableStorage/Mapping/` (entity ↔ model and entity ↔ index mappings)

Key mapping responsibilities:
- Normalize title:
  - `TitleNormalized = Title.Trim().ToLowerInvariant()`
- Normalize tags:
  - per decisions: lower-case, validate against TagCatalog, max 10.
  - store display string as `"tag-a;tag-b"`
  - app model should expose tags as list; mapper converts list ↔ delimited string.
- Compute keys and index rows consistently:
  - helpers for partition/row key formats (centralized).

---

## 6) Persistence abstractions (Application layer)

Create interfaces in `Application/Abstractions/Persistence/`.

Keep them feature-focused (CQRS-friendly), but still allow reuse.

Suggested interfaces:

- `IPromptWriteStore`
  - `CreateAsync(Prompt prompt, CancellationToken ct)`
  - `UpdateAsync(Prompt prompt, string expectedETag, CancellationToken ct)` (or include ETag on model)
  - `SoftDeleteAsync(string authorId, string promptId, CancellationToken ct)`
  - (internally responsible for synchronously updating index tables)

- `IPromptReadStore`
  - `GetByIdForAuthorAsync(string authorId, string promptId, CancellationToken ct)`
  - `GetPublicByIdAsync(string promptId, CancellationToken ct)` (enforces visibility)
  - `ListMyPromptsAsync(string authorId, ContinuationToken? token, int pageSize, CancellationToken ct)`
  - `ListPublicNewestAsync(YearMonthBucket startBucket, ContinuationToken? token, int pageSize, CancellationToken ct)`
  - `ListPublicMostLikedAsync(ScoreBucket startBucket, ContinuationToken? token, int pageSize, CancellationToken ct)`

- `IVoteStore`
  - `UpsertVoteAsync(promptId, voterId, voteValue, CancellationToken ct)`
  - `GetVoteAsync(promptId, voterId, CancellationToken ct)`

- `ITagCatalogStore`
  - `GetActiveTagsAsync(CancellationToken ct)`

Where `ContinuationToken` is an app-friendly wrapper (see Pagination section).

---

## 7) Infrastructure: Table storage client + per-table wrappers

### Client factory
Location: `Infrastructure/TableStorage/Client/`

- `ITableServiceClientFactory`
  - `TableServiceClient Create()`
- Implementation reads `TableStorageOptions`.

### Per-table wrapper pattern
Location: `Infrastructure/TableStorage/Tables/<TableName>/`

Each wrapper owns:
- the `TableClient` instance (created from shared `TableServiceClient`)
- table-specific query helpers
- table-specific CRUD methods (thin)

Example wrappers:
- `PromptsTable`
- `PromptVotesTable`
- `TagIndexTable`
- `PublicPromptsNewestIndexTable`
- `PublicPromptsMostLikedIndexTable`
- `TitleSearchIndexTable`
- `TagCatalogTable`

The write store(s) orchestrate multi-table updates; wrappers remain simple.

---

## 8) CQRS feature plan (Application/Features)

### Prompts — commands
- `CreatePromptCommand` → `CreatePromptHandler`
  - Validates title length, prompt text length, tags <= 10
  - Validates tags exist in TagCatalog
  - Creates `PromptId` (ULID)
  - Writes `Prompts` row
  - Writes `TagIndex` rows
  - If public: writes `PublicPromptsNewestIndex` (+ `PublicPromptsMostLikedIndex` with computed initial score)
  - Optionally writes `TitleSearchIndex`

- `UpdatePromptCommand` → `UpdatePromptHandler`
  - Uses optimistic concurrency with `ETag` (provided by read)
  - Updates `Prompts`
  - Computes tag diffs → add/remove `TagIndex` rows
  - Handles visibility changes:
    - public→private: delete public index rows
    - private→public: insert public index rows
  - Updates title search index tokens if enabled

- `DeletePromptCommand` → `DeletePromptHandler`
  - Soft delete in `Prompts` (`IsDeleted=true`)
  - Remove from public indexes
  - Remove all `TagIndex` rows for prompt (preferred)
  - Remove `TitleSearchIndex` rows for prompt (if enabled)

### Votes — commands
- `UpsertVoteCommand` → `UpsertVoteHandler`
  - Upsert row in `PromptVotes`
  - Update aggregate counts in `Prompts` using ETag retry
  - Update public indexes synchronously (MVP):
    - update denormalized Likes/Dislikes in newest index
    - recompute score and move most-liked row if key changes (delete old, insert new)

### Read queries (repositories/handlers)
- `GetPromptQuery` (author/private aware)
- `ListMyPromptsQuery`
- `ListPublicNewestQuery` (bucket iteration + continuation token)
- `ListPublicMostLikedQuery` (score buckets)
- `SearchPublicByTitleQuery` (MVP strategy below)
- `FilterPublicByTagsQuery` (AND semantics)

---

## 9) Query patterns: pagination + AND tags + title search (MVP)

### Pagination primitives
Create in `Infrastructure/TableStorage/Pagination/`:
- `ContinuationPage<T>`: `Items`, `ContinuationToken?`
- `TableContinuationToken` wrapper that can store:
  - raw Table SDK continuation values (NextPartitionKey/NextRowKey) OR
  - a serialized token string

Also support **bucket iteration** for:
- public newest: month buckets (`yyyyMM`)
- most liked: score buckets (`0000`..`9999` or chosen max)

Implementation detail:
- token should carry both:
  - current bucket id
  - SDK continuation for that bucket
  - so next page can continue within bucket, then move to next bucket when empty.

### AND tag filtering
From the plan: `TagIndex` supports AND by intersecting prompt IDs across tag partitions.

MVP approach:
1. Query each selected tag partition (`t|{tag}`) for `PromptId` rows.
2. Intersect `PromptId`s in-memory.
3. Hydrate prompt summaries:
   - Option A (fastest to implement): read from `Prompts` by author is not possible for public; so use a public index (newest) to hydrate, or denormalize enough fields in `TagIndex` for list rendering.
   - Given the schema already denormalizes list-relevant fields in `TagIndex`, prefer using `TagIndex` rows as the summary source.

Pagination caveat:
- true continuation-token pagination over intersections is non-trivial.
- MVP acceptable behavior:
  - limit to small tag result sets by page size cap per tag
  - document that deep pagination for multi-tag AND is best-effort.

### Title search (contains-ish)
Per unresolved issues: pure contains requires scan.

MVP recommend (already in `4-db-plan.md`): `TitleSearchIndex` tokenization.
- Normalize title to `TitleNormalized`.
- Tokenize to words and optional prefixes (3–5 chars).
- Store `q|{token}` → `PromptId` rows.

Query:
- Tokenize user input similarly.
- Fetch candidate PromptIds from partitions (intersection optional for multi-token search)
- Hydrate summaries.

If you want a literal `contains` fallback for early MVP, restrict it:
- only search within last N public prompts (e.g., newest buckets for last 1–2 months), then filter in memory.

---

## 10) Concurrency and retries

Location: `Infrastructure/TableStorage/Concurrency/` and `Infrastructure/TableStorage/Retry/`.

### ETag strategy
- Reads return entities with `ETag`.
- Writes use `UpdateEntityAsync(entity, etag, TableUpdateMode.Replace)`.

### Retry strategy
- Apply exponential backoff on transient failures and 429.
- For aggregate updates (likes/dislikes), implement bounded retry loop:
  1. read prompt entity
  2. compute new counts
  3. try update with ETag
  4. retry on conflict

Keep retry logic centralized:
- `TableRetryPolicy.ExecuteAsync(...)`

---

## 11) Dependency injection

Add DI registration extension in `Infrastructure/DI/`:
- `AddTableStorage(this IServiceCollection, IConfiguration)`
  - binds `TableStorageOptions`
  - registers `ITableServiceClientFactory` singleton
  - registers per-table wrappers as scoped
  - registers stores (`IPromptReadStore`, `IPromptWriteStore`, etc.) as scoped

---

## 12) Unit tests (in-memory fakes)

Project: `PromptHub.Web.UnitTests`.

Strategy:
- Unit test feature handlers (commands/queries) with in-memory implementations of:
  - `IPromptReadStore` / `IPromptWriteStore`
  - `IVoteStore`
  - `ITagCatalogStore`

Recommended test focus:
- Tag normalization + validation
- Visibility rules enforced by reads
- Soft delete removes from user-facing queries
- Vote transitions update aggregates correctly (like→none, dislike→like, etc.)
- Index update logic called (can be asserted via fake store state)

---

## 13) Implementation sequence (small, safe steps)

1. Add `TableStorageOptions` + DI wiring skeleton.
2. Add Table client factory + per-table wrappers (no features yet).
3. Implement storage entities + mapping utilities (keys, normalization).
4. Implement read stores first (My Prompts, Public Newest).
5. Implement `CreatePrompt` write workflow (including TagIndex + public indexes).
6. Implement vote workflow with optimistic concurrency + most-liked index move.
7. Add optional TitleSearchIndex.
8. Add unit tests and in-memory fakes for handlers.

---

## Appendix: key format helpers

Centralize these in Infrastructure (single source of truth):

- `Prompts.PartitionKey = "u|{AuthorId}"`
- `PromptVotes.PartitionKey = "p|{PromptId}"` / `RowKey = "u|{VoterId}"`
- `TagIndex.PartitionKey = "t|{Tag}"`
- `PublicPromptsNewestIndex.PartitionKey = "pub|newest|{yyyyMM}"`
- `PublicPromptsNewestIndex.RowKey = "{CreatedAtTicksDesc}|{PromptId}"`
- `PublicPromptsMostLikedIndex.PartitionKey = "pub|liked|{ScoreBucket}"`
- `PublicPromptsMostLikedIndex.RowKey = "{ScoreDesc}|{CreatedAtTicksDesc}|{PromptId}"`
- `TitleSearchIndex.PartitionKey = "q|{Token}"`
- `TagCatalog.PartitionKey = "tagcatalog"`

