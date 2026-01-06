# Authentication & Authorization — High-level plan (MVP)

This document summarizes the agreed plan for authentication and authorization for the `PromptHub.Web` Blazor Server application.

## Goals
- No anonymous access: **no application content is rendered unless the user is authenticated**.
- Support personal accounts:
  - **Microsoft personal accounts** (Outlook.com / Live)
  - **Google accounts** (Gmail)
- Keep authorization simple for MVP: **any authenticated user can use the application** (no mandatory member role).
- Use a stable user identifier for storage ownership and voting.

## Identity platform choice
**Microsoft Entra External ID (CIAM)** is used (the modern successor to Azure AD B2C) because it supports social identity providers like Google and Microsoft personal accounts.

## Authentication approach
- Authentication protocol: **OpenID Connect**.
- Application type: Blazor Server.
- Use `Microsoft.Identity.Web` to integrate OIDC with ASP.NET Core authentication.
- Use code flow (recommended for server-side apps).

### Identity providers
Configure Entra External ID tenant with:
- **Microsoft Account** identity provider
- **Google** identity provider

## Authorization approach
- Enforce authentication globally using a **fallback policy**.
  - Default: all routes require an authenticated user.
  - Exceptions: only the required auth callback routes/endpoints.
- No app-role requirement in MVP.

## User identity mapping
- Canonical user key: `oid` claim.
- Use `oid` for all persistence identifiers:
  - `Prompts.AuthorId`
  - `PromptVotes.VoterId`

Do not key application data off email/UPN, because those can be missing or change per identity provider.

## Claims used in UI (no Graph dependency)
- Display name: use the available name claim from the ID token (for example the standard `name` claim) and fall back safely if missing.
- Avoid Microsoft Graph permissions unless a concrete feature requires it.

## Session, cookies, and tokens
- Use secure authentication cookies:
  - HTTPS-only cookies
  - `SameSite` configured appropriately for OIDC
- Do **not** persist tokens (`SaveTokens = false`) unless a clear downstream API scenario exists.
- Sign-out must:
  - Clear the local auth cookie
  - Trigger the provider sign-out callback (`/signout-callback-oidc`)

## Secrets and configuration
- Use `ClientId` + `ClientSecret` for the web app.
- Local development:
  - Store `AzureAdB2C:ClientSecret` in **user secrets**.
- Production:
  - Store secrets in **App Service Configuration** (or Key Vault), never in the repository.

## MFA
- MFA is not enforced in application logic.
- If required later, configure MFA in Entra user flows / tenant policies.

## Operational checklist (MVP)
- External ID tenant created and configured.
- Web app registered with correct redirect URIs:
  - `https://localhost:<port>/signin-oidc`
  - `https://localhost:<port>/signout-callback-oidc`
- Google identity provider configured with correct redirect URI (`.../oauth2/authresp`).
- Microsoft identity provider enabled.
- App configuration updated for `AzureAdB2C` settings.
- Local user-secrets set for client secret.
- Test flows:
  - Unauthenticated -> app route => redirected to sign-in
  - Sign-in with Google
  - Sign-in with Microsoft personal account
  - Sign-out clears local session and completes provider sign-out
