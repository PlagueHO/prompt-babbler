targetScope = 'subscription'

@sys.description('Name of the environment which is used to generate a short unique hash used in all resources.')
@minLength(1)
@maxLength(24)
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

@sys.description('Entra ID API app registration client ID. Leave empty to disable authentication (single-user anonymous mode).')
param apiClientId string = ''

@sys.description('Entra ID SPA app registration client ID. Leave empty to disable authentication.')
param spaClientId string = ''

@sys.description('Location for the Azure Static Web App. Must be one of the supported regions. Defaults to the primary location if not specified.')
@allowed([
  ''
  'centralus'
  'eastasia'
  'eastus2'
  'westeurope'
  'westus2'
])
param staticWebAppLocation string = ''

@sys.description('Container image to deploy for the backend API Container App.')
param containerImageApi string = 'ghcr.io/plagueho/prompt-babbler-api:latest'

@sys.description('Container image to deploy for the MCP Server Container App.')
param containerImageMcpServer string = 'ghcr.io/plagueho/prompt-babbler-mcp-server:latest'

@sys.description('Entra ID MCP server app registration client ID. Leave empty to disable authentication (single-user anonymous mode).')
param mcpClientId string = ''

@sys.description('Optional access code for single-user mode protection. Leave empty to disable.')
@secure()
param accessCode string = ''

var abbrs = loadJsonContent('./abbreviations.json')
var modelDeployments = loadJsonContent('./model-deployments.json')

// Tags that should be applied to all resources.
var tags = {
  'azd-env-name': environmentName
  project: 'prompt-babbler'
}

// Generate a unique token to be used in naming resources.
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Cosmos DB database name used across AVM module and standalone container modules.
var cosmosDbDatabaseName = 'prompt-babbler'

var logAnalyticsWorkspaceName = '${abbrs.operationalInsightsWorkspaces}${environmentName}'
var applicationInsightsName = '${abbrs.insightsComponents}${environmentName}'
var foundryName = '${abbrs.aiFoundryAccounts}${environmentName}'
var foundryCustomSubDomainName = toLower(replace(environmentName, '-', ''))
var defaultProjectName = 'promptbabbler'
var containerAppsEnvironmentName = '${abbrs.appManagedEnvironments}${environmentName}'
var containerAppName = '${abbrs.appContainerApps}${environmentName}-api'
var mcpServerContainerAppName = '${abbrs.appContainerApps}${environmentName}-mcp-server'
var staticWebAppName = '${abbrs.webStaticSites}${environmentName}'
var effectiveStaticWebAppLocation = !empty(staticWebAppLocation) ? staticWebAppLocation : location
var cosmosDbAccountName = '${abbrs.cosmosDBAccounts}${resourceToken}'
var vnetName = '${abbrs.networkVirtualNetworks}${environmentName}'
var acaSubnetName = '${abbrs.networkVirtualNetworksSubnets}${environmentName}-aca'
var privateEndpointSubnetName = '${abbrs.networkVirtualNetworksSubnets}${environmentName}-pe'
var cosmosDbPrivateEndpointName = '${abbrs.networkPrivateEndpoints}${environmentName}-cosmosdb'
var foundryPrivateEndpointName = '${abbrs.networkPrivateEndpoints}${environmentName}-foundry'

// The application resources that are deployed into the application resource group
module rg 'br/public:avm/res/resources/resource-group:0.4.3' = {
  name: 'resource-group-deployment-${resourceToken}'
  params: {
    name: resourceGroupName
    location: location
    tags: tags
  }
}

// --------- NETWORKING RESOURCES ---------
// Virtual Network with subnets for Container Apps Environment and Private Endpoints
module virtualNetwork 'br/public:avm/res/network/virtual-network:0.8.1' = {
  name: 'virtual-network-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: vnetName
    location: location
    tags: tags
    addressPrefixes: [
      '10.0.0.0/16'
    ]
    subnets: [
      {
        name: acaSubnetName
        addressPrefix: '10.0.0.0/23'
        // Delegation to Microsoft.App/environments is required when infrastructureSubnetResourceId is set
        // on a managed environment (both Consumption and Workload Profiles).
        delegation: 'Microsoft.App/environments'
      }
      {
        name: privateEndpointSubnetName
        addressPrefix: '10.0.2.0/24'
        // Disable network policies to allow private endpoints
        privateEndpointNetworkPolicies: 'Disabled'
      }
    ]
  }
}

// Private DNS Zone for Cosmos DB
module cosmosDbPrivateDnsZone 'br/public:avm/res/network/private-dns-zone:0.8.1' = {
  name: 'cosmos-db-private-dns-zone-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: 'privatelink.documents.azure.com'
    location: 'global'
    tags: tags
    virtualNetworkLinks: [
      {
        virtualNetworkResourceId: virtualNetwork.outputs.resourceId
      }
    ]
  }
}

