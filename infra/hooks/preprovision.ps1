# preprovision.ps1 — azd preprovision hook
# Creates Entra ID app registrations when ENABLE_ENTRA_AUTH=true.
# Skips if already provisioned (AZURE_AD_API_CLIENT_ID is set).
# Requires: az CLI authenticated, Application.ReadWrite.All Graph permission.
$ErrorActionPreference = 'Stop'

$enableEntraAuth = $env:ENABLE_ENTRA_AUTH
if ($enableEntraAuth -ne 'true') {
    Write-Host "ENABLE_ENTRA_AUTH is not 'true' — skipping Entra ID app registration."
    exit 0
}

# Check if app registrations are already provisioned
$existingApiClientId = $env:AZURE_AD_API_CLIENT_ID
if (-not [string]::IsNullOrEmpty($existingApiClientId)) {
    Write-Host "AZURE_AD_API_CLIENT_ID is already set ($existingApiClientId) — skipping Entra ID app registration."
    exit 0
}

Write-Host "ENABLE_ENTRA_AUTH=true — provisioning Entra ID app registrations..."

# Resolve variables from azd environment
$environmentName = $env:AZURE_ENV_NAME
if ([string]::IsNullOrEmpty($environmentName)) {
    throw "AZURE_ENV_NAME is required"
}

$resourceGroup = if ($env:AZURE_RESOURCE_GROUP) { $env:AZURE_RESOURCE_GROUP } else { "rg-$environmentName" }
$swaName = $env:AZURE_STATIC_WEB_APP_NAME

# Build the SPA redirect URI
$spaRedirectUri = ''
if (-not [string]::IsNullOrEmpty($swaName)) {
    $spaRedirectUri = "https://$swaName.azurestaticapps.net"
}

Write-Host "Deploying Entra ID app registrations..."
Write-Host "  Resource Group: $resourceGroup"
Write-Host "  Environment:    $environmentName"
Write-Host "  SPA Redirect:   $(if ($spaRedirectUri) { $spaRedirectUri } else { '(localhost only)' })"

$deploymentOutput = az deployment group create `
    --resource-group $resourceGroup `
    --template-file "./infra/entra-id/app-registrations.bicep" `
    --parameters environmentName=$environmentName `
                 spaProductionRedirectUri=$spaRedirectUri `
    --query "properties.outputs" `
    --output json | ConvertFrom-Json

$apiClientId = $deploymentOutput.apiClientId.value
$spaClientId = $deploymentOutput.spaClientId.value

if ([string]::IsNullOrEmpty($apiClientId)) {
    throw "Failed to retrieve API client ID from deployment output."
}

Write-Host "Entra ID app registrations created successfully."
Write-Host "  API Client ID: $apiClientId"
Write-Host "  SPA Client ID: $spaClientId"

# Store outputs in azd environment for use by main.bicep
azd env set AZURE_AD_API_CLIENT_ID $apiClientId
azd env set AZURE_AD_SPA_CLIENT_ID $spaClientId

Write-Host "Entra ID client IDs saved to azd environment."
