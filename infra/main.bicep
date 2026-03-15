targetScope = 'subscription'

@sys.description('Name of the environment which is used to generate a short unique hash used in all resources.')
@minLength(1)
@maxLength(40)
param environmentName string

@sys.description('Location for all resources.')
@minLength(1)
@metadata({
  azd: {
    type: 'location'
  }
})
param location string

@sys.description('The Azure resource group where new resources will be deployed.')
@metadata({
  azd: {
    type: 'resourceGroup'
  }
})
param resourceGroupName string = 'rg-${environmentName}'

@sys.description('Id of the user or app to assign application roles.')
param principalId string

@sys.description('Type of the principal referenced by principalId.')
@allowed([
  'User'
  'ServicePrincipal'
])
param principalIdType string = 'User'

@sys.description('Whether to enable public network access to Azure resources.')
param enablePublicNetworkAccess bool = true

var abbrs = loadJsonContent('./abbreviations.json')
var modelDeployments = loadJsonContent('./model-deployments.json')

// Tags that should be applied to all resources.
var tags = {
  'azd-env-name': environmentName
  project: 'prompt-babbler'
}

// Generate a unique token to be used in naming resources.
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

var logAnalyticsWorkspaceName = '${abbrs.operationalInsightsWorkspaces}${environmentName}'
var applicationInsightsName = '${abbrs.insightsComponents}${environmentName}'
var foundryName = '${abbrs.aiFoundryAccounts}${environmentName}'
var foundryCustomSubDomainName = toLower(replace(environmentName, '-', ''))
var defaultProjectName = 'promptbabbler'
var containerAppsEnvironmentName = '${abbrs.appManagedEnvironments}${environmentName}'
var containerAppName = '${abbrs.appContainerApps}${environmentName}-api'
var staticWebAppName = '${abbrs.webStaticSites}${environmentName}'

// The application resources that are deployed into the application resource group
module rg 'br/public:avm/res/resources/resource-group:0.4.3' = {
  name: 'resource-group-deployment-${resourceToken}'
  params: {
    name: resourceGroupName
    location: location
    tags: tags
  }
}

// --------- MONITORING RESOURCES ---------
module logAnalyticsWorkspace 'br/public:avm/res/operational-insights/workspace:0.15.0' = {
  name: 'log-analytics-workspace-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    tags: tags
  }
}

module applicationInsights 'br/public:avm/res/insights/component:0.7.1' = {
  name: 'application-insights-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: applicationInsightsName
    location: location
    tags: tags
    workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
  }
}

// --------- MICROSOFT FOUNDRY ---------
module foundryService './cognitive-services/accounts/main.bicep' = {
  name: 'microsoft-foundry-service-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: foundryName
    kind: 'AIServices'
    location: location
    customSubDomainName: foundryCustomSubDomainName
    disableLocalAuth: false
    allowProjectManagement: true
    diagnosticSettings: [
      {
        name: 'send-to-log-analytics'
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
            enabled: true
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
            enabled: true
          }
        ]
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    publicNetworkAccess: enablePublicNetworkAccess ? 'Enabled' : 'Disabled'
    sku: 'S0'
    deployments: modelDeployments
    defaultProject: defaultProjectName
    projects: [
      {
        name: defaultProjectName
        location: location
        properties: {
          displayName: 'Prompt Babbler'
          description: 'Project for Prompt Babbler speech-to-prompt application'
        }
      }
    ]
    tags: tags
  }
}

// Foundry role assignments for the deploying principal
var foundryRoleAssignmentsArray = [
  ...(!empty(principalId) ? [
    {
      roleDefinitionIdOrName: 'Contributor'
      principalType: principalIdType
      principalId: principalId
    }
    {
      roleDefinitionIdOrName: 'Cognitive Services OpenAI Contributor'
      principalType: principalIdType
      principalId: principalId
    }
    {
      roleDefinitionIdOrName: 'Cognitive Services Speech User'
      principalType: principalIdType
      principalId: principalId
    }
  ] : [])
]

module foundryRoleAssignments './core/security/role_foundry.bicep' = {
  name: 'microsoft-foundry-role-assignments-${resourceToken}'
  scope: az.resourceGroup(resourceGroupName)
  dependsOn: [
    rg
    foundryService
  ]
  params: {
    foundryName: foundryName
    roleAssignments: foundryRoleAssignmentsArray
  }
}

// --------- CONTAINER APPS ENVIRONMENT ---------
module containerAppsEnvironment 'br/public:avm/res/app/managed-environment:0.10.0' = {
  name: 'container-apps-environment-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: containerAppsEnvironmentName
    location: location
    tags: tags
    logAnalyticsWorkspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
    zoneRedundant: false
  }
}

