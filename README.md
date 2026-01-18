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

The application is intended to be deployed to **Azure App Service** and secured via **Microsoft Entra External ID** (no anonymous access) with sign-in using **Microsoft personal accounts (Outlook.com/Live)** and **Google accounts (Gmail)**.

## Tech stack

- **Runtime / Framework**
  - **.NET 9**
  - **Blazor Server**
- **UI**
  - **MudBlazor**
- **Authentication & authorization**
  - **Microsoft Entra External ID** (CIAM)
  - Social sign-in: Microsoft personal accounts (Outlook.com/Live) and Google accounts (Gmail)
  - Global authorization fallback policy requiring an authenticated user (no anonymous access)
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
- Access to the project  **Microsoft Entra External ID** tenant and app registration
- (Optional, for storage) **Azurite** or an Azure Storage account for Table Storage

### Run the app

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Ensure configuration is set up (see `appsettings.json` and environment-specific settings).
4. Run the `PromptHub.Web` project.

### Authentication configuration (local dev)
Client secrets must never be committed to the repository.

1. Initialize user secrets for the `PromptHub.Web` project:
   - `dotnet user-secrets init --project PromptHub.Web`
2. Set the External ID client secret:
   - `dotnet user-secrets set "AzureAdB2C:ClientSecret" "<YOUR_CLIENT_SECRET>" --project PromptHub.Web`

In production, configure secrets via **App Service Configuration** (or Key Vault), not `appsettings.json`.

### Configuration notes (MVP assumptions)
- The app requires sign-in before rendering any application pages/data.
- Authorization is enforced via a global fallback policy requiring an authenticated user.
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

## Available scripts
This repository uses standard .NET CLI commands:

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the Blazor Server app
dotnet run --project PromptHub.Web

# Run with Development environment explicitly (PowerShell)
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project PromptHub.Web

# Watch mode (hot reload)
dotnet watch --project PromptHub.Web

# Initialize user-secrets for local development
dotnet user-secrets init --project PromptHub.Web

# Set a secret (example: External ID client secret)
dotnet user-secrets set "AzureAdB2C:ClientSecret" "<YOUR_CLIENT_SECRET>" --project PromptHub.Web

# Run all tests
dotnet test

# Run unit tests only
dotnet test ./PromptHub.Web.UnitTests/PromptHub.Web.UnitTests.csproj

# Release build
dotnet build -c Release

# Publish for deployment (e.g., Azure App Service)
dotnet publish ./PromptHub.Web/PromptHub.Web.csproj -c Release -o ./artifacts/publish

# Provision Azure Table Storage tables (requires Azure CLI + PowerShell)
pwsh ./scripts/1-tablestorage.ps1 -ConfigPath ./scripts/config.ps1
