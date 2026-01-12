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
    Mapping/
  Infrastructure/
    TableStorage/
      Configuration/
      Client/
      Tables/
        Prompts/
        PromptVotes/
        PublicPromptsNewestIndex/
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

Bind a single `TableStorageOptions` section and expose the tables that exist today:

- `TableStorageOptions`
  - `ConnectionString`
  - `PromptsTableName` (default: `Prompts`)
  - `PromptVotesTableName` (default: `PromptVotes`)
  - `PublicPromptsNewestIndexTableName` (default: `PublicPromptsNewestIndex`)

Plan: keep the names configurable to support dev/prod separation and allow table suffixing.

---

## 3) Storage entities (1:1 with tables)

Implement one storage entity per table. These are “database entities” and should live under `Infrastructure/TableStorage/Entities/`.

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

### `PublicPromptsNewestIndex` table
- `PublicPromptsNewestIndexEntity`
  - `PartitionKey = "pub|newest|{yyyyMM}"`
  - `RowKey = "{CreatedAtTicksDesc}|{PromptId}"`
  - Denormalized fields for list display (title, tags, author metadata, likes/dislikes, timestamps, updated at, etc.).

---

## 4) Application models (UI/feature-facing)

Models should be storage-agnostic and live under `Application/Models/`.

Suggested models (minimal MVP):

- `Prompt`
  - `PromptId`, `AuthorId`, `Title`, `PromptText`, `Tags` (as `IReadOnlyList<string>`), `Visibility`, `CreatedAt`, `UpdatedAt`, `Likes`, `Dislikes`, `ETag`.
- `PromptSummary`
  - Used by public listing; contains subset of fields plus aggregated counts and `AuthorEmail` for display.
- `VoteState`
  - `PromptId`, `VoterId`, `VoteValue`, `UpdatedAt`, `ETag`.
- `TagCatalogItem`
  - `Tag`, `DisplayName?`, `IsActive`, `SortOrder?` (data-driven allowed list sourced from configuration or a lightweight seed process).

---

## 5) Mapping strategy

Because storage entities are 1:1 with tables while application models are domain-oriented:

- Place mapping helpers in:
  - `Application/Mapping/` (app-side normalization helpers)
  - `Infrastructure/TableStorage/Mapping/` (entity ↔ model and index mappings)

Key mapping responsibilities:
- Normalize title:
  - `TitleNormalized = Title.Trim().ToLowerInvariant()`
- Normalize tags:
  - per decisions: lower-case, validate against the approved tag catalog, max 10.
  - store display string as `"tag-a;tag-b"`
  - app model exposes tags as list; mapper converts list ↔ delimited string.
- Compute keys consistently for the newest index and prompt partitions.

---

## 6) Persistence abstractions (Application layer)

Create interfaces in `Application/Abstractions/Persistence/` tailored to the flows built so far.

- `IPromptWriteStore`
  - `CreateAsync(Prompt prompt, CancellationToken ct)`
  - `UpdateAsync(Prompt prompt, string expectedETag, CancellationToken ct)`
  - `SoftDeleteAsync(string authorId, string promptId, CancellationToken ct)`
  - (internally responsible for synchronously updating `PublicPromptsNewestIndex` when visibility/timestamps change)

- `IPromptReadStore`
  - `GetByIdForAuthorAsync(string authorId, string promptId, CancellationToken ct)`
  - `GetPublicByIdAsync(string promptId, CancellationToken ct)` (enforces visibility)
  - `ListMyPromptsAsync(string authorId, ContinuationToken? token, int pageSize, CancellationToken ct)`
  - `ListPublicNewestAsync(YearMonthBucket startBucket, ContinuationToken? token, int pageSize, CancellationToken ct)`

- `IVoteStore`
  - `GetVoteAsync(string promptId, string voterId, CancellationToken ct)`
  - `UpsertVoteAsync(string promptId, string voterId, VoteValue voteValue, CancellationToken ct)`

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
- `PublicPromptsNewestIndexTable`

The write store(s) orchestrate multi-table updates; wrappers remain simple.

---

## 8) CQRS feature plan (Application/Features)

### Prompts — commands
- `CreatePromptCommand` → `CreatePromptHandler`
  - Validates title length, prompt text length, tags <= 10.
  - Validates tags exist in the approved catalog.
  - Creates `PromptId` (ULID).
  - Writes `Prompts` row.
  - If public: upserts `PublicPromptsNewestIndex` row with denormalized metadata.

- `UpdatePromptCommand` → `UpdatePromptHandler`
  - Uses optimistic concurrency with `ETag`.
  - Updates `Prompts`.
  - Maintains `PublicPromptsNewestIndex`:
    - If visibility remains public: update the index row.
    - If visibility changes to private or `IsDeleted=true`: delete the index row.