// Private DNS Zones for Azure AI Foundry / Cognitive Services
module foundryPrivateDnsZone 'br/public:avm/res/network/private-dns-zone:0.8.1' = {
  name: 'foundry-private-dns-zone-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: 'privatelink.cognitiveservices.azure.com'
    location: 'global'
    tags: tags
    virtualNetworkLinks: [
      {
        virtualNetworkResourceId: virtualNetwork.outputs.resourceId
      }
    ]
  }
}

// Private DNS Zone for OpenAI endpoint access
module openAiPrivateDnsZone 'br/public:avm/res/network/private-dns-zone:0.8.1' = {
  name: 'openai-private-dns-zone-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: 'privatelink.openai.azure.com'
    location: 'global'
    tags: tags
    virtualNetworkLinks: [
      {
        virtualNetworkResourceId: virtualNetwork.outputs.resourceId
      }
    ]
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
    raiPolicies: [
      {
        name: 'PromptBabblerContentPolicy'
        basePolicyName: 'Microsoft.Default'
        mode: 'Blocking'
        contentFilters: [
          { name: 'Hate', enabled: true, blocking: true, severityThreshold: 'Medium', source: 'Prompt' }
          { name: 'Sexual', enabled: true, blocking: true, severityThreshold: 'Medium', source: 'Prompt' }
          { name: 'Violence', enabled: true, blocking: true, severityThreshold: 'Medium', source: 'Prompt' }
          { name: 'SelfHarm', enabled: true, blocking: true, severityThreshold: 'Medium', source: 'Prompt' }
          { name: 'Hate', enabled: true, blocking: true, severityThreshold: 'Medium', source: 'Completion' }
          { name: 'Sexual', enabled: true, blocking: true, severityThreshold: 'Medium', source: 'Completion' }
          { name: 'Violence', enabled: true, blocking: true, severityThreshold: 'Medium', source: 'Completion' }
          { name: 'SelfHarm', enabled: true, blocking: true, severityThreshold: 'Medium', source: 'Completion' }
        ]
      }
    ]
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

var foundryEndpointWithSlash = endsWith(foundryService.outputs.endpoint, '/')
  ? foundryService.outputs.endpoint
  : '${foundryService.outputs.endpoint}/'
var foundryProjectEndpoint = foundryService.outputs.?defaultProjectEndpoint ?? '${foundryEndpointWithSlash}api/projects/${defaultProjectName}'

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

// NOTE: The foundry private endpoint is deployed AFTER the foundryRoleAssignments module
// completes to avoid a race condition where the account is still in "Accepted" state.
module foundryPrivateEndpoint 'br/public:avm/res/network/private-endpoint:0.12.0' = {
  name: 'foundry-private-endpoint-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    foundryRoleAssignments
  ]
  params: {
    name: foundryPrivateEndpointName
    location: location
    tags: tags
    subnetResourceId: virtualNetwork.outputs.subnetResourceIds[1] // privateEndpointSubnet
    privateLinkServiceConnections: [
      {
        name: foundryPrivateEndpointName
        properties: {
          privateLinkServiceId: foundryService.outputs.resourceId
          groupIds: [
            'account'
          ]
        }
      }
    ]
    privateDnsZoneGroup: {
      privateDnsZoneGroupConfigs: [
        {
          privateDnsZoneResourceId: foundryPrivateDnsZone.outputs.resourceId
        }
        {
          privateDnsZoneResourceId: openAiPrivateDnsZone.outputs.resourceId
        }
      ]
    }
  }
}

// --------- COSMOS DB (SERVERLESS) ---------
module cosmosDbAccount 'br/public:avm/res/document-db/database-account:0.19.0' = {
  name: 'cosmos-db-account-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: cosmosDbAccountName
    location: location
    tags: tags
    capabilitiesToAdd: [
      'EnableServerless'
      'EnableNoSQLVectorSearch'
    ]
    enableBurstCapacity: false
    disableLocalAuthentication: false
    disableKeyBasedMetadataWriteAccess: false
    zoneRedundant: false
    networkRestrictions: {
      publicNetworkAccess: enablePublicNetworkAccess ? 'Enabled' : 'Disabled'
    }
    privateEndpoints: [
      {
        name: cosmosDbPrivateEndpointName
        subnetResourceId: virtualNetwork.outputs.subnetResourceIds[1] // privateEndpointSubnet
        privateDnsZoneGroup: {
          privateDnsZoneGroupConfigs: [
            {
              privateDnsZoneResourceId: cosmosDbPrivateDnsZone.outputs.resourceId
            }
          ]
        }
        service: 'Sql'
      }
    ]
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
    sqlDatabases: [
      {
        name: cosmosDbDatabaseName
        containers: [
          {
            name: 'prompt-templates'
            paths: [
              '/userId'
            ]
          }
          // babbles container is deployed separately via babblesVectorContainer module
          // with vector search config. See: https://github.com/PlagueHO/prompt-babbler/issues/107
          {
            name: 'generated-prompts'
            paths: [
              '/babbleId'
            ]
          }
          {
            name: 'users'
            paths: [
              '/userId'
            ]
          }
        ]
      }
    ]
  }
}

