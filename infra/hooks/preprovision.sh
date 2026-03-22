#!/usr/bin/env bash
# preprovision.sh — azd preprovision hook
# Creates Entra ID app registrations when ENABLE_ENTRA_AUTH=true.
# Skips if already provisioned (AZURE_AD_API_CLIENT_ID is set).
# Requires: az CLI authenticated, Application.ReadWrite.All Graph permission.
set -euo pipefail

ENABLE_ENTRA_AUTH="${ENABLE_ENTRA_AUTH:-false}"

if [ "${ENABLE_ENTRA_AUTH}" != "true" ]; then
  echo "ENABLE_ENTRA_AUTH is not 'true' — skipping Entra ID app registration."
  exit 0
fi

# Check if app registrations are already provisioned
EXISTING_API_CLIENT_ID="${AZURE_AD_API_CLIENT_ID:-}"
if [ -n "${EXISTING_API_CLIENT_ID}" ]; then
  echo "AZURE_AD_API_CLIENT_ID is already set (${EXISTING_API_CLIENT_ID}) — skipping Entra ID app registration."
  exit 0
fi

echo "ENABLE_ENTRA_AUTH=true — provisioning Entra ID app registrations..."

# Resolve variables from azd environment
ENVIRONMENT_NAME="${AZURE_ENV_NAME:?AZURE_ENV_NAME is required}"
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-rg-${ENVIRONMENT_NAME}}"
SWA_NAME="${AZURE_STATIC_WEB_APP_NAME:-}"

# Build the SPA redirect URI. If the Static Web App name is known, use it.
# Otherwise default to empty (localhost only — the Bicep param default handles this).
SPA_REDIRECT_URI=""
if [ -n "${SWA_NAME}" ]; then
  SPA_REDIRECT_URI="https://${SWA_NAME}.azurestaticapps.net"
fi

echo "Deploying Entra ID app registrations..."
echo "  Resource Group: ${RESOURCE_GROUP}"
echo "  Environment:    ${ENVIRONMENT_NAME}"
echo "  SPA Redirect:   ${SPA_REDIRECT_URI:-'(localhost only)'}"

DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group "${RESOURCE_GROUP}" \
  --template-file "./infra/entra-id/app-registrations.bicep" \
  --parameters environmentName="${ENVIRONMENT_NAME}" \
               spaProductionRedirectUri="${SPA_REDIRECT_URI}" \
  --query "properties.outputs" \
  --output json)

API_CLIENT_ID=$(echo "${DEPLOYMENT_OUTPUT}" | jq -r '.apiClientId.value')
SPA_CLIENT_ID=$(echo "${DEPLOYMENT_OUTPUT}" | jq -r '.spaClientId.value')

if [ -z "${API_CLIENT_ID}" ] || [ "${API_CLIENT_ID}" = "null" ]; then
  echo "ERROR: Failed to retrieve API client ID from deployment output."
  exit 1
fi

echo "Entra ID app registrations created successfully."
echo "  API Client ID: ${API_CLIENT_ID}"
echo "  SPA Client ID: ${SPA_CLIENT_ID}"

# Store outputs in azd environment for use by main.bicep
azd env set AZURE_AD_API_CLIENT_ID "${API_CLIENT_ID}"
azd env set AZURE_AD_SPA_CLIENT_ID "${SPA_CLIENT_ID}"

echo "Entra ID client IDs saved to azd environment."
