// --------------------------------------------------------------------------------
// This BICEP file will create a KeyVault secret for Cosmos
// --------------------------------------------------------------------------------
param keyVaultName string = ''
param keyName string = ''
param cosmosAccountName string = ''

// --------------------------------------------------------------------------------
resource cosmosResource 'Microsoft.DocumentDB/databaseAccounts@2022-02-15-preview' existing = { name: cosmosAccountName }
var cosmosKey = '${listKeys(cosmosResource.id, cosmosResource.apiVersion).primaryMasterKey}'
var cosmosConnectionString = 'AccountEndpoint=https://${cosmosAccountName}.documents.azure.com:443/;AccountKey=${cosmosKey}'

// --------------------------------------------------------------------------------
resource keyvaultResource 'Microsoft.KeyVault/vaults@2021-11-01-preview' existing = { 
  name: keyVaultName
  resource cosmosSecret 'secrets' = {
    name: keyName
    properties: {
      value: cosmosConnectionString
    }
  }
}
