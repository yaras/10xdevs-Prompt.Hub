# Voting implementation plan (MVP)

## Goal
Enable authenticated users to like/dislike prompts from the **Public prompts** list using `PromptCard`, while enforcing:

- One vote per user per prompt
- Toggle/switch behavior:
  - Like -> Like again = None
  - Dislike -> Dislike again = None
  - Like -> Dislike = switch
  - Dislike -> Like = switch
- Self-voting allowed
- UI is optimistic and shows per-user selection state
- Errors are surfaced via `ISnackbar` at the page level

This plan keeps `PromptCard` **UI-only** and pushes vote rules and persistence into the application layer.

---

## Decisions (confirmed)
- Voting is available **only** in Public prompts.
- `PromptCard` exposes `EventCallback` and does not call stores/services directly.
- Toggle logic lives in an application feature/service.
- Details opening remains a dedicated button (already implemented).

---

## 1) Data contract changes
### 1.1 Extend `PromptSummaryModel` to carry per-user vote state
Add a property representing the current viewer’s vote for the prompt:

- `VoteValue UserVote` (default `VoteValue.None`)

Rationale:
- Allows `PromptCard` to render highlighted selection.
- Allows optimistic UI updates without extra lookup structures.

If `PromptSummaryModel` is used broadly, keep the new property optional and default-safe.

---

## 2) UI changes
### 2.1 Update `PromptCard`
Add parameters and UI behavior:

- `bool ShowVoting` (default `false` to avoid accidental voting in other pages)
- `EventCallback<VoteRequest> OnVote`
- Display like/dislike as clickable buttons when `ShowVoting == true`.
- Visual state:
  - Like button: highlighted/filled when `Prompt.UserVote == Like`
  - Dislike button: highlighted/filled when `Prompt.UserVote == Dislike`

Important:
- Ensure vote button clicks do not invoke view/edit.

### 2.2 Update `PublicPrompts` page
- Pass `ShowVoting="true"` and `OnVote="HandleVoteAsync"` into `PromptCard`.
- Resolve current user id (`oid`) once via `AuthenticationStateProvider`.
- Implement optimistic UI update on vote:
  - Capture prior vote + counts
  - Apply local count deltas coherently
  - Call application feature
  - On failure: revert + show snackbar

---

## 3) Application layer: voting feature
Create an application feature that:

- Resolves voter id (passed in from UI) and validates inputs.
- Loads existing vote state (optional / can be upsert-only if store returns prior).
- Computes new vote state based on existing and requested action.
- Persists user vote state in `PromptVotes`.
- Updates aggregates in canonical `Prompts` table with optimistic concurrency + retry.

Suggested API:
- `IPromptVotingFeature`
  - `Task<VoteResult> VoteAsync(VoteCommand command, CancellationToken ct)`

Where:
- `VoteCommand` includes `PromptId`, `VoterId`, `VoteValue Requested`.
- `VoteResult` includes `VoteValue NewVote`, `int Likes`, `int Dislikes` (authoritative counts after update).

Note: Public indexes are eventually consistent; the page can continue using its local updated values and refresh later.

---

## 4) Persistence layer
### 4.1 Ensure `IVoteStore` is implemented
`IVoteStore` already exists. Confirm there is an Azure Table implementation (e.g., `TableVoteStore`) that:
- Gets vote by (promptId, voterId)
- Upserts vote

### 4.2 Update prompt aggregates
Ensure there is a write API capable of updating likes/dislikes safely:
- Option A: add method to `IPromptWriteStore`:
  - `Task<PromptAggregatesModel> ApplyVoteDeltaAsync(authorId, promptId, deltaLikes, deltaDislikes, ct)` with ETag retry
- Option B: add dedicated persistence abstraction for aggregates to avoid leaking author partition rules.

Because `Prompts` table is partitioned by `u|{AuthorId}`, you must be able to locate the prompt row. The public listing already carries `AuthorId`, so the feature can accept `AuthorId` as part of its command or re-hydrate prompt metadata by id.

Recommended MVP command shape:
- Include `AuthorId` in `VoteCommand` from `PromptSummaryModel.AuthorId`.

---

## 5) Error handling and resilience
- Retry policy for Table operations on 429 with exponential backoff (where policy is centralized today).
- UI shows a generic snackbar error message (no stack traces).
- Keep list visible if vote fails.

---

## 6) Testing
### 6.1 Unit tests (xUnit + FluentAssertions)
For the application feature:
- Like from None -> Like increments likes
- Like from Like -> None decrements likes
- Dislike from None -> Dislike increments dislikes
- Dislike from Dislike -> None decrements dislikes
- Switch Like -> Dislike adjusts both counts
- Switch Dislike -> Like adjusts both counts
- Self voting allowed (no special casing)

Mock:
- `IVoteStore`
- prompt aggregate store / `IPromptWriteStore`

### 6.2 Component tests (bUnit) (optional)
- `PromptCard` renders highlighted state based on `UserVote`
- Clicking like/dislike triggers `OnVote` callback with correct payload

---

## Files (expected)
- `PromptHub.Web/Components/Prompts/PromptCard.razor`
- `PromptHub.Web/Components/Prompts/PromptCard.razor.cs`
- `PromptHub.Web/Components/Pages/PublicPrompts.razor`
- `PromptHub.Web/Components/Pages/PublicPrompts.razor.cs`
- `PromptHub.Web/Application/Features/Votes/IPromptVotingFeature.cs` (new)
- `PromptHub.Web/Application/Features/Votes/PromptVotingFeature.cs` (new)
- `PromptHub.Web/Application/Models/Votes/VoteCommand.cs` + `VoteResult.cs` (new or colocated)
- Persistence implementation additions as needed
- `PromptHub.Web.UnitTests` new tests for vote feature

---

## Acceptance criteria
- Public prompts list shows like/dislike buttons that reflect the user’s current vote.
- Clicking like/dislike updates counts immediately and highlights selection.
- Vote rules (toggle/switch) behave as specified.
- Only one vote per user per prompt is enforced in storage (`PromptVotes`).
- Errors show via snackbar and do not break the list.
