// --------------------------------------------------------------------------------
// This BICEP file will create KeyVault secret for a service bus connection
// --------------------------------------------------------------------------------
param keyVaultName string = ''
param keyName string = ''
param serviceBusName string = ''
param accessKeyName string = 'RootManageSharedAccessKey'

// --------------------------------------------------------------------------------
resource serviceBusResource 'Microsoft.ServiceBus/namespaces@2021-11-01' existing = { name: serviceBusName }
var serviceBusEndpoint = '${serviceBusResource.id}/AuthorizationRules/RootManageSharedAccessKey' 
var serviceBusConnectionString       = 'Endpoint=sb://${serviceBusResource.name}.servicebus.windows.net/;SharedAccessKeyName=${accessKeyName};SharedAccessKey=${listKeys(serviceBusEndpoint, serviceBusResource.apiVersion).primaryKey}' 

// --------------------------------------------------------------------------------
resource keyvaultResource 'Microsoft.KeyVault/vaults@2021-11-01-preview' existing = { 
  name: keyVaultName
  resource serviceBusSecret 'secrets' = {
    name: keyName
    properties: {
      value: serviceBusConnectionString
    }
  }
}