// --------- COSMOS DB BABBLES VECTOR CONTAINER ---------
// Workaround: AVM document-db/database-account 0.19.0 does not support vectorEmbeddingPolicy
// on containers. This standalone module deploys the babbles container with vector search config
// using the raw ARM resource type. Remove when AVM 0.19.1 is published to MCR.
// Tracking issue: https://github.com/PlagueHO/prompt-babbler/issues/107
module babblesVectorContainer './cosmos-babbles-vector-container.bicep' = {
  name: 'cosmos-babbles-vector-container-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    cosmosDbAccount
  ]
  params: {
    cosmosDbAccountName: cosmosDbAccountName
    databaseName: cosmosDbDatabaseName
    location: location
    tags: tags
  }
}

// --------- CONTAINER APPS ENVIRONMENT ---------
module containerAppsEnvironment 'br/public:avm/res/app/managed-environment:0.13.2' = {
  name: 'container-apps-environment-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: containerAppsEnvironmentName
    location: location
    tags: tags
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsWorkspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
    }
    zoneRedundant: false
    // VNET integration - attach to the ACA subnet
    infrastructureSubnetResourceId: virtualNetwork.outputs.subnetResourceIds[0] // acaSubnet
    // internal: false means the environment remains publicly accessible (external ingress)
    internal: false
    // Allow public network access so smoke tests and external clients can reach the container apps
    publicNetworkAccess: 'Enabled'
  }
}

// --------- ASPIRE DASHBOARD ---------
module aspireDashboard 'aspire-dashboard.bicep' = {
  name: 'aspire-dashboard-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    containerAppsEnvironment
  ]
  params: {
    containerAppsEnvironmentName: containerAppsEnvironmentName
  }
}

// --------- CONTAINER APP (API) ---------
module containerApp 'br/public:avm/res/app/container-app:0.22.1' = {
  name: 'container-app-api-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
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
        image: containerImageApi
        resources: {
          cpu: json('0.5')
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
            value: foundryProjectEndpoint
          }
          {
            name: 'ConnectionStrings__cosmos'
            value: 'AccountEndpoint=${cosmosDbAccount.outputs.endpoint}'
          }
          {
            name: 'ConnectionStrings__foundry'
            value: 'Endpoint=${foundryService.outputs.endpoint}'
          }
          {
            name: 'ConnectionStrings__ai-foundry'
            value: 'Endpoint=${foundryProjectEndpoint}'
          }
          ...(!empty(apiClientId) ? [
            {
              name: 'AzureAd__ClientId'
              value: apiClientId
            }
            {
              name: 'AzureAd__TenantId'
              value: tenant().tenantId
            }
            {
              name: 'AzureAd__Instance'
              value: environment().authentication.loginEndpoint
            }
          ] : [])
          {
            name: 'CORS__AllowedOrigins'
            value: 'https://${staticWebApp.outputs.defaultHostname}'
          }
          ...(!empty(accessCode) ? [
            {
              name: 'ACCESS_CODE'
              value: accessCode
            }
          ] : [])
        ]
      }
    ]
    ingressExternal: true
    ingressTargetPort: 8080
    ingressTransport: 'auto'
    scaleSettings: {
      minReplicas: 0
      maxReplicas: 3
    }
  }
}

// Assign Cognitive Services OpenAI User + Speech User roles to the Container App's
// managed identity so it can call Foundry models and Speech Service via managed identity
var containerAppFoundryRoleAssignments = [
  {
    roleDefinitionIdOrName: 'Cognitive Services OpenAI User'
    principalType: 'ServicePrincipal'
    principalId: containerApp.outputs.?systemAssignedMIPrincipalId ?? ''
  }
  {
    roleDefinitionIdOrName: 'Cognitive Services Speech User'
    principalType: 'ServicePrincipal'
    principalId: containerApp.outputs.?systemAssignedMIPrincipalId ?? ''
  }
]

module containerAppFoundryRoles './core/security/role_foundry.bicep' = {
  name: 'container-app-foundry-roles-${resourceToken}'
  scope: az.resourceGroup(resourceGroupName)
  params: {
    foundryName: foundryName
    roleAssignments: containerAppFoundryRoleAssignments
  }
}

