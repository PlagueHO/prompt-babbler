metadata name = 'Cosmos DB SQL Role Assignments'
metadata description = 'Creates SQL data-plane role assignments on an existing Cosmos DB account without redeploying the account resource.'

@description('Required. The name of the existing Cosmos DB account.')
param cosmosDbAccountName string

@description('Required. Array of role assignments to create.')
param sqlRoleAssignments sqlRoleAssignmentType[]

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosDbAccountName
}

resource roleAssignments 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-11-15' = [
  for (assignment, index) in sqlRoleAssignments: {
    parent: cosmosDbAccount
    name: guid(
      '${cosmosDbAccount.id}/sqlRoleDefinitions/${assignment.roleDefinitionId}',
      assignment.principalId,
      cosmosDbAccount.id
    )
    properties: {
      principalId: assignment.principalId
      roleDefinitionId: '${cosmosDbAccount.id}/sqlRoleDefinitions/${assignment.roleDefinitionId}'
      scope: cosmosDbAccount.id
    }
  }
]

// =============== //
//   Definitions   //
// =============== //

@export()
type sqlRoleAssignmentType = {
  @description('Required. The principal ID to assign the role to.')
  principalId: string
  @description('Required. The Cosmos DB built-in role definition GUID (e.g., 00000000-0000-0000-0000-000000000002 for Data Contributor).')
  roleDefinitionId: string
}
