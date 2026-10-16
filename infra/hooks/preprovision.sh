#!/usr/bin/env bash
# preprovision.sh — azd preprovision hook
# Creates Entra ID app registrations when ENABLE_ENTRA_AUTH=true.
# Skips if already provisioned (AZURE_AD_API_CLIENT_ID is set).
# Requires: az CLI authenticated, Application.ReadWrite.All Graph permission.
set -eu

AZURE_SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-}"
if [ -z "${AZURE_SUBSCRIPTION_ID}" ]; then
  echo "ERROR: AZURE_SUBSCRIPTION_ID is required to register subscription features." >&2
  exit 1
fi

FEATURE_NAMESPACE="Microsoft.Network"
FEATURE_NAME="AllowBringYourOwnPublicIpAddress"
FEATURE_STATE=$(az feature list \
  --subscription "${AZURE_SUBSCRIPTION_ID}" \
  --namespace "${FEATURE_NAMESPACE}" \
  --query "[?contains(name, '${FEATURE_NAME}')].properties.state | [0]" \
  --output tsv) || {
  echo "ERROR: Could not read ${FEATURE_NAMESPACE}/${FEATURE_NAME} registration state." >&2
  exit 1
}

if [ "${FEATURE_STATE}" != "Registered" ]; then
  echo "Registering ${FEATURE_NAMESPACE}/${FEATURE_NAME}..."
  az feature register \
    --subscription "${AZURE_SUBSCRIPTION_ID}" \
    --namespace "${FEATURE_NAMESPACE}" \
    --name "${FEATURE_NAME}" \
    --output none || {
    echo "ERROR: Could not register ${FEATURE_NAMESPACE}/${FEATURE_NAME}. The Azure identity needs permission to register subscription features." >&2
    exit 1
  }

  ATTEMPT=1
  while [ "${ATTEMPT}" -le 120 ]; do
    FEATURE_STATE=$(az feature show \
      --subscription "${AZURE_SUBSCRIPTION_ID}" \
      --namespace "${FEATURE_NAMESPACE}" \
      --name "${FEATURE_NAME}" \
      --query properties.state \
      --output tsv) || {
      echo "ERROR: Could not check ${FEATURE_NAMESPACE}/${FEATURE_NAME} registration status." >&2
      exit 1
    }
    if [ "${FEATURE_STATE}" = "Registered" ]; then
      break
    fi

    echo "Feature registration state: ${FEATURE_STATE} (attempt ${ATTEMPT}/120)."
    sleep 15
    ATTEMPT=$((ATTEMPT + 1))
  done

  if [ "${FEATURE_STATE}" != "Registered" ]; then
    echo "ERROR: Timed out waiting for ${FEATURE_NAMESPACE}/${FEATURE_NAME} to register." >&2
    exit 1
  fi
else
  echo "${FEATURE_NAMESPACE}/${FEATURE_NAME} is already registered."
fi

az provider register --subscription "${AZURE_SUBSCRIPTION_ID}" --namespace "${FEATURE_NAMESPACE}" --wait || {
  echo "ERROR: Could not refresh registration for ${FEATURE_NAMESPACE}." >&2
  exit 1
}

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
