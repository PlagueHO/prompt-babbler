using './main.bicep'

// Required parameters
param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'azdtemp')
param location = readEnvironmentVariable('AZURE_LOCATION', 'EastUS2')

// User or service principal deploying the resources
param principalId = readEnvironmentVariable('AZURE_PRINCIPAL_ID', '')
param principalIdType = toLower(readEnvironmentVariable('AZURE_PRINCIPAL_ID_TYPE', 'user')) == 'serviceprincipal' ? 'ServicePrincipal' : 'User'

// Network access parameter
param enablePublicNetworkAccess = bool(readEnvironmentVariable('ENABLE_PUBLIC_NETWORK_ACCESS', 'true'))

// Entra ID app registration client IDs (set by preprovision hook when ENABLE_ENTRA_AUTH=true)
param apiClientId = readEnvironmentVariable('AZURE_AD_API_CLIENT_ID', '')
param spaClientId = readEnvironmentVariable('AZURE_AD_SPA_CLIENT_ID', '')
