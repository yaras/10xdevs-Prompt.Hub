# Product Requirements Document (PRD) - PromptHub

## 1. Product Overview

### 1.1 Summary
PromptHub is an internal, single-tenant web application that enables company members to store, share, discover, and reuse AI prompts. The product focuses on an MVP that supports prompt CRUD, visibility (private/public), search and tag-based filtering, voting (likes/dislikes), and AI-assisted tag suggestions.

### 1.2 Target users and context
- Primary users: individual members of Scrum teams (developers, testers, Scrum Masters) inside a single organization.
- Usage context: internal collaboration hub to share prompt ideas, reuse effective prompts, and reduce duplicated effort.

### 1.3 Platforms and delivery
- Web application accessible via modern browsers.
- Deployed to Azure App Service.

### 1.4 Authentication and access model
- No anonymous access; login is required before any application content is visible.
- Authentication uses **Microsoft Entra External ID** (CIAM) with social identity providers:
  - Microsoft personal accounts (Outlook.com / Live)
  - Google accounts (Gmail)
- Authorization is enforced globally (fallback policy): all application routes require an authenticated user.
- No mandatory app-role gating in MVP; any authenticated user can access the application.

### 1.5 Core data objects (MVP)
Prompt
- Required fields
  - Title
  - PromptText
  - Tags
- System fields
  - CreatedAt
  - UpdatedAt
  - AuthorId
  - AuthorEmail (optional; captured from identity claims when available)
  - IsDeleted (soft delete)
  - Likes (aggregate count)
  - Dislikes (aggregate count)

Vote (per-user state for a prompt)
- PromptId
- VoterId
- VoteValue (like/dislike/none)
- UpdatedAt

Tag catalog
- Predefined list managed centrally (seeded operationally) and not user-generated.
- Tags are lower-case.

### 1.6 Information architecture (MVP)
- Public catalog: all public prompts visible to all authenticated and authorized users.
- My prompts: the user’s personal library including private prompts and any public prompts they authored.

## 2. User Problem

### 2.1 Problems to solve
- Prompt knowledge is currently fragmented across individual notes, chats, or documents; teams struggle to find high-quality prompts that already exist.
- Users need a simple and fast way to store prompts for personal reuse.
- Users need a safe internal channel to share prompts across teams while keeping some prompts private.
- Users need lightweight discovery tools (title search and tag filtering) to find relevant prompts quickly.
- Users want guidance on relevant tags to improve findability, without the overhead of a fully free-form tagging system.

### 2.2 User needs
- Secure access to ensure internal content is never exposed publicly.
- Create and maintain a personal library of prompts.
- Share prompts broadly with the organization when appropriate.
- Find prompts quickly through search and tags.
- Identify useful prompts via likes/dislikes.
- Receive AI-suggested tags constrained to an approved tag list.

## 3. Functional Requirements

### 3.1 Authentication and authorization
FR-001 Entra sign-in required
- The application must require sign-in via Microsoft Entra External ID before rendering any application pages or data.

FR-002 Global authenticated access
- The application must enforce a global authorization policy (fallback policy) that requires an authenticated user for all routes.

FR-003 Access denied behavior
- If an unauthenticated user attempts to access the application, the user is redirected to sign-in.

FR-004 User identity
- The application must identify users by a stable Entra user identifier and use it for ownership checks and voting.
- The stable identifier is the `oid` claim.

### 3.2 Prompt creation and editing
FR-010 Create prompt
- Users must be able to create a prompt with Title, PromptText, Tags, and Visibility (private/public).

FR-011 Prompt validations
- Title
  - Required
  - Max length 200 characters
- PromptText
  - Required
  - Max length 3000 characters
- Tags
  - Optional but recommended
  - Must be selected from predefined tag list
  - Lower-case
  - Maximum 10 tags per prompt

FR-012 Update prompt
- Users must be able to edit Title, PromptText, Tags, and Visibility for prompts they authored.

FR-013 Ownership enforcement
- Only the author can edit or delete a prompt.

FR-014 Soft delete
- Deleting a prompt must set IsDeleted=true and remove it from all user-facing lists and queries.

FR-015 Audit fields
- Create/update operations must populate CreatedAt and UpdatedAt.

### 3.3 Prompt visibility and access rules
FR-020 Private prompt visibility
- Private prompts must be visible only to the author.

