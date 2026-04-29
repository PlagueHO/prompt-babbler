@sys.description('Name of the Container Apps Managed Environment to enable the Aspire Dashboard on.')
param containerAppsEnvironmentName string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2026-01-01' existing = {
  name: containerAppsEnvironmentName
}

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2025-10-02-preview' = {
  name: 'aspire-dashboard'
  parent: containerAppsEnvironment
  properties: {
    componentType: 'AspireDashboard'
  }
}
