# Microsoft Entra External ID (CIAM) Integration Plan for Blazor Server Application

## Overview
This document outlines the step-by-step process to integrate **Microsoft Entra External ID (CIAM)** with the PromptHub Blazor Server application.

This plan is aligned with the MVP requirements:
- No anonymous access: all application routes require an authenticated user (global fallback policy)
- Sign-in using social identity providers:
  - Microsoft personal accounts (Outlook.com / Live)
  - Google accounts (Gmail)
- No Microsoft Graph dependency for MVP
- Stable user identifier for persistence: prefer `oid` if present, otherwise use OIDC `sub`

## Prerequisites
- Active Azure subscription
- .NET 9.0 SDK installed
- Blazor Server application (PromptHub.Web)
- Admin access to Azure Portal

---

## Phase 1: Azure Entra External ID (CIAM) Configuration

### Step 1: Create Entra External ID Tenant
1. Navigate to [Azure Portal](https://portal.azure.com)
2. Search for "Microsoft Entra External ID"
3. Create a new External ID tenant
4. Switch to the newly created tenant (directory switcher)

### Step 2: Register the Blazor Application
1. In the External ID tenant, navigate to **App registrations**
2. Click **New registration**
3. Configure the application:
   - **Name**: PromptHub Web Application
   - **Redirect URI (Web)**: `https://{DEV_HOST}:{DEV_HTTPS_PORT}/signin-oidc`
4. Click **Register**
5. Save:
   - **Application (client) ID**
   - **Directory (tenant) ID**

### Step 3: Configure Client Secret
1. In the app registration: **Certificates & secrets**
2. Create a **New client secret**
3. Copy the secret value immediately

### Step 4: Configure Authentication (Redirect + Logout URLs)
1. In the app registration: **Authentication**
2. Under **Web**, ensure Redirect URI includes:
   - `https://{DEV_HOST}:{DEV_HTTPS_PORT}/signin-oidc`
3. Add Logout URL:
   - `https://{DEV_HOST}:{DEV_HTTPS_PORT}/signout-callback-oidc`

### Step 5: Configure Social Identity Providers (Microsoft + Google)
1. Navigate to **External Identities** / **Identity providers** (portal UX may vary)
2. Configure:
   - Microsoft Account
   - Google
3. Ensure the provider configuration is enabled for the tenant

### Step 6: Identify the correct CIAM authority host
CIAM tenants use `ciamlogin.com` endpoints.

In the app registration:
1. Go to **Endpoints**
2. Locate an endpoint that references `https://{tenant}.ciamlogin.com/...`
3. The authority host is the base:
   - `https://{tenant}.ciamlogin.com/`

---

## Phase 2: Blazor Application Configuration

### Step 7: Required NuGet Packages
Add to `PromptHub.Web.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="9.0.0" />
<PackageReference Include="Microsoft.Identity.Web" Version="2.*" />
<PackageReference Include="Microsoft.Identity.Web.UI" Version="2.*" />
```

### Step 8: Update appsettings.json
Configure CIAM settings (note: no B2C policy id is used):

```json
{
  "AzureAd": {
    "Instance": "https://{TENANT_NAME}.ciamlogin.com/",
    "TenantId": "<YOUR_TENANT_ID>",
    "ClientId": "<YOUR_CLIENT_ID>",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### Step 9: Configure User Secrets (Development)
Store the secret outside config files:

```powershell
dotnet user-secrets init --project PromptHub.Web
dotnet user-secrets set "AzureAd:ClientSecret" "<YOUR_CLIENT_SECRET>" --project PromptHub.Web
```

### Step 10: Program.cs authentication + global authorization
Configure authentication + enforce authentication globally:

- Use `AddMicrosoftIdentityWebApp` bound to `AzureAd`
- Set `Authority` explicitly to `https://{tenant}.ciamlogin.com/{TenantId}/v2.0`
- Configure fallback policy to require authenticated users
- Map controllers (for `MicrosoftIdentity/Account/*` endpoints)

---

## Phase 3: Testing and Validation

1. Run the app:

```powershell
dotnet run --project PromptHub.Web
```

2. Browse to any route
3. You should be redirected to the External ID hosted sign-in UI
4. After sign-in you should return to the app

Validate stable user id:
- Prefer `oid` claim if present
- Otherwise use `sub` claim for persistence

---

## Troubleshooting

**Issue: Unable to obtain configuration / metadata document**
- Confirm `AzureAd:Instance` uses `https://{tenant}.ciamlogin.com/`
- Confirm `Authority` resolves to `https://{tenant}.ciamlogin.com/{tenantId}/v2.0`
- Verify you are using the correct tenant/app registration

**Issue: Redirect URI mismatch**
- Ensure app registration Redirect URI matches your local HTTPS URL exactly

---

## Resources
- Microsoft Identity Web: https://learn.microsoft.com/entra/identity-platform/microsoft-identity-web
- External ID (CIAM): https://learn.microsoft.com/entra/external-id/
- Blazor security: https://learn.microsoft.com/aspnet/core/blazor/security/