FR-021 Public prompt visibility
- Public prompts must be visible to all authenticated and authorized users.

FR-022 Switching visibility
- Users must be able to change a prompt’s visibility between private and public for prompts they authored.

### 3.4 Tag catalog and filtering
FR-030 Predefined tags
- The application must load a predefined tag list from Azure Table Storage.
- Users cannot create new tags in the MVP.

FR-031 Tag selection UI
- Users must be able to select and remove tags while creating/editing prompts.

FR-032 Tag filtering
- Users must be able to filter lists by selecting one or more tags.

FR-033 Tag casing and normalization
- The system must store and display tags in lower-case.

### 3.5 Search and discovery
FR-040 Title search
- The application must support searching prompts by title.
- Search must work in both Public catalog and My prompts views.

FR-041 Combined search and filtering
- Users must be able to apply tag filters alongside title search.

FR-042 Empty and no-result states
- The application must provide clear empty states for lists with no prompts and no-result states for searches/filters.

FR-043 Prompt detail view
- Users must be able to open a prompt to view its full Title, PromptText, Tags, author indication (where applicable), and vote totals.

### 3.6 Prompt text rendering (simplified markdown)
FR-050 Markdown rendering
- PromptText must support simplified markdown rendering in the UI.

FR-051 XSS-safe rendering
- Rendered output must be sanitized to prevent XSS.
- The system must not allow arbitrary HTML injection through PromptText.

### 3.7 Voting (likes/dislikes)
FR-060 Vote on prompts
- Users must be able to like or dislike a prompt they can view, subject to voting scope rules.

FR-061 One vote per user per prompt
- Each user can have at most one active vote per prompt.

FR-062 Vote toggle and switching
- Clicking the same vote again removes the vote (like -> none, dislike -> none).
- Clicking the opposite switches the vote (like -> dislike, dislike -> like).

FR-063 Display aggregates
- The UI must display total likes and dislikes for each prompt.

FR-064 Vote persistence
- The system must persist per-user vote state so totals and the user’s current selection are consistent across sessions.

### 3.8 AI-assisted tag suggestions
FR-070 Suggest tags on manual action
- On prompt create/edit, when the user clicks a "Suggest tags" action, the UI must trigger an AI tag suggestion request.

FR-071 Loader and non-blocking behavior
- The UI must show a loader while suggestions are being fetched.
- Failures or timeouts must not block prompt creation/editing.

FR-072 Suggestions constrained to allowed tags
- AI suggestions must be limited to tags from the predefined tag list.

FR-073 Suggestion size and UX
- The system should return 3–4 suggested tags.
- Users must be able to add a suggested tag by clicking it.

FR-074 Retrigger on demand
- If the title changes, users can click "Suggest tags" again to trigger a new suggestion request.

### 3.9 Storage, pagination, and operational requirements
FR-080 Azure Table Storage
- All prompts and votes must be stored in Azure Table Storage.

FR-081 Query efficiency
- Table design must prioritize key access patterns (public catalog listing, my prompts listing, title search, tag filtering) and avoid table scans.

FR-082 Pagination/continuations
- Lists should be designed to support pagination using continuation tokens, even if the initial dataset is small.

FR-083 Resilience
- Storage operations should include retry with exponential backoff for throttling scenarios (HTTP 429).

FR-084 Observability
- Key operations (CRUD, voting, AI tag suggestions) should log failures with structured logging.
- Errors shown to users must not include stack traces or sensitive data.

## 4. Product Boundaries

### 4.1 In scope (MVP)
- Azure Entra authentication (single tenant).
- Role-based authorization using a global policy requiring member app role.
- Prompt CRUD with soft delete.
- Private/public visibility with ownership enforcement.
- Predefined tag list from configuration; selection only.
- Search by title and tag-based filtering.
- Voting (like/dislike) with per-user toggle/switch behavior and aggregate counts.
- AI tag suggestions on title blur, limited to predefined tags.
- Azure Table Storage persistence and Azure App Service deployment.

### 4.2 Out of scope (MVP)
- Multi-tenant authentication.
- Anonymous access.
- In-app administration or moderation UI.
- User-generated/custom tags.
- Advanced collaboration features (real-time editing, commenting, versioning).
- Forking prompts; users may copy-paste to create a new prompt instead.
- Advanced ranking, recommendations, or analytics dashboards (beyond basic success metrics collection).

