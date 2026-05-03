@sys.description('Name of the Cosmos DB account.')
param cosmosDbAccountName string

@sys.description('Name of the SQL database.')
param databaseName string

@sys.description('Location for the resource.')
param location string

@sys.description('Tags to apply to the resource.')
param tags object = {}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosDbAccountName

  resource database 'sqlDatabases' existing = {
    name: databaseName
  }
}

resource babblesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: cosmosDbAccount::database
  name: 'babbles'
  location: location
  tags: tags
  properties: {
    resource: {
      id: 'babbles'
      partitionKey: {
        paths: [
          '/userId'
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
          {
            path: '/contentVector/*'
          }
        ]
        vectorIndexes: [
          {
            path: '/contentVector'
            type: 'quantizedFlat'
          }
        ]
      }
      vectorEmbeddingPolicy: {
        vectorEmbeddings: [
          {
            path: '/contentVector'
            dataType: 'float32'
            distanceFunction: 'cosine'
            dimensions: 1536
          }
        ]
      }
    }
  }
}
