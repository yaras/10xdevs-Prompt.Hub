param(
	[string]$ConfigPath = ".\config.ps1"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ConfigPath)) {
	throw "Config file not found: $ConfigPath"
}

. $ConfigPath

if (-not $subscrptionId -or -not $resourceGroupName -or -not $storageAccountName) {
	throw "Missing required configuration variables in config file. Expected: `$subscrptionId, `$resourceGroupName, `$storageAccountName."
}

$requiredTables = @(
	"Prompts",
	"PromptVotes",
	"PublicPromptsNewestIndex"
)

Write-Host "Using subscription: $subscrptionId"
Write-Host "Resource group: $resourceGroupName"
Write-Host "Storage account: $storageAccountName"

az account set --subscription $subscrptionId | Out-Null

$storageKey = az storage account keys list `
	--resource-group $resourceGroupName `
	--account-name $storageAccountName `
	--query "[0].value" -o tsv

if (-not $storageKey) {
	throw "Unable to retrieve storage account key for $storageAccountName"
}

foreach ($tableName in $requiredTables) {
	Write-Host "Ensuring table exists: $tableName"
	az storage table create `
		--name $tableName `
		--account-name $storageAccountName `
		--account-key $storageKey | Out-Null
}

Write-Host "Done. Tables ensured: $($requiredTables -join ', ')"