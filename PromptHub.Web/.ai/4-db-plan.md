# PromptHub Azure Table Storage schema plan (MVP)

This schema is designed for **Azure Table Storage** (NoSQL). “Tables” below refer to **Azure Table Storage tables**, and “columns” are entity properties on each row.

> Conventions
> - Every entity includes `PartitionKey` (string), `RowKey` (string), `Timestamp` (DateTimeOffset, server-managed), and `ETag` (string, for optimistic concurrency).
> - `PromptId` is a **ULID** stored as a **string** (26 chars).
> - `UserId` is the **stable Entra user identifier** (typically `oid` claim) stored as string.
> - Dates are stored as `DateTimeOffset`.
> - All tags are stored **lower-case**.

---

## 1) Core tables with their columns, data types, and constraints

### Table: `Prompts`
Stores the canonical prompt record, including its metadata, tags, visibility, and vote aggregates.

**Keys**
- `PartitionKey` (string, required): `u|{AuthorId}`
- `RowKey` (string, required): `{PromptId}`

**Columns**
- `PromptId` (string, required, ULID) — must equal `RowKey`
- `AuthorId` (string, required) — must equal value encoded in `PartitionKey`
- `Title` (string, required, max 200)
- `TitleNormalized` (string, required) — lower-case, trimmed (used for constrained search)
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
- Tag count `<= 10`; tags must come from the approved catalog (managed via configuration or a lightweight store)
- `IsDeleted=true` rows are excluded from all user-facing queries

---

### Table: `PromptVotes`
Per-user vote state for each prompt; one row per (`PromptId`,`VoterId`) pair.

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

### Table: `PublicPromptsNewestIndex`
Materialized view for the **Public catalog sorted by newest first** so the UI can paginate without scanning the main `Prompts` table. This table carries denormalized prompt metadata required for public listing cards.

**Keys (time-bucketed to reduce hot partitions)**
- `PartitionKey` (string, required): `pub|newest|{yyyyMM}` (month bucket from `CreatedAt`)
- `RowKey` (string, required): `{CreatedAtTicksDesc}|{PromptId}`
  - `CreatedAtTicksDesc` = zero-padded 19-digit string of `(DateTimeOffset.MaxValue.UtcTicks - CreatedAt.UtcTicks)` so lexicographic order == newest-first.

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
- Includes only prompts where `Visibility="public"` and `IsDeleted=false`
- On soft delete or visibility change to private: remove the corresponding index row

---

## 2) Relationships between tables

- `Prompts` (1) ? (many) `PromptVotes`
  - `PromptVotes.PartitionKey = p|{PromptId}` references `Prompts.RowKey = {PromptId}`

- `Prompts` (public subset) ? `PublicPromptsNewestIndex`
  - One index row per public, not-deleted prompt; this row holds the metadata necessary for public listings.

- Tag filtering applies AND semantics by intersecting prompt rows that contain every selected tag in their `Tags` column (the index rows already mirror those tags).

- Title search runs constrained queries after loading the next chunk of `PublicPromptsNewestIndex`, filtering in memory using the normalized title.

---

## 3) Additional notes / design decisions

- **Partitioning strategy (public reads/writes):** bucket the newest index (`yyyyMM`) to avoid a single hot partition while keeping continuation tokens focused.
- **Pagination:** Always paginate using continuation tokens. When exhausting a bucket, move to the previous month while carrying the continuation state.
- **Soft delete:** treat `IsDeleted=true` as tombstoned. User-facing queries should skip those rows and remove them from the newest index immediately.
- **Voting aggregates:** `Prompts.Likes`/`Dislikes` are updated with optimistic concurrency (ETag) and retries; public listings may lag slightly but will update in subsequent writes.
- **Security:** enforce access rules at the application layer:
  - Public catalog only reads `PublicPromptsNewestIndex`.
  - My prompts queries `Prompts` using the `u|{AuthorId}` partition.
  - Direct fetches check `Visibility` and `AuthorId` before responding.
- **Tag catalog operations (operational):** manage the allowed tag list via configuration or a lightweight seed process (optional table) and use it for validation in prompt workflows and AI suggestions.