### 4.3 Assumptions
- The organization operates within a single Azure Entra tenant.
- Role assignment is handled by admins in Azure Portal.
- Content removed for moderation can be deleted/updated directly in Azure Storage during MVP.

### 4.4 Open questions and decisions needed
- Definition of simplified markdown (supported syntax and sanitization rules).
- Azure Table Storage key/query strategy for title search and tag filtering.
- Pagination expectations (page size and which lists must paginate from day one).
- Voting scope for private prompts and behavior when a prompt switches visibility.
- AI provider/model, cost limits, timeout/retry configuration, and fallback behavior.
- Operational messaging when access is denied due to missing role.

## 5. User Stories

### US-001 Secure access gate (login required)
Description
- As a company member, I must sign in before accessing any content so that internal prompts are never exposed to anonymous users.

Acceptance Criteria
- Given I am not authenticated, when I navigate to any app URL, then I am redirected to sign-in.
- Given I complete sign-in successfully, when I return to the app, then I can access authorized areas.

### US-010 View Public catalog
Description
- As a member, I want to browse all public prompts so I can discover prompts shared across the company.

Acceptance Criteria
- Given I am an authorized user, when I open Public catalog, then I see a list of public prompts.
- Private prompts authored by other users are not visible.
- Deleted prompts are not visible.

### US-011 View My prompts
Description
- As a member, I want to browse my own prompts so I can quickly reuse and edit my library.

Acceptance Criteria
- Given I am an authorized user, when I open My prompts, then I see prompts where AuthorId is my user id and IsDeleted is false.
- Both private and public prompts that I authored are visible.

### US-012 Empty states for catalogs
Description
- As a member, I want clear empty states so I understand when there are no prompts to display.

Acceptance Criteria
- Given Public catalog has no prompts, when I open it, then I see an empty state message.
- Given My prompts has no prompts, when I open it, then I see an empty state message.

### US-020 Create prompt (basic flow)
Description
- As a member, I want to create a prompt with title, text, tags, and visibility so I can store and share reusable prompts.

Acceptance Criteria
- Given I open the create prompt UI, when I enter a valid title and prompt text and save, then the prompt is created.
- CreatedAt and UpdatedAt are set.
- AuthorId is set to my user id.
- IsDeleted is false.

### US-021 Create prompt validations
Description
- As a member, I want validation feedback so I can correct input errors before saving.

Acceptance Criteria
- Given I attempt to save with an empty title, then I see a validation error and the save does not succeed.
- Given I attempt to save with a title longer than 200 characters, then I see a validation error and the save does not succeed.
- Given I attempt to save with empty prompt text, then I see a validation error and the save does not succeed.
- Given I attempt to save with prompt text longer than 3000 characters, then I see a validation error and the save does not succeed.

### US-022 Select tags from predefined list
Description
- As a member, I want to select tags from an approved list so that tagging stays consistent.

Acceptance Criteria
- Given I am creating or editing a prompt, when I open tag selection, then I can only choose from the predefined tags.
- I cannot enter arbitrary tag text.

### US-023 Enforce tag limit and normalization
Description
- As a member, I want the system to enforce tag rules so prompts remain consistently categorized.

Acceptance Criteria
- Given I already selected 10 tags, when I try to add an 11th tag, then the UI prevents adding it and provides feedback.
- Tags are stored and displayed in lower-case.

### US-024 AI tag suggestions on title blur
Description
- As a member, I want tag suggestions after entering a title so I can quickly add relevant tags.

Acceptance Criteria
- Given I am in the create/edit prompt UI, when I enter a title and the title field loses focus, then the system requests tag suggestions.
- While the request is in progress, a loader is visible.
- When suggestions return, then 3–4 suggested tags are displayed.
- When I click a suggested tag, then it is added to my selected tags (unless already selected or tag limit reached).

### US-025 Retrigger AI suggestions when title changes
Description
- As a member, I want suggestions to update when I change the title so that suggestions remain relevant.

Acceptance Criteria
- Given I receive suggestions, when I change the title and leave the field again, then a new request is triggered.
- The suggestions list updates to reflect the new response.