// Assign Cosmos DB Built-in Data Contributor role to the Container App's managed identity
// for data plane access. The built-in role GUID is 00000000-0000-0000-0000-000000000002.
module containerAppCosmosDbRoles './core/security/role_cosmosdb.bicep' = {
  name: 'container-app-cosmos-roles-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  params: {
    cosmosDbAccountName: cosmosDbAccountName
    sqlRoleAssignments: [
      {
        principalId: containerApp.outputs.?systemAssignedMIPrincipalId ?? ''
        roleDefinitionId: '00000000-0000-0000-0000-000000000002'
      }
    ]
  }
}

// --------- CONTAINER APP (MCP SERVER) ---------
module mcpServerContainerApp 'br/public:avm/res/app/container-app:0.22.1' = {
  name: 'container-app-mcp-server-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  params: {
    name: mcpServerContainerAppName
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    location: location
    tags: union(tags, {
      'azd-service-name': 'mcp-server'
    })
    managedIdentities: {
      systemAssigned: true
    }
    containers: [
      {
        name: 'mcp-server'
        image: containerImageMcpServer
        resources: {
          cpu: json('0.5')
          memory: '1Gi'
        }
        env: [
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: applicationInsights.outputs.connectionString
          }
          {
            name: 'services__api__https__0'
            value: 'https://${containerApp.outputs.fqdn}'
          }
          ...(!empty(mcpClientId) ? [
            {
              name: 'AzureAd__ClientId'
              value: mcpClientId
            }
            {
              name: 'AzureAd__TenantId'
              value: tenant().tenantId
            }
            {
              name: 'AzureAd__Instance'
              value: environment().authentication.loginEndpoint
            }
          ] : [])
          ...(!empty(accessCode) ? [
            {
              name: 'AccessControl__AccessCode'
              value: accessCode
            }
          ] : [])
        ]
      }
    ]
    ingressExternal: true
    ingressTargetPort: 8080
    ingressTransport: 'auto'
    scaleSettings: {
      minReplicas: 0
      maxReplicas: 3
    }
  }
}

// Assign Cosmos DB Built-in Data Contributor to the deploying principal for local dev
module principalCosmosDbRoles './core/security/role_cosmosdb.bicep' = if (!empty(principalId)) {
  name: 'principal-cosmos-roles-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    cosmosDbAccount
  ]
  params: {
    cosmosDbAccountName: cosmosDbAccountName
    sqlRoleAssignments: [
      {
        principalId: principalId
        roleDefinitionId: '00000000-0000-0000-0000-000000000002'
      }
    ]
  }
}

// --------- STATIC WEB APP (FRONTEND) ---------
module staticWebApp 'br/public:avm/res/web/static-site:0.9.4' = {
  name: 'static-web-app-deployment-${resourceToken}'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: staticWebAppName
    location: effectiveStaticWebAppLocation
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
output AZURE_AI_FOUNDRY_PROJECT_ENDPOINT string = foundryProjectEndpoint

// Container App (API)
output AZURE_CONTAINER_APP_NAME string = containerApp.outputs.name
output AZURE_CONTAINER_APP_FQDN string = containerApp.outputs.fqdn

// Container App (MCP Server)
output AZURE_MCP_SERVER_CONTAINER_APP_NAME string = mcpServerContainerApp.outputs.name
output AZURE_MCP_SERVER_CONTAINER_APP_FQDN string = mcpServerContainerApp.outputs.fqdn

// Static Web App (Frontend)
output AZURE_STATIC_WEB_APP_NAME string = staticWebApp.outputs.name
output AZURE_STATIC_WEB_APP_DEFAULT_HOSTNAME string = staticWebApp.outputs.defaultHostname

// Cosmos DB
output COSMOS_DB_ACCOUNT_NAME string = cosmosDbAccount.outputs.name
output COSMOS_DB_ENDPOINT string = cosmosDbAccount.outputs.endpoint

// Entra ID (set via preprovision hook when ENABLE_ENTRA_AUTH=true)
output AZURE_AD_API_CLIENT_ID string = apiClientId
output AZURE_AD_SPA_CLIENT_ID string = spaClientId
output AZURE_AD_MCP_CLIENT_ID string = mcpClientId
output AZURE_AD_TENANT_ID string = tenant().tenantId

// Networking
output VNET_NAME string = virtualNetwork.outputs.name
output VNET_RESOURCE_ID string = virtualNetwork.outputs.resourceId
output ACA_SUBNET_RESOURCE_ID string = virtualNetwork.outputs.subnetResourceIds[0]
output PRIVATE_ENDPOINT_SUBNET_RESOURCE_ID string = virtualNetwork.outputs.subnetResourceIds[1]
