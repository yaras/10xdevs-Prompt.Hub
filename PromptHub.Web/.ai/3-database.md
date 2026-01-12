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
9. Store tags on prompt as a normalized, lower-case, delimited string for display; implement tag filtering by intersecting prompt rows that contain the selected tags.

</decisions>

<matched_recommendations>

1. Use the `PublicPromptsNewestIndex` table for newest-first public listing so the UI can read ordered pages via continuation tokens instead of scanning the canonical `Prompts` table. Defer any future “most liked” ordering until there is a dedicated index or aggregated computation.
2. Title search is constrained: normalize the query, fetch only the next portion of the newest index, and apply the contains-esque filter in memory before showing results. Document the UX limitations (minimum query length, paginated search).
3. Tag filtering observes strict AND semantics by intersecting tags stored on the rows returned from the newest index before presenting them. This keeps the index simple and avoids extra Table Storage tables.
4. Soft delete handling still keeps `IsDeleted` on `Prompts` while ensuring user-facing queries skip those rows and the newest index only tracks active public prompts.
5. Vote modeling uses a dedicated `PromptVotes` table keyed by (`PromptId`,`VoterId`) and updates aggregates in `Prompts` via optimistic concurrency with retry.
6. Partition/hotspot awareness favors bucketed partitions for the newest index (e.g., `pub|newest|{yyyyMM}`) so list queries cover a limited hotspot and can iterate backward in time as needed.

</matched_recommendations>

<database_planning_summary>

### a) Main requirements for the database schema

- Storage backend is **Azure Table Storage**.
- Core MVP objects to persist:
  - **Prompts** with: `PromptId` (ULID), `Title`, `PromptText`, `Tags` (lower-case delimited), `Visibility` (private/public), `AuthorId`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `Likes`, `Dislikes`.
  - **PromptVotes** per-user entries for (`PromptId`,`VoterId`) and `VoteValue`.
  - **PublicPromptsNewestIndex** exposing denormalized metadata (title, tags, timestamps, aggregates) for the public listing.
- Lists must be **pagination-ready** via continuation tokens inside bucketed partitions.
- Public catalog currently supports **newest** ordering; any “most liked” view is deferred until an additional index or aggregation strategy exists.
- Tag filtering and title search operate on the data surfaced by `PublicPromptsNewestIndex` and the `Posts.Tags`/`Title` columns, keeping extra tables out of the path.
- Deletion is **soft** (`IsDeleted=true`) and these rows are invisible to any user-facing query.

### b) Key entities and their relationships

- `Prompts` (1) ? (many) `PromptVotes`: each prompt can have many votes, and each (prompt, user) pair has at most one row in `PromptVotes`.
- `Prompts` ? `PublicPromptsNewestIndex`: one index row per public, non-deleted prompt; this table carries ordered rows for newest-first presentation.
- Tag filtering compares the `Tags` column on the index or prompt rows and retains only prompts containing all selected tags.
- Title search is applied after fetching the next page from `PublicPromptsNewestIndex` by matching normalized text on the driver side.

### c) Important security and scalability concerns

- Table Storage provides no row-level authorization, so the application must validate `AuthorId` + `Visibility` before returning data.
- Private prompts stay partitioned by `u|{AuthorId}` and never appear in public queries.
- Deleted prompts keep `IsDeleted=true` and are skipped in listings.
- Avoid table scans by relying on `PublicPromptsNewestIndex` and paginating within its bucketed partitions.
- Vote aggregates tolerate eventual consistency while the application continues retrying updates on the canonical `Prompts` row.

### d) Unresolved items / clarification needed for schema finalization

1. Define the UX for constrained title search (minimum query length, how “Load more” interacts with filtering, and what counts as an acceptable match set).
2. Confirm how tag filtering will paginate: how many pages must be scanned per request, and what caps must be documented.
3. Settle on the bucket strategy for `PublicPromptsNewestIndex` (e.g., monthly) so continuation tokens remain manageable and hotspots are avoided.

</database_planning_summary>

<unresolved_issues>

1. Title search constraints: determine how to balance user expectations with the reality that Table Storage cannot do arbitrary contains queries.
2. Tag filtering pagination: clarify how many pages and rows must be fetched to maintain newest ordering while respecting AND semantics.
3. Partitioning strategy for public listing: confirm the level of bucketing and whether fallback partitions are needed for very large catalogs.

</unresolved_issues>

</conversation_summary>
