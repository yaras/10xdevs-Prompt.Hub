<conversation_summary>

<decisions>

1. Use Azure Table Storage for persistence and (for simplicity) keep `Prompt` data in a single main table (no metadata/content split).
2. Public catalog must support ordering by `newest` and `most liked`.
3. Title search can be `contains` search for MVP.
4. Tag filtering must use `AND` semantics (prompt must contain all selected tags).
5. Soft-deleted prompts become invisible to all users but remain stored in the database.
6. Use `ULID` as `PromptId`.
7. Voting aggregates can be eventually consistent.
8. Expected scale: up to ~100 prompts per user; average prompt text size up to ~2000 characters.
9. Store tags on prompt as a normalized, lower-case, delimited string for display; implement tag filtering via an index table keyed by tag.

</decisions>

<matched_recommendations>

1. Encode list sort order into keys for efficient pagination: since Public catalog needs `newest` and `most liked`, plan dedicated query shapes/indexes so Table Storage can return results in the desired order without scans (e.g., time-ordered keys for newest; separate “most-liked” index/materialized view for ranking).
2. Title search in Table Storage: “contains” search is not natively indexable; for MVP, implement a dedicated title search index (token/prefix-based) or accept constrained/approximate behavior and document it explicitly.
3. Tag filtering as an index: store tags for display on the prompt entity, but query by tags through a `Tag -> PromptId` index table to avoid scanning prompt rows.
4. Soft delete handling: keep `IsDeleted` in entities but ensure indexes exclude deleted prompts (remove/skip index rows when `IsDeleted=true`) so user-facing queries never need broad reads.
5. Vote modeling: store per-user vote state in a dedicated votes table keyed by (`PromptId`,`VoterId`) and allow eventual consistency for aggregates; update aggregate counts with optimistic concurrency and retry.
6. Partition/hotspot awareness: even at MVP scale, avoid a single hot “public” partition if implementing public listing; consider time-bucketed or sharded partitions to distribute load while supporting newest-first.

</matched_recommendations>

<database_planning_summary>

### a) Main requirements for the database schema

- Storage backend is **Azure Table Storage**.
- Core MVP objects to persist:
  - **Prompts** with: `PromptId` (ULID), `Title`, `PromptText`, `Tags` (lower-case delimited), `Visibility` (private/public), `AuthorId`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `Likes`, `Dislikes`.
  - **Votes** as per-user state: `PromptId`, `VoterId`, `VoteValue` (like/dislike/none), `UpdatedAt`.
  - **Tag catalog** is predefined from configuration (not user-generated).
- Lists must be **pagination-ready** using continuation tokens.
- Public catalog must support two sorts: **newest** and **most liked**.
- Search requirements:
  - Title search for MVP can be **contains**.
  - Tag filtering must support **AND** across selected tags.
- Deletion is **soft delete** (`IsDeleted=true`) and deleted prompts must be invisible to all user-facing queries.

### b) Key entities and their relationships

- `Prompt` (1) — (many) `Vote` relationship:
  - Each prompt can have many votes.
  - Each (prompt, user) pair has at most one active vote state.
- `Prompt` — tags:
  - Tags are stored on the prompt for display.
  - Querying/filtering by tags is done via a **tag index table** keyed by `Tag` that maps to `PromptId` (and may store denormalized fields needed for list display).
- Tag catalog is a configuration-driven allowed list; prompts may reference only tags from that list.

### c) Important security and scalability concerns

- Table Storage has no native row-level security; authorization must be enforced by the application layer (Entra ID identity/roles).
- Visibility rules still matter at query time:
  - Private prompts must not appear in public catalog.
  - Public prompts appear in the public catalog.
  - Deleted prompts appear in no user-facing query.
- Performance/scalability considerations:
  - Avoid table scans for public listing, title search, and tag filtering (use targeted partitions and index tables/materialized views).
  - `most liked` ordering generally requires a separate index/materialized view because Table Storage cannot efficiently sort by arbitrary properties.
  - `contains` title search is not directly supported; MVP must either accept a constrained implementation or introduce an index strategy.
  - Voting totals are allowed to be eventually consistent; design should tolerate concurrent updates and retries.

### d) Unresolved items / clarification needed for schema finalization

- Exact approach for **most-liked ranking** (materialized index table design, update strategy, and tie-breaking).
- Definition/acceptance criteria for **contains title search** in Table Storage (tokenization rules, normalization, minimum query length, and whether approximate matching is acceptable).
- Concrete strategy for **AND tag filtering** (client-side intersection of multiple tag index queries vs. a dedicated compound index strategy), including pagination behavior under AND semantics.
- Partition key strategy for public listing (single partition vs time-bucketed/sharded) and expected query patterns for “My prompts”.

</database_planning_summary>

<unresolved_issues>

1. “Contains” title search: Table Storage can’t do efficient contains queries without scanning; need a specific MVP approach (token index, n-grams, or UX constraint).
2. “Most liked” ordering: requires a separate index/materialized view or periodic recomputation; define update mechanics and acceptable staleness.
3. AND tag filtering: must define whether results are computed by intersecting per-tag index queries (and how pagination/continuations will work).
4. Partitioning strategy for public reads/writes: confirm whether a single public partition is acceptable or if sharding/time-bucketing is required.

</unresolved_issues>

</conversation_summary>
