# Entra External ID Integration Plan for Blazor Server Application

## Overview
This document outlines the step-by-step process to integrate Microsoft Entra External ID with the PromptHub Blazor Server application.

This plan is aligned with the MVP requirements:
- No anonymous access: all application routes require an authenticated user (global fallback policy)
- Sign-in using social identity providers:
  - Microsoft personal accounts (Outlook.com / Live)
  - Google accounts (Gmail)
- No Microsoft Graph dependency for MVP
- Stable user identifier for persistence: `oid`

## Prerequisites
- Active Azure subscription
- .NET 9.0 SDK installed
- Blazor Server application (PromptHub.Web)
- Admin access to Azure Portal
- Visual Studio 2022 or VS Code

---

## Phase 1: Azure Entra External ID Configuration

### Step 1: Create Entra External ID Tenant
1. Navigate to [Azure Portal](https://portal.azure.com)
2. Search for "Microsoft Entra External ID" in the search bar
3. Click **Create** to create a new External ID tenant
4. Fill in the required information:
   - **Organization name**: `{ORG_NAME}`
   - **Initial domain name**: `{TENANT_NAME}` (will be `{TENANT_DOMAIN}`)
   - **Country/Region**: Select your region
5. Click **Review + Create** and then **Create**
6. Wait for the tenant to be provisioned (2-3 minutes)
7. Switch to the newly created tenant using the directory switcher (top-right corner)

### Step 2: Register the Blazor Application
1. In the Entra External ID tenant, navigate to **App registrations**
2. Click **New registration**
3. Configure the application:
   - **Name**: PromptHub Web Application
   - **Supported account types**: Select "Accounts in any identity provider or organizational directory (for authenticating users with user flows)"
   - **Redirect URI**:
     - Platform: **Web**
     - URI: `https://{DEV_HOST}:{DEV_HTTPS_PORT}/signin-oidc`
     - Add additional URIs for production later (see Phase 4)
4. Click **Register**
5. **Save the following values** (you'll need them later):
   - **Application (client) ID**
   - **Directory (tenant) ID**

### Step 3: Configure Application Secrets
1. In your app registration, navigate to **Certificates & secrets**
2. Click **New client secret**
3. Add description: "PromptHub Web Secret"
4. Set expiration: 24 months (recommended)
5. Click **Add**
6. **Copy and save the secret value immediately** (it won't be shown again)

### Step 4: Configure API Permissions
1. Navigate to **API permissions** in your app registration
2. For MVP, do **not** add Microsoft Graph permissions (for example `User.Read`).
3. Keep permissions/scopes to the minimum required for OpenID Connect sign-in.

### Step 5: Configure Authentication Settings
1. Navigate to **Authentication** in your app registration
2. Under **Platform configurations** -> **Web**, verify your redirect URI
3. Add logout URL: `https://{DEV_HOST}:{DEV_HTTPS_PORT}/signout-callback-oidc`
4. Use the **authorization code flow**.
5. Do not enable implicit grant flows unless you have a specific requirement.
5. Under **Advanced settings**:
   - Set **Allow public client flows**: **No**
6. Click **Save**

### Step 6: Configure Token Configuration (Optional but Recommended)
1. Navigate to **Token configuration**
2. Click **Add optional claim**
3. Select **ID** token type
4. Ensure your ID token includes the claims you need for MVP UI:
   - `name` (display)
   - `email` (optional)
   - `oid` (required for a stable application user key)
5. Click **Add**

### Step 7: Create User Flows (Sign-up and Sign-in)
1. In the Entra External ID tenant, navigate to **User flows**
2. Click **New user flow**
3. Select **Sign up and sign in**
4. Configure the user flow:
   - **Name**: B2C_1_signupsignin
   - **Identity providers**:
     - ✅ Microsoft Account (personal)
     - ✅ Google
5. Under **User attributes and token claims**, select:
   - Collect attributes during sign-up:
     - ✅ Display Name
     - ✅ Email Address
     - ✅ Given Name
     - ✅ Surname
   - Return claims in token:
     - ✅ Display Name
     - ✅ Email Addresses
     - ✅ Given Name
     - ✅ Surname
     - ✅ User's Object ID (required; maps to `oid`)
6. Click **Create**

### Step 8: Configure Social Identity Providers (Optional)
If you want to enable social login:

#### For Microsoft Account:
1. Navigate to **Identity providers** -> **Microsoft Account**
2. Follow the wizard to configure

#### For Google:
1. Create OAuth 2.0 credentials in Google Cloud Console
2. Navigate to **Identity providers** -> **Google**
3. Enter Client ID and Client Secret
4. Add authorized redirect URI: `https://{TENANT_NAME}.b2clogin.com/{TENANT_DOMAIN}/oauth2/authresp`

### Step 9: Configure Branding (Optional)
1. Navigate to **Company branding**
2. Customize:
   - Logo
   - Background image
   - Sign-in page text
   - Colors and themes
3. Click **Save**

---

## Phase 2: Blazor Application Configuration

### Step 10: Install Required NuGet Packages
Add the following packages to `PromptHub.Web.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="9.0.0" />
<PackageReference Include="Microsoft.Identity.Web" Version="2.*" />
<PackageReference Include="Microsoft.Identity.Web.UI" Version="2.*" />
```

Or run in terminal:
```powershell
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
```

### Step 11: Update appsettings.json
Add Entra External ID configuration to `appsettings.json`:

```json
{
  "AzureAdB2C": {
    "Instance": "https://{TENANT_NAME}.b2clogin.com/",
    "Domain": "{TENANT_DOMAIN}",
    "TenantId": "<YOUR_TENANT_ID>",
    "ClientId": "<YOUR_CLIENT_ID>",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "SignUpSignInPolicyId": "{USER_FLOW_NAME}"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Step 12: Configure User Secrets (Development)
For development, store sensitive data in user secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAdB2C:ClientSecret" "<YOUR_CLIENT_SECRET>"
```

### Step 13: Update Program.cs
Modify `Program.cs` to configure authentication:

```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using PromptHub.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Microsoft Identity Web authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        options.ResponseType = "code";
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;
    });

// Add authorization services (global auth)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Add cascading authentication state
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Add authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map authentication endpoints
app.MapControllers();

app.Run();
```

### Step 14: Update App.razor
Wrap the application with authentication components:

```razor
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="bootstrap/bootstrap.min.css" />
    <link rel="stylesheet" href="app.css" />
    <link rel="stylesheet" href="PromptHub.Web.styles.css" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <HeadOutlet />
</head>

<body>
    <CascadingAuthenticationState>
        <Routes />
    </CascadingAuthenticationState>
    <script src="_framework/blazor.web.js"></script>
</body>

</html>
```

### Step 15: Create Login/Logout Components
Create a new component `Components/Layout/LoginDisplay.razor`:

```razor
@using Microsoft.AspNetCore.Components.Authorization

<AuthorizeView>
    <Authorized>
        <span class="navbar-text">
            Hello, @context.User.Identity?.Name!
        </span>
        <a href="MicrosoftIdentity/Account/SignOut" class="nav-link">
            Log out
        </a>
    </Authorized>
    <NotAuthorized>
        <a href="MicrosoftIdentity/Account/SignIn" class="nav-link">
            Log in
        </a>
    </NotAuthorized>
</AuthorizeView>
```

### Step 16: Update NavMenu.razor
Add the login display to your navigation menu:

```razor
@* Add at the top of the navbar *@
<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">PromptHub.Web</a>
        <LoginDisplay />
    </div>
</div>
```

### Step 17: Protect Pages with Authorization
Authentication is enforced globally via a fallback policy.

Only add `@attribute [Authorize]` for clarity on security-sensitive pages (optional), or `@attribute [AllowAnonymous]` for any explicitly public routes (not expected in MVP).

```razor
@page "/counter"
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize]

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>
<p>Current count: @currentCount</p>
<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
```

### Step 18: Access User Information
Create a service or use AuthenticationStateProvider to access user claims:

```razor
@page "/profile"
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@attribute [Authorize]

<PageTitle>Profile</PageTitle>

<h1>User Profile</h1>

<AuthorizeView>
    <Authorized>
        <dl>
            <dt>Name:</dt>
            <dd>@context.User.Identity?.Name</dd>
            
            <dt>Email:</dt>
            <dd>@context.User.FindFirst("emails")?.Value</dd>
            
            <dt>Object ID (oid):</dt>
            <dd>@context.User.FindFirst("oid")?.Value</dd>
        </dl>
    </Authorized>
</AuthorizeView>
```

---

## Phase 3: Testing and Validation

### Step 19: Test Local Development
1. Ensure your application runs on the expected HTTPS URL: `https://{DEV_HOST}:{DEV_HTTPS_PORT}`
2. Run the application:
   ```powershell
   dotnet run --project PromptHub.Web
   ```
3. Navigate to a protected page
4. You should be redirected to the Entra External ID login page
5. Sign up with a new account or sign in
6. Verify you're redirected back to the application
7. Verify user information is displayed correctly
8. Test logout functionality

### Step 20: Test Different Scenarios
- ✅ Sign in with Google
- ✅ Sign in with Microsoft personal account
- ✅ Access protected pages
- ✅ Verify every route requires authentication (fallback policy)
- ✅ View user profile information
- ✅ Sign out and verify session is cleared
- ✅ Password reset flow (if enabled in user flow)

---

## Phase 4: Production Deployment

### Step 21: Configure Production Settings
1. Update redirect URIs in Azure app registration:
   - Add production URL: `https://{PROD_HOST}/signin-oidc`
   - Add production logout URL: `https://{PROD_HOST}/signout-callback-oidc`

2. Update `appsettings.Production.json`:
```json
{
  "AzureAdB2C": {
    "Instance": "https://{TENANT_NAME}.b2clogin.com/",
    "Domain": "{TENANT_DOMAIN}",
    "TenantId": "<YOUR_TENANT_ID>",
    "ClientId": "<YOUR_CLIENT_ID>",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "SignUpSignInPolicyId": "{USER_FLOW_NAME}"
  }
}
```

### Step 22: Configure Azure App Service
1. In Azure Portal, navigate to your App Service
2. Go to **Configuration** -> **Application settings**
3. Add the following settings:
   - `AzureAdB2C__ClientSecret` = `<YOUR_CLIENT_SECRET>`
   - `AzureAdB2C__Instance` = `https://{TENANT_NAME}.b2clogin.com/`
   - `AzureAdB2C__Domain` = `{TENANT_DOMAIN}`
   - `AzureAdB2C__TenantId` = `<YOUR_TENANT_ID>`
   - `AzureAdB2C__ClientId` = `<YOUR_CLIENT_ID>`
4. Click **Save**

### Step 23: Deploy and Verify
1. Deploy the application to Azure App Service
2. Test all authentication flows in production
3. Monitor Application Insights for any authentication errors

---

## Phase 5: Advanced Configuration (Optional)

### Step 24: Add Custom Policies (Advanced)
For advanced scenarios like custom user journeys, password complexity, MFA:
1. Navigate to **Identity Experience Framework** in Entra External ID
2. Create custom policies using XML
3. Upload custom policies
4. Test custom policies

### Step 25: Configure Multi-Factor Authentication
1. Navigate to **User flows** -> Select your flow
2. Click **Properties**
3. Under **Multifactor authentication**, select:
   - **MFA enforcement**: On
   - **Type of method**: Choose (Email, Phone, or both)
4. Click **Save**

### Step 26: Add Custom Attributes
1. Navigate to **User attributes** in Entra External ID
2. Click **Add** to create custom attributes
3. Add to user flow collection/claims
4. Access in application via claims

### Step 27: Configure Token Lifetime
1. Navigate to **Token configuration** in app registration
2. Adjust:
   - Access token lifetime
   - Refresh token lifetime
   - ID token lifetime

---

## Security Best Practices

### ✅ Checklist
- [ ] Store Client Secret in Azure Key Vault or App Service Configuration (not in code)
- [ ] Use HTTPS everywhere (enforce in production)
- [ ] Enable logging and monitoring (Application Insights)
- [ ] Regularly rotate client secrets
- [ ] Implement proper error handling for authentication failures
- [ ] Use authorization policies for fine-grained access control
- [ ] Enable MFA for sensitive operations
- [ ] Configure session timeout appropriately
- [ ] Validate tokens properly
- [ ] Keep NuGet packages updated

---

## Troubleshooting

### Common Issues

**Issue: Redirect URI mismatch**
- Solution: Verify redirect URIs in Azure match exactly with your application URLs

**Issue: Client secret expired**
- Solution: Generate new secret in Azure and update application configuration

**Issue: User not redirected after login**
- Solution: Check that `UseAuthentication()` comes before `UseAuthorization()` in Program.cs

**Issue: Claims not appearing**
- Solution: Verify claims are configured in user flow token configuration

**Issue: CORS errors**
- Solution: Ensure your domain is properly configured in Entra External ID settings

---

## Resources

- [Microsoft Identity Web Documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [Entra External ID Documentation](https://learn.microsoft.com/en-us/azure/active-directory-b2c/)
- [Blazor Authentication Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)
- [Azure AD B2C Samples](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2)

---

## Timeline Estimate

- **Phase 1 (Azure Setup)**: 1-2 hours
- **Phase 2 (Blazor Configuration)**: 2-3 hours
- **Phase 3 (Testing)**: 1-2 hours
- **Phase 4 (Production)**: 1-2 hours
- **Phase 5 (Advanced - Optional)**: 2-4 hours

**Total**: 7-13 hours (depending on complexity and optional features)

---

## Next Steps

1. Complete Phase 1: Azure Entra External ID Configuration
2. Save all credentials securely
3. Proceed to Phase 2: Blazor Application Configuration
4. Test thoroughly in development
5. Deploy to production
6. Monitor and iterate