- `DeletePromptCommand` → `DeletePromptHandler`
  - Soft delete in `Prompts` (`IsDeleted=true`).
  - Remove the corresponding row in `PublicPromptsNewestIndex` if it exists.

### Votes — commands
- `UpsertVoteCommand` → `UpsertVoteHandler`
  - Upsert row in `PromptVotes`.
  - Update aggregate counts in `Prompts` using ETag retry.
  - Public newest index may lag; the write path can optionally refresh the index (MVP keeps eventually consistent values).

### Read queries (repositories/handlers)
- `GetPromptQuery` (author/private aware).
- `ListMyPromptsQuery`.
- `ListPublicNewestQuery` (bucket iteration + continuation token).
- `SearchPublicByTitleQuery` (constraint-based search on the newest index data).
- `FilterPublicByTagsQuery` (AND semantics applied on the `Tags` column of the newest index rows).

---

## 9) Query patterns: pagination + filtering (MVP)

### Pagination primitives
Create in `Infrastructure/TableStorage/Pagination/`:
- `ContinuationPage<T>`: `Items`, `ContinuationToken?`
- `TableContinuationToken` wrapper that stores:
  - raw Table SDK continuation values (NextPartitionKey/NextRowKey)
  - serialized state describing the current bucket

Also support **bucket iteration** for the newest index (`yyyyMM`).
Implementation detail:
- The token carries the current month partition and Table SDK continuation so the UI can keep fetching until the current bucket is exhausted, then move to earlier months.

### Tag filtering
Apply strict AND semantics directly on the `Tags` column of rows returned by `PublicPromptsNewestIndex`:
1. Fetch a page from the newest index.
2. Retain only prompts whose `Tags` string contains all selected normalized tags.
3. Repeat paging until enough matches or the listing ends.

Document that deep filtering may require loading additional pages and is intentionally low-complexity for MVP.

### Title search (contains-ish)
Constrained title search runs in-memory on the prompt titles returned from the newest index page:
- Normalize the query (trim + lower-case).
- Filter the fetched rows by checking if `TitleNormalized` contains the search token.
- Require a minimum query length (e.g., `>= 3`) to keep the search fast.

If more results are desired, the UI will continue loading pages and applying the filter client-side.

---

## 10) Concurrency and retries

Location: `Infrastructure/TableStorage/Concurrency/` and `Infrastructure/TableStorage/Retry/`.

### ETag strategy
- Reads return entities with `ETag`.
- Writes use `UpdateEntityAsync(entity, etag, TableUpdateMode.Replace)`.

### Retry strategy
- Apply exponential backoff on transient failures and 429.
- For aggregate updates (likes/dislikes), implement a bounded retry loop:
  1. read prompt entity
  2. compute new counts
  3. try update with ETag
  4. retry on conflict

Keep retry logic centralized via `TableRetryPolicy.ExecuteAsync(...)`.

---

## 11) Dependency injection

Add DI registration extension in `Infrastructure/DI/`:
- `AddTableStorage(this IServiceCollection, IConfiguration)`
  - binds `TableStorageOptions`
  - registers `ITableServiceClientFactory` singleton
  - registers per-table wrappers (`PromptsTable`, `PromptVotesTable`, `PublicPromptsNewestIndexTable`) as scoped
  - registers stores (`IPromptReadStore`, `IPromptWriteStore`, `IVoteStore`, `ITagCatalogStore`) as scoped

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
- Soft delete removes prompts from both the author list and the public newest index
- Vote transitions update aggregates correctly (like→none, dislike→like, etc.)
- Index updates invoked when prompts change visibility

---

## 13) Implementation sequence (small, safe steps)

1. Add `TableStorageOptions` + DI wiring skeleton.
2. Add Table client factory + per-table wrappers (Prompts, PromptVotes, PublicPromptsNewestIndex).
3. Implement storage entities + mapping utilities (keys, normalization).
4. Implement read stores first (My Prompts, Public Newest).
5. Implement `CreatePrompt` write workflow (including newest index maintenance).
6. Implement vote workflow with optimistic concurrency + prompt aggregate updates.
7. Add unit tests and in-memory fakes for handlers.

---

## Appendix: key format helpers

Centralize these in Infrastructure (single source of truth):

- `Prompts.PartitionKey = "u|{AuthorId}"`
- `PromptVotes.PartitionKey = "p|{PromptId}"` / `RowKey = "u|{VoterId}"`
- `PublicPromptsNewestIndex.PartitionKey = "pub|newest|{yyyyMM}"`
- `PublicPromptsNewestIndex.RowKey = "{CreatedAtTicksDesc}|{PromptId}"`

