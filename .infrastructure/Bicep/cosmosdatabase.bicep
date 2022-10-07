// --------------------------------------------------------------------------------
// This BICEP file will create a Cosmos Database
// This expects a parameter with a list of containers/keys, something like this:
//   var cosmosContainerArray = [
//     { name: 'products', partitionKey: '/category' }
//     { name: 'orders',   partitionKey: '/customerNumber' } 
//   ]
// --------------------------------------------------------------------------------
param orgPrefix string = 'org'
param appPrefix string = 'app'
@allowed(['dev','demo','qa','stg','prod'])
param environmentCode string = 'dev'
param appSuffix string = '1'
param location string = resourceGroup().location
param runDateTime string = utcNow()
param templateFileName string = '~cosmosDatabase.bicep'
param containerArray array = []
param cosmosDatabaseName string = 'MyDatabase'

// --------------------------------------------------------------------------------
var cosmosAccountName = '${orgPrefix}-${appPrefix}-cosmos-${environmentCode}${appSuffix}'

// --------------------------------------------------------------------------------
resource cosmosAccountResource 'Microsoft.DocumentDB/databaseAccounts@2022-05-15' = {
  name: cosmosAccountName
  location: location
  tags: {
    LastDeployed: runDateTime
    TemplateFile: templateFileName
    defaultExperience: 'Core (SQL)'
    CosmosAccountType: 'Non-Production'
    Organization: orgPrefix
    Application: appPrefix
    Environment: environmentCode
  }
  kind: 'GlobalDocumentDB'
  identity: {
    type: 'None'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    isVirtualNetworkFilterEnabled: false
    virtualNetworkRules: []
    disableKeyBasedMetadataWriteAccess: false
    enableFreeTier: false
    enableAnalyticalStorage: false
    createMode: 'Default'
    databaseAccountOfferType: 'Standard'
    defaultIdentity: 'FirstPartyIdentity'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    ipRules: []
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: 240
        backupRetentionIntervalInHours: 8
      }
    }
    networkAclBypassResourceIds: []
    capacity: {
      totalThroughputLimit: 4000
    }
  }
}

resource cosmosDbResource 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2020-06-01-preview' = {
    name: '${cosmosAccountResource.name}/${cosmosDatabaseName}'
    properties: {
        resource: {
            id: cosmosDatabaseName
        }
        options: {
        }
    }
}

resource containerResources 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2020-06-01-preview' = [for container in containerArray: {
    name: '${cosmosDbResource.name}/${container.name}'
    properties: {
        resource: {
            id: container.name
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
                ]
            }
            partitionKey: {
                paths: [
                    container.partitionKey
                ]
                kind: 'Hash'
            }
            conflictResolutionPolicy: {
                mode: 'LastWriterWins'
                conflictResolutionPath: '/_ts'
            }
        }
        options: {
        }
    }
}]

// --------------------------------------------------------------------------------
output cosmosAccountName string = cosmosAccountResource.name
output cosmosAccountId string = cosmosAccountResource.id
output cosmosAccountApiVersion string = cosmosAccountResource.apiVersion