### US-026 AI suggestion failure is non-blocking
Description
- As a member, I want to continue creating a prompt even if AI suggestions fail so my work is not blocked.

Acceptance Criteria
- Given the AI suggestion request fails or times out, when I continue editing, then I can still save the prompt.
- A non-blocking error message is shown indicating suggestions are unavailable.

### US-030 Edit my prompt
Description
- As a member, I want to edit prompts I authored so I can keep them accurate and useful.

Acceptance Criteria
- Given I am the author, when I edit and save, then the changes are persisted.
- UpdatedAt changes to a later timestamp.

### US-031 Prevent editing others’ prompts
Description
- As a member, I should not be able to edit prompts I do not own so that ownership is respected.

Acceptance Criteria
- Given I am not the author, when I attempt to access edit actions for the prompt, then the UI does not show edit controls.
- Given I am not the author, when I attempt to invoke an edit endpoint/action directly, then the operation is rejected.

### US-032 Delete my prompt (soft delete)
Description
- As a member, I want to delete prompts I authored so I can remove outdated or incorrect entries.

Acceptance Criteria
- Given I am the author, when I delete a prompt, then IsDeleted is set to true.
- The prompt no longer appears in Public catalog or My prompts.

### US-033 Prevent deleting others’ prompts
Description
- As a member, I should not be able to delete prompts I do not own.

Acceptance Criteria
- Given I am not the author, when I attempt to delete the prompt via UI, then delete controls are not available.
- Given I am not the author, when I attempt to delete via direct invocation, then the operation is rejected.

### US-040 Change visibility (private/public)
Description
- As a member, I want to set or change a prompt’s visibility so I can keep some prompts private and share others widely.

Acceptance Criteria
- Given I am the author, when I mark a prompt as public, then it appears in Public catalog.
- Given I am the author, when I mark a prompt as private, then it is removed from Public catalog and remains in My prompts.

### US-041 Ensure private prompts are private
Description
- As a member, I need confidence that private prompts are only visible to me.

Acceptance Criteria
- Given a prompt is private and I am not the author, when I search/browse, then the prompt is not returned.
- Given I have a direct link to a private prompt I do not own, when I attempt to open it, then access is denied or the prompt is not found.

### US-050 Search prompts by title (Public catalog)
Description
- As a member, I want to search public prompts by title so I can quickly find relevant entries.

Acceptance Criteria
- Given I am in Public catalog, when I enter a search query, then results update to include only public prompts whose titles match the query.
- Given no prompts match, then a no-results state is shown.

### US-051 Search prompts by title (My prompts)
Description
- As a member, I want to search my prompts by title so I can find my own entries quickly.

Acceptance Criteria
- Given I am in My prompts, when I enter a search query, then results update to include only my prompts whose titles match the query.
- Given no prompts match, then a no-results state is shown.

### US-052 Filter by tags
Description
- As a member, I want to filter prompts by selected tags so I can narrow results to a category.

Acceptance Criteria
- Given I select one or more tags, when filtering is applied, then only prompts containing the selected tags are shown.
- Given I clear selected tags, then the unfiltered list is shown again.

### US-053 Combine search and tag filtering
Description
- As a member, I want to combine title search with tag filtering so I can efficiently refine results.

Acceptance Criteria
- Given I have an active search query and selected tags, when results are shown, then only prompts matching both criteria are displayed.

### US-060 View prompt details
Description
- As a member, I want to open a prompt and view its full content so I can reuse it.

Acceptance Criteria
- Given I open a prompt from a list, when the detail view loads, then I see title, rendered prompt text, tags, and vote totals.
- Prompt text is rendered using simplified markdown.

### US-061 Copy prompt text
Description
- As a member, I want to copy prompt text so I can paste it into my work tools.

Acceptance Criteria
- Given I view a prompt, when I click copy, then the prompt text is copied to the clipboard.
- The UI confirms the copy action.

### US-070 Render markdown safely
Description
- As a member, I want prompt text rendered safely so that shared content cannot execute scripts.

Acceptance Criteria
- Given prompt text contains markup that could be interpreted as HTML/script, when it is rendered, then scripts do not execute.
- The rendered output does not include unsafe tags/attributes.

### US-080 Like a prompt
Description
- As a member, I want to like a prompt so I can signal it is useful.