// --------- CONTAINER APP (API) ---------
module containerApp 'br/public:avm/res/app/container-app:0.12.0' = {
  name: 'container-app-api-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
    containerAppsEnvironment
  ]
  params: {
    name: containerAppName
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    location: location
    tags: union(tags, {
      'azd-service-name': 'api'
    })
    managedIdentities: {
      systemAssigned: true
    }
    containers: [
      {
        name: 'api'
        image: 'ghcr.io/plagueho/prompt-babbler-api:latest'
        resources: {
          cpu: '0.5'
          memory: '1Gi'
        }
        env: [
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: applicationInsights.outputs.connectionString
          }
          {
            name: 'AZURE_AI_FOUNDRY_ENDPOINT'
            value: foundryService.outputs.endpoint
          }
          {
            name: 'AZURE_AI_FOUNDRY_PROJECT_ENDPOINT'
            value: 'https://${foundryCustomSubDomainName}.services.ai.azure.com/api/projects/${defaultProjectName}'
          }
        ]
      }
    ]
    ingressExternal: true
    ingressTargetPort: 8080
    ingressTransport: 'auto'
    scaleMinReplicas: 0
    scaleMaxReplicas: 3
  }
}

// Assign Cognitive Services OpenAI User + Speech User roles to the Container App's
// managed identity so it can call Foundry models and Speech Service via managed identity
var containerAppFoundryRoleAssignments = [
  {
    roleDefinitionIdOrName: 'Cognitive Services OpenAI User'
    principalType: 'ServicePrincipal'
    principalId: containerApp.outputs.systemAssignedMIPrincipalId
  }
  {
    roleDefinitionIdOrName: 'Cognitive Services Speech User'
    principalType: 'ServicePrincipal'
    principalId: containerApp.outputs.systemAssignedMIPrincipalId
  }
]

module containerAppFoundryRoles './core/security/role_foundry.bicep' = {
  name: 'container-app-foundry-roles-${resourceToken}'
  scope: az.resourceGroup(resourceGroupName)
  dependsOn: [
    foundryService
    containerApp
  ]
  params: {
    foundryName: foundryName
    roleAssignments: containerAppFoundryRoleAssignments
  }
}

// --------- STATIC WEB APP (FRONTEND) ---------
module staticWebApp 'br/public:avm/res/web/static-site:0.7.0' = {
  name: 'static-web-app-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: staticWebAppName
    location: location
    tags: union(tags, {
      'azd-service-name': 'frontend'
    })
    sku: 'Free'
  }
}

// --------- OUTPUTS ---------
output AZURE_RESOURCE_GROUP string = rg.outputs.name
output AZURE_PRINCIPAL_ID string = principalId
output AZURE_PRINCIPAL_ID_TYPE string = principalIdType

// Monitoring
output LOG_ANALYTICS_WORKSPACE_NAME string = logAnalyticsWorkspace.outputs.name
output LOG_ANALYTICS_RESOURCE_ID string = logAnalyticsWorkspace.outputs.resourceId
output LOG_ANALYTICS_WORKSPACE_ID string = logAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
output APPLICATION_INSIGHTS_NAME string = applicationInsights.outputs.name
output APPLICATION_INSIGHTS_RESOURCE_ID string = applicationInsights.outputs.resourceId
output APPLICATION_INSIGHTS_INSTRUMENTATION_KEY string = applicationInsights.outputs.instrumentationKey

// Microsoft Foundry
output AZURE_AI_FOUNDRY_NAME string = foundryService.outputs.name
output AZURE_AI_FOUNDRY_ID string = foundryService.outputs.resourceId
output AZURE_AI_FOUNDRY_ENDPOINT string = foundryService.outputs.endpoint
output AZURE_AI_FOUNDRY_RESOURCE_ID string = foundryService.outputs.resourceId
output AZURE_AI_FOUNDRY_PROJECT_ENDPOINT string = 'https://${foundryCustomSubDomainName}.services.ai.azure.com/api/projects/${defaultProjectName}'

// Container App (API)
output AZURE_CONTAINER_APP_NAME string = containerApp.outputs.name
output AZURE_CONTAINER_APP_FQDN string = containerApp.outputs.fqdn

// Static Web App (Frontend)
output AZURE_STATIC_WEB_APP_NAME string = staticWebApp.outputs.name
output AZURE_STATIC_WEB_APP_DEFAULT_HOSTNAME string = staticWebApp.outputs.defaultHostname
