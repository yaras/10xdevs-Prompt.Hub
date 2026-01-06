# PromptHub Azure Table Storage schema plan (MVP)

This schema is designed for **Azure Table Storage** (NoSQL). “Tables” below refer to **Azure Table Storage tables**, and “columns” are entity properties on each row.

> Conventions
> - Every entity includes `PartitionKey` (string), `RowKey` (string), `Timestamp` (DateTimeOffset, server-managed), and `ETag` (string, for optimistic concurrency).
> - `PromptId` is a **ULID** stored as a **string** (26 chars).
> - `UserId` is the **stable Entra user identifier** (typically `oid` claim) stored as string.
> - Dates are stored as `DateTimeOffset`.
> - All tags are stored **lower-case**.

---

## 1) List of tables with their columns, data types, and constraints

### Table: `Prompts`
Stores the canonical prompt record (single-table prompt storage per decision #1).

**Keys**
- `PartitionKey` (string, required): `u|{AuthorId}`
- `RowKey` (string, required): `{PromptId}`

**Columns**
- `PromptId` (string, required, ULID) — must equal `RowKey`
- `AuthorId` (string, required) — must equal value encoded in `PartitionKey`
- `Title` (string, required, max 200)
- `TitleNormalized` (string, required) — lower-case, trimmed (supports contains search in memory after constrained fetch; also feeds optional index)
- `PromptText` (string, required, max 3000)
- `Tags` (string, required) — normalized lower-case delimited string for display, e.g. `"tag-a;tag-b;tag-c"`
- `Visibility` (string, required) — enum: `"private" | "public"`
- `CreatedAt` (DateTimeOffset, required)
- `UpdatedAt` (DateTimeOffset, required)
- `IsDeleted` (bool, required, default false)
- `Likes` (int, required, default 0)
- `Dislikes` (int, required, default 0)

**Constraints / invariants (application enforced)**
- `Title.Length <= 200`, `PromptText.Length <= 3000`
- Tag count `<= 10`; tags must exist in the configured allowed tag catalog
- `IsDeleted=true` rows are excluded from all user-facing indexes/queries

---

### Table: `PromptVotes`
Per-user vote state for each prompt (one vote per user per prompt).

**Keys**
- `PartitionKey` (string, required): `p|{PromptId}`
- `RowKey` (string, required): `u|{VoterId}`

**Columns**
- `PromptId` (string, required, ULID) — must match `PartitionKey` suffix
- `VoterId` (string, required) — must match `RowKey` suffix
- `VoteValue` (int, required) — enum: `-1` (dislike), `0` (none), `1` (like)
- `UpdatedAt` (DateTimeOffset, required)

**Constraints / invariants**
- Exactly one row per (`PromptId`,`VoterId`)

---

### Table: `TagIndex`
Inverted index from tag -> prompts, used to support **AND tag filtering** without scanning the `Prompts` table.

**Keys**
- `PartitionKey` (string, required): `t|{Tag}`
- `RowKey` (string, required): `{PromptId}`

**Columns (minimal; denormalize for list rendering if desired)**
- `Tag` (string, required) — lower-case, must equal `PartitionKey` suffix
- `PromptId` (string, required, ULID)
- `AuthorId` (string, required)
- `Visibility` (string, required) — `"private" | "public"`
- `CreatedAt` (DateTimeOffset, required)
- `Likes` (int, required)
- `Dislikes` (int, required)
- `IsDeleted` (bool, required)

**Constraints / invariants**
- Maintain this table transactionally/compensating with prompt writes:
  - On create/update: upsert rows for current tags
  - On tag removal: delete rows for removed tags
  - On soft delete: delete all tag rows for the prompt (or mark `IsDeleted=true` and filter; deletion preferred to keep queries clean)
  - On visibility change: update affected tag rows

---

### Table: `PublicPromptsNewestIndex`
Materialized view for the **Public catalog sorted by newest first** with efficient pagination.

**Keys (time-bucketed to reduce hot partitions)**
- `PartitionKey` (string, required): `pub|newest|{yyyyMM}` (month bucket from `CreatedAt`)
- `RowKey` (string, required): `{CreatedAtTicksDesc}|{PromptId}`
  - `CreatedAtTicksDesc` is a zero-padded 19-digit string of `(DateTimeOffset.MaxValue.UtcTicks - CreatedAt.UtcTicks)` so lexicographic order == newest-first.

**Columns**
- `PromptId` (string, required, ULID)
- `AuthorId` (string, required)
- `Title` (string, required)
- `TitleNormalized` (string, required)
- `Tags` (string, required)
- `CreatedAt` (DateTimeOffset, required)
- `UpdatedAt` (DateTimeOffset, required)
- `Likes` (int, required)
- `Dislikes` (int, required)

**Constraints / invariants**
- Only includes prompts where `Visibility="public"` and `IsDeleted=false`
- On soft delete or visibility change to private: delete index row

---

### Table: `PublicPromptsMostLikedIndex`
Materialized view for the **Public catalog sorted by most liked** (eventually consistent is acceptable).

**Keys (bucketed to reduce hot partitions; score bucket allows manageable resort on updates)**
- `PartitionKey` (string, required): `pub|liked|{ScoreBucket}`
  - `ScoreBucket` is an integer string derived from likes score, e.g. `0000`..`9999` where higher bucket means more liked.
- `RowKey` (string, required): `{ScoreDesc}|{CreatedAtTicksDesc}|{PromptId}`
  - `ScoreDesc` is a zero-padded string of `(MaxScore - Score)` to sort descending
  - tie-breakers: newer first, then `PromptId`

**Columns**
- `PromptId` (string, required, ULID)
- `AuthorId` (string, required)
- `Title` (string, required)
- `TitleNormalized` (string, required)
- `Tags` (string, required)
- `CreatedAt` (DateTimeOffset, required)
- `Likes` (int, required)
- `Dislikes` (int, required)
- `Score` (int, required) — suggested: `Likes - Dislikes` (or simply `Likes` if preferred)

**Constraints / invariants**
- Only includes prompts where `Visibility="public"` and `IsDeleted=false`
- Updates are **eventually consistent**: on vote changes, recompute `Score` and move the row if the key changes (delete old + insert new)

---

### Table: `TitleSearchIndex` (optional but recommended)
Supports faster-than-scan title search in Table Storage while still enabling **contains-ish** UX via tokenization.

**Keys**
- `PartitionKey` (string, required): `q|{Token}`
- `RowKey` (string, required): `{PromptId}`

**Columns**
- `Token` (string, required) — lower-case
- `PromptId` (string, required, ULID)
- `Visibility` (string, required)
- `AuthorId` (string, required)
- `IsDeleted` (bool, required)
- `CreatedAt` (DateTimeOffset, required)

**Tokenization rule (application enforced)**
- From `TitleNormalized`, generate word tokens and optionally 3–5 char prefixes per token.
- Query by user input token/prefix, then hydrate prompt details by `PromptId`.

---

### Table: `TagCatalog`
Stores the predefined allowed tags (even if loaded from config, storing allows auditing and future admin UI).

**Keys**
- `PartitionKey` (string, required): `tagcatalog`
- `RowKey` (string, required): `{Tag}`

**Columns**
- `Tag` (string, required) — lower-case; must equal `RowKey`
- `IsActive` (bool, required, default true)
- `DisplayName` (string, optional) — if you want a nicer label; otherwise omit
- `SortOrder` (int, optional)

---

## 2) Relationships between tables

Since Azure Table Storage has no joins/foreign keys, relationships are **logical** and maintained by the application:

- `Prompts` (1) -> (many) `PromptVotes`
  - `PromptVotes.PartitionKey = p|{PromptId}` refers to `Prompts.RowKey = {PromptId}`

- `Prompts` (many-to-many) `Tags` via `TagIndex`
  - For each tag on a prompt, one row exists in `TagIndex` for that `{Tag, PromptId}` pair

- `Prompts` (public subset) -> `PublicPromptsNewestIndex`
  - One index row per public, not-deleted prompt for newest ordering

- `Prompts` (public subset) -> `PublicPromptsMostLikedIndex`
  - One index row per public, not-deleted prompt for most-liked ordering

- `Prompts.TitleNormalized` -> `TitleSearchIndex` (optional)
  - Many rows per prompt (one per token/prefix)

- `TagCatalog` constrains tag values used in `Prompts.Tags` and `TagIndex.Tag`

---

## 3) Additional notes / design decisions

- **Partitioning strategy (public reads/writes):** public listing tables are **bucketed** (`yyyyMM` for newest; score buckets for most liked) to avoid a single hot partition while preserving efficient ordered queries within a bucket.
- **Pagination:** All listing/index queries should use Table Storage continuation tokens. For bucketed partitions, pagination should iterate partitions in order (e.g., current month then previous months) while carrying continuation tokens per partition.
- **Soft delete:** treat `IsDeleted=true` as tombstoned. For user-facing queries, prefer **removing** rows from index tables on delete to eliminate accidental exposure and avoid filtering overhead.
- **Voting aggregates:** `Prompts.Likes`/`Dislikes` are aggregates updated with optimistic concurrency (ETag) and retry; indexes are updated asynchronously or in the same write path depending on desired consistency.
- **Security:** no row-level security in storage; enforce access rules in application services:
  - Public catalog uses only public indexes.
  - My prompts uses `Prompts` partition `u|{AuthorId}`.
  - Direct prompt fetch must check `Visibility` and `AuthorId` unless the request is for public content.