Acceptance Criteria
- Given I view a prompt I am allowed to vote on, when I click like, then my vote is recorded as like.
- The like count increases by 1 (accounting for my previous vote state).
- The UI shows my current vote selection.

### US-081 Dislike a prompt
Description
- As a member, I want to dislike a prompt so I can signal it is not useful.

Acceptance Criteria
- Given I view a prompt I am allowed to vote on, when I click dislike, then my vote is recorded as dislike.
- The dislike count increases by 1 (accounting for my previous vote state).
- The UI shows my current vote selection.

### US-082 Toggle off my vote
Description
- As a member, I want to remove my vote so I can revert to neutral.

Acceptance Criteria
- Given I previously liked a prompt, when I click like again, then my vote becomes none and likes decrement by 1.
- Given I previously disliked a prompt, when I click dislike again, then my vote becomes none and dislikes decrement by 1.

### US-083 Switch vote direction
Description
- As a member, I want to switch from like to dislike (or vice versa) so my feedback stays accurate.

Acceptance Criteria
- Given I previously liked a prompt, when I click dislike, then likes decrement by 1 and dislikes increment by 1.
- Given I previously disliked a prompt, when I click like, then dislikes decrement by 1 and likes increment by 1.

### US-084 Vote persistence
Description
- As a member, I want my vote state to persist so I see consistent behavior across sessions.

Acceptance Criteria
- Given I voted on a prompt, when I reload the app later, then my current selection is indicated.
- Aggregate counts reflect all votes.

### US-090 Voting edge cases for visibility changes
Description
- As a member, I want predictable voting behavior when a prompt changes visibility so totals remain correct.

Acceptance Criteria
- Given a prompt changes from public to private, when I am not the author, then I can no longer access it.
- Vote totals and per-user vote state remain stored and consistent if the prompt becomes public again.

### US-100 Handle storage throttling gracefully
Description
- As a member, I want the app to remain usable during transient storage throttling so my actions don’t fail unnecessarily.

Acceptance Criteria
- Given Azure Table Storage returns throttling responses, when I retryable operation occurs, then the system retries with exponential backoff.
- If retries are exhausted, then the UI shows a generic error message and does not expose technical details.

### US-101 Pagination readiness
Description
- As a member, I want lists to load efficiently so browsing remains fast as the catalog grows.

Acceptance Criteria
- Given a list has more than the page size of prompts, when I load more, then the next page is fetched using continuation tokens.
- The UI does not attempt to load all prompts at once.

### US-110 Error handling without sensitive information
Description
- As a member, I want errors presented safely so that I am informed without seeing sensitive details.

Acceptance Criteria
- Given an unexpected error occurs, when it is shown in the UI, then it contains a user-friendly message and no stack trace.
- The error is logged with sufficient detail for troubleshooting.

## 6. Success Metrics

### 6.1 Adoption and activation
- Percentage of invited members who successfully sign in within the first week of launch.
- Time-to-first-prompt created (median time from first login to first successful prompt creation).

### 6.2 Core usage
- Prompts created per user per week.
- Ratio of public to private prompts.
- Prompt edits per prompt (indicates ongoing maintenance and value).

### 6.3 Discovery effectiveness
- Searches per session.
- Search-to-open rate (percentage of searches resulting in opening a prompt detail).
- Tag filter usage frequency (percentage of sessions where tags are used).

### 6.4 Collaboration signal
- Votes per public prompt.
- Like/dislike ratio distribution by prompt.

### 6.5 AI suggestion reliability and performance
- AI tag suggestion request success rate.
- Median latency for AI tag suggestions.
- Percentage of suggestion failures where prompt creation still succeeds (target: near 100%).

### 6.6 Usability and satisfaction
- Task completion rate in lightweight internal usability tests for:
  - Create prompt
  - Find prompt via search/filter
  - Vote on prompt
- Target outcome: at least 75% of participants complete core tasks without assistance.

## PRD Checklist Review
- Each user story is testable: yes; all acceptance criteria define observable behavior.
- Acceptance criteria are clear and specific: yes; criteria specify preconditions, actions, and outcomes.
- Enough user stories to build a fully functional application: yes; covers authentication/authorization, CRUD, visibility, tags, search/filter, voting, AI suggestions, error handling, and list scaling.
- Authentication and authorization requirements included: yes; US-001 through US-003 address secure access and role gating.
