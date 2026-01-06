# PromptHub

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4)](https://learn.microsoft.com/aspnet/core/blazor/hosting-models#blazor-server)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Table of contents
- [Project description](#project-description)
- [Tech stack](#tech-stack)
- [Getting started locally](#getting-started-locally)
- [Available scripts](#available-scripts)
- [Project scope](#project-scope)
- [Project status](#project-status)
- [License](#license)

## Project description
PromptHub is an internal, single-tenant web application for storing, sharing, discovering, and reusing AI prompts across an organization. It focuses on an MVP that enables authenticated and authorized members to:

- Create, view, edit, and soft-delete prompts
- Control visibility (private/public) with ownership enforcement
- Search by title and filter by predefined tags
- Like/dislike prompts with per-user vote state and aggregate totals
- Receive AI-assisted tag suggestions constrained to an approved tag catalog
- Render prompt text using simplified Markdown with XSS-safe sanitization

The application is intended to be deployed to **Azure App Service** and secured via **Azure Entra ID** (single tenant, no anonymous access).

## Tech stack
- **Runtime / Framework**
  - **.NET 9**
  - **Blazor Server**
- **UI**
  - **MudBlazor**
- **Authentication & authorization**
  - **Azure Entra ID** (single tenant)
  - Global authorization policy requiring a configured **member app role**
  - Role assignment managed in **Azure Portal** (no in-app admin UI for MVP)
- **Storage**
  - **Azure Table Storage**
    - Prompts (with soft delete via `IsDeleted`)
    - Votes (per-user vote state)
    - Tag catalog (predefined list stored in Table Storage)
  - Design expectations: avoid table scans, use continuation tokens for pagination, retry with exponential backoff on throttling (HTTP 429)
- **Observability**
  - Structured logging for failures in key operations (CRUD, voting, AI suggestions)
  - User-facing errors must be generic (no stack traces/sensitive data)
- **Testing**
  - **xUnit** (recommended: FluentAssertions; bUnit for component tests)

## Getting started locally
### Prerequisites
- **Visual Studio 2022** with the ASP.NET workload, or the **.NET 9 SDK**
- Access to the project�s **Azure Entra ID** tenant (single-tenant app)
- (Optional, for storage) **Azurite** or an Azure Storage account for Table Storage

### Run the app
1. Clone the repository.
2. Open the solution in Visual Studio.
3. Ensure configuration is set up (see `appsettings.json` and environment-specific settings).
4. Run the `PromptHub.Web` project.

### Configuration notes (MVP assumptions)
- The app requires sign-in before rendering any application pages/data.
- Authorization is role-gated via a global policy requiring the configured **member app role**.
 - Tag catalog is stored in Azure Table Storage (`TagCatalog`); tags are stored/displayed in lower-case and users cannot create new tags in the MVP.

## Provisioning scripts

PowerShell helper scripts are available in `scripts` to provision Azure resources required by the app.

- `scripts/1-tablestorage.ps1` creates the Azure Table Storage tables defined in `PromptHub.Web/.ai/4-db-plan.md`.
  - Prerequisites: Azure CLI (`az`) installed and logged in.
  - Configuration: update values in `scripts/config.ps1` (subscription, resource group, storage account).
  - Run:
    - From the repository root:
      - `pwsh ./scripts/1-tablestorage.ps1 -ConfigPath ./scripts/config.ps1`

> Note: `scripts/config.ps1` is treated as local-only configuration and is excluded from build/publish outputs.

> Missing details for a fully copy/paste local setup: exact `appsettings.json` keys for Entra ID, Table Storage connection settings, and the required app role name/value.

## Available scripts
This repository uses standard .NET CLI commands:
