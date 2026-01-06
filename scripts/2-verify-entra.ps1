param(
	[string]$ConfigPath = ".\config.ps1",
	[string]$AppDisplayName = "PromptHub",
	[string]$UserFlowName = "B2C_1_signupsignin",
	[string]$DevHost = "localhost",
	[int]$DevHttpsPort = 5001,
	[switch]$FailOnWarning
)

$ErrorActionPreference = "Stop"

function Write-Status($label, $ok, $details) {
	$prefix = if ($ok) { "[OK]  " } else { "[FAIL]" }
	Write-Host "$prefix $label"
	if ($details) {
		if ($details -is [System.Collections.IEnumerable] -and -not ($details -is [string])) {
			foreach ($d in $details) { Write-Host "      $d" }
		}
		else {
			Write-Host "      $details"
		}
	}
}

function Write-Warn($label, $details) {
	Write-Host "[WARN] $label"
	if ($details) { Write-Host "      $details" }
	if ($FailOnWarning) {
		throw "FailOnWarning enabled and a warning was produced: $label"
	}
}

function Require-Command($name, $installHint) {
	if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
		throw "Required command not found: $name. $installHint"
	}
}

function AzJson($args) {
	Write-Host "-------------------"
	Write-Host "az " $args
	$out = & az @args 2>$null
	if (-not $out) { return $null }
	Write-Host "-------------------"
	return $out | ConvertFrom-Json
}

function Try-AzJson($args) {
	try { return AzJson $args } catch { return $null }
}

function Get-RedirectUris($app) {
	$uris = @()

	if ($app.web -and $app.web.redirectUris) {
		$uris += @($app.web.redirectUris)
	}

	if ($app.spa -and $app.spa.redirectUris) {
		$uris += @($app.spa.redirectUris)
	}

	if ($app.publicClient -and $app.publicClient.redirectUris) {
		$uris += @($app.publicClient.redirectUris)
	}

	return $uris | Where-Object { $_ } | Select-Object -Unique
}

if (-not (Test-Path -LiteralPath $ConfigPath)) {
	throw "Config file not found: $ConfigPath"
}

. $ConfigPath

if (-not $subscrptionId) {
	throw "Missing required configuration variable in config file: `$subscrptionId"
}

Require-Command -name "az" -installHint "Install Azure CLI and ensure it is on PATH."
Write-Host "Using subscription: $subscrptionId"
az account set --subscription $subscrptionId | Out-Null

Write-Host "Checking Azure CLI login state..."
$account = Try-AzJson @("account", "show", "-o", "json")
if (-not $account) {
	throw "Not logged in (or unable to query account). Run: az login"
}
Write-Status -label "Azure CLI logged in" -ok $true -details "$($account.user.name) / $($account.tenantId)"

Require-Command -name "az" -installHint "Azure CLI missing (already checked)."
$graphExt = & az extension list --query "[?name=='application-insights' || name=='account' || name=='azure-devops']" -o json 2>$null | ConvertFrom-Json
# We will use Microsoft Graph; validate CLI has the graph commands available.
$help = & az help 2>$null
if (-not $help) {
	Write-Warn -label "Unable to run 'az help'." -details "Azure CLI may be misconfigured."
}

# --- Entra / External ID checks via Microsoft Graph (az rest) ---
# NOTE: External ID user flows are not reliably exposed via az CLI in all environments.
# This script verifies app registration settings that the docs require and warns for user-flow/provider items.

$expectedRedirectUri = "https://$DevHost`:$DevHttpsPort/signin-oidc"
$expectedLogoutUri = "https://$DevHost`:$DevHttpsPort/signout-callback-oidc"

Write-Host ""
Write-Host "== Verifying app registration =="
Write-Host "Target display name: $AppDisplayName"

$apps = AzJson @(
	"rest",
	"--method", "GET",
	"--uri", "https://graph.microsoft.com/v1.0/applications?`$filter=displayName eq '$AppDisplayName'&`$select=id,appId,displayName,web,spa,publicClient,requiredResourceAccess,signInAudience"
)

if (-not $apps -or -not $apps.value -or $apps.value.Count -eq 0) {
	Write-Status -label "App registration exists" -ok $false -details "No app with displayName '$AppDisplayName' found in tenant '$($account.tenantId)'."
	throw "App registration not found."
}

if ($apps.value.Count -gt 1) {
	Write-Warn -label "Multiple app registrations match displayName." -details "Using the first match. Consider making displayName unique."
}

$app = $apps.value[0]
Write-Status -label "App registration exists" -ok $true -details "appId=$($app.appId) objectId=$($app.id)"

# Supported account types / audience (best-effort; CIAM adds nuances)
if ($app.signInAudience) {
	# Common values: AzureADMyOrg, AzureADMultipleOrgs, AzureADandPersonalMicrosoftAccount, PersonalMicrosoftAccount
	# External ID registrations can also use tenant-specific settings; we treat mismatch as warning.
	$aud = $app.signInAudience
	$audOk = $aud -ne "AzureADMyOrg"
	if ($audOk) {
		Write-Status -label "Supported account types not limited to single org" -ok $true -details "signInAudience=$aud"
	}
	else {
		Write-Warn -label "Supported account types look single-org." -details "signInAudience=$aud. Docs expect sign-in via user flows / external identities; re-check app registration supported account types."
	}
}
else {
	Write-Warn -label "Unable to read signInAudience." -details "Graph property missing from response."
}

