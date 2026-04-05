@sys.description('Name of the Container Apps Managed Environment to enable the Aspire Dashboard on.')
param containerAppsEnvironmentName string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' existing = {
  name: containerAppsEnvironmentName
}

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
  name: 'aspire-dashboard'
  parent: containerAppsEnvironment
  properties: {
    componentType: 'AspireDashboard'
  }
}
