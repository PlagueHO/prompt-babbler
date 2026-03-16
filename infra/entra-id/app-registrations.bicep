// Entra ID App Registrations for Prompt Babbler
// Uses the Microsoft Graph Bicep extension to create API and SPA app registrations.

extension microsoftGraphV1

@description('Environment name used as display name prefix.')
param environmentName string

@description('Production redirect URI for the SPA (e.g., https://<staticwebapp>.azurestaticapps.net).')
param spaProductionRedirectUri string = ''

// Deterministic GUID for the access_as_user scope
var accessAsUserScopeId = guid('prompt-babbler-access-as-user')

// ---- API App Registration ----
resource apiApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: 'prompt-babbler-api'
  displayName: '${environmentName}-prompt-babbler-api'
  signInAudience: 'AzureADMyOrg'
  api: {
    requestedAccessTokenVersion: 2
    oauth2PermissionScopes: [
      {
        id: accessAsUserScopeId
        value: 'access_as_user'
        type: 'User'
        adminConsentDisplayName: 'Access Prompt Babbler API'
        adminConsentDescription: 'Allow the app to access Prompt Babbler API on behalf of the signed-in user.'
        userConsentDisplayName: 'Access Prompt Babbler API'
        userConsentDescription: 'Allow the app to access Prompt Babbler API on your behalf.'
        isEnabled: true
      }
    ]
  }
  optionalClaims: {
    accessToken: [
      {
        name: 'idtyp'
        essential: false
      }
    ]
  }
  identifierUris: [
    'api://prompt-babbler-api'
  ]
}

resource apiServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: apiApp.appId
}

// ---- SPA App Registration ----

var spaRedirectUris = empty(spaProductionRedirectUri)
  ? [
      'http://localhost:5173'
    ]
  : [
      'http://localhost:5173'
      spaProductionRedirectUri
    ]

resource spaApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: 'prompt-babbler-spa'
  displayName: '${environmentName}-prompt-babbler-spa'
  signInAudience: 'AzureADMyOrg'
  spa: {
    redirectUris: spaRedirectUris
  }
  requiredResourceAccess: [
    {
      resourceAppId: apiApp.appId
      resourceAccess: [
        {
          id: accessAsUserScopeId
          type: 'Scope'
        }
      ]
    }
  ]
}

resource spaServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: spaApp.appId
}

// ---- Pre-authorize SPA for API scope (avoids user consent prompt) ----
// NOTE: This may require a separate update if circular reference doesn't resolve in single pass.
// If deployment fails here, move preAuthorizedApplications to a post-deployment script.

// ---- Outputs ----

@description('The client ID (appId) of the API app registration.')
output apiClientId string = apiApp.appId

@description('The client ID (appId) of the SPA app registration.')
output spaClientId string = spaApp.appId