# Redirect URIs
$redirectUris = Get-RedirectUris -app $app
$hasRedirect = $redirectUris -contains $expectedRedirectUri
Write-Status -label "Redirect URI contains /signin-oidc" -ok $hasRedirect -details @(
	"Expected: $expectedRedirectUri",
	"Configured: $($redirectUris -join ', ')"
)

# Logout URL (front-channel)
$logoutOk = $false
$logoutConfigured = $null
if ($app.web -and $app.web.logoutUrl) {
	$logoutConfigured = $app.web.logoutUrl
	$logoutOk = ($logoutConfigured -eq $expectedLogoutUri)
}

Write-Status -label "Logout URL configured (/signout-callback-oidc)" -ok $logoutOk -details @(
	"Expected: $expectedLogoutUri",
	"Configured: $logoutConfigured"
)

# Implicit grant / code flow expectations
$implicitAccessOk = $true
$implicitIdOk = $true
if ($app.web -and $app.web.implicitGrantSettings) {
	$implicitAccessOk = -not [bool]$app.web.implicitGrantSettings.enableAccessTokenIssuance
	$implicitIdOk = -not [bool]$app.web.implicitGrantSettings.enableIdTokenIssuance

	Write-Status -label "Implicit grant (access token) disabled" -ok $implicitAccessOk -details $app.web.implicitGrantSettings.enableAccessTokenIssuance
	Write-Status -label "Implicit grant (id token) disabled" -ok $implicitIdOk -details $app.web.implicitGrantSettings.enableIdTokenIssuance
}
else {
	Write-Warn -label "Unable to read implicit grant settings." -details "Property web.implicitGrantSettings missing; verify 'Implicit grant' is disabled in the portal."
}

# Public client flows should be disabled
# In Graph, this maps loosely to publicClient presence and/or isFallbackPublicClient.
$isPublicClient = $false
$publicClientUris = @()
if ($app.publicClient -and $app.publicClient.redirectUris) {
	$isPublicClient = $true
	$publicClientUris = @($app.publicClient.redirectUris)
}
if ($isPublicClient -and $publicClientUris.Count -gt 0) {
	Write-Warn -label "Public client configuration detected." -details "Docs expect Allow public client flows: No. PublicClient redirectUris: $($publicClientUris -join ', ')"
}
else {
	Write-Status -label "Public client flows not configured" -ok $true -details $null
}

# API permissions: avoid Microsoft Graph permissions for MVP
# Check requiredResourceAccess entries that target Microsoft Graph (resourceAppId well-known)
$graphResourceAppId = "00000003-0000-0000-c000-000000000000"
$required = @()
if ($app.requiredResourceAccess) { $required = @($app.requiredResourceAccess) }

$graphAccess = $required | Where-Object { $_.resourceAppId -eq $graphResourceAppId }
if ($graphAccess -and $graphAccess.Count -gt 0) {
	Write-Status -label "No Microsoft Graph API permissions (MVP)" -ok $false -details "requiredResourceAccess includes Microsoft Graph. Remove Graph scopes if not needed."
}
else {
	Write-Status -label "No Microsoft Graph API permissions (MVP)" -ok $true -details $null
}

Write-Host ""
Write-Host "== Verifying token/exposed claims expectations =="
Write-Warn -label "Token claims (oid/name/email) cannot be fully verified via Graph app object alone." -details "Verify in External ID user flow 'User attributes and token claims' that 'User's Object ID' is returned (oid), and Display Name/Email are included."

Write-Host ""
Write-Host "== Verifying social identity providers =="
Write-Warn -label "Identity providers (Google / Microsoft Account) are not verified by this script." -details "Verify in External ID tenant -> Identity providers that Google and Microsoft Account are enabled and Google redirect URI is configured (…/oauth2/authresp)."

Write-Host ""
Write-Host "== Verifying user flow =="
Write-Warn -label "User flow verification is not automated here." -details "Verify user flow '$UserFlowName' exists and enables Google + Microsoft Account, returns oid (Object ID), and collects/returns Display Name + Email."

Write-Host ""
Write-Host "== Verifying app configuration file presence (repository) =="
$appSettingsPath = Join-Path -Path $PSScriptRoot -ChildPath "..\PromptHub.Web\appsettings.json"
$appSettingsPath = (Resolve-Path -LiteralPath $appSettingsPath).Path

$appSettings = Get-Content -LiteralPath $appSettingsPath -Raw | ConvertFrom-Json
$hasAzureAdB2C = $null -ne $appSettings.AzureAdB2C

if ($hasAzureAdB2C) {
	Write-Status -label "appsettings.json contains AzureAdB2C section" -ok $true -details $null
}
else {
	Write-Warn -label "appsettings.json is missing AzureAdB2C section." -details "Docs expect AzureAdB2C config (Instance/Domain/TenantId/ClientId/CallbackPath/SignedOutCallbackPath/SignUpSignInPolicyId)."
}

Write-Host ""
Write-Host "== Summary =="
Write-Host "Checked:"
Write-Host " - App registration exists and key settings (redirect/logout/implicit grant/public client/Graph permissions)"
Write-Host "Manual verification recommended for:"
Write-Host " - External ID user flow ($UserFlowName) settings and claims (oid/name/email)"
Write-Host " - Social identity providers (Google, Microsoft Account)"
Write-Host ""
Write-Host "Done."