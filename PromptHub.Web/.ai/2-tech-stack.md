# Tech Stack Summary — PromptHub (MVP)

This document summarizes the intended MVP technology stack based on `.ai/1-prd.md`.

## Application type
- Internal, single-tenant web application (no anonymous access).
- Delivered as a browser-based app, deployed to **Azure App Service**.

## Frontend
- **Blazor** (project is a Blazor app; MVP UI built as Blazor components).
- **MudBlazor** for UI components.

## Authentication & authorization
- **Microsoft Entra External ID** authentication (CIAM) with social identity providers:
  - Microsoft personal accounts (Outlook.com / Live)
  - Google accounts (Gmail)
- Authorization via a **global fallback policy** requiring an authenticated user (no anonymous access).
- Users are identified by a stable Entra user identifier (`oid` claim) for ownership checks and voting.

## Data storage
- **Azure Table Storage** for persistence of:
  - Prompts (with soft delete via `IsDeleted`)
  - Votes (per-user vote state)
  - Tag catalog (predefined list stored in Table Storage)
- Design expectations:
  - Avoid table scans; optimize partitions/keys for access patterns.
  - Support pagination via continuation tokens.
  - Retries with exponential backoff on throttling (HTTP 429).

## Core features (MVP scope)
- Prompt CRUD (title, prompt text, tags, visibility), with ownership enforcement.
- Private/public visibility model.
- Search by title and tag filtering.
- Likes/dislikes with per-user vote toggle/switch and aggregate counts.
- AI-assisted tag suggestions (3–4 tags) constrained to the predefined tag list.
- Simplified Markdown rendering for prompt text with XSS-safe sanitization.

## Observability & reliability
- Structured logging of failures for key operations (CRUD, voting, AI tag suggestions).
- User-facing errors must be generic (no stack traces or sensitive data).

## Testing
- **xUnit** as the unit test framework.
- (Per project guidance) FluentAssertions recommended for assertions; bUnit for Blazor component tests.