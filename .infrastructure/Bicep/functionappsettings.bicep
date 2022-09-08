// --------------------------------------------------------------------------------
// This BICEP file will add unique Configuration settings to a web or function app
// ----------------------------------------------------------------------------------------------------
// To deploy this Bicep manually:
//   az deployment group create -n main-deploy-20220820T140100Z --resource-group rg_iotdemo_dev --template-file 'functionAppSettings.bicep' --parameters functionAppName='xxx-iotdemo-process-dev' functionStorageAccountName='xxxiotdemofuncdevstore' functionInsightsName='xxx-iotdemo-process-dev-insights' customAppSettings="{'dateTested':'20220820T140100Z'}" 
// --------------------------------------------------------------------------------
param functionAppName string
param functionStorageAccountName string
param functionInsightsKey string
param customAppSettings object

resource storageAccountResource 'Microsoft.Storage/storageAccounts@2019-06-01' existing = { name: functionStorageAccountName }
var storageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountResource.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccountResource.id, storageAccountResource.apiVersion).keys[0].value}'

var BASE_SLOT_APPSETTINGS = {
  AzureWebJobsStorage: storageAccountConnectionString
  WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: storageAccountConnectionString
  WEBSITE_CONTENTSHARE: functionAppName
  APPINSIGHTS_INSTRUMENTATIONKEY: functionInsightsKey
  APPLICATIONINSIGHTS_CONNECTION_STRING: 'InstrumentationKey=${functionInsightsKey}'
  FUNCTIONS_WORKER_RUNTIME: 'dotnet'
  FUNCTIONS_EXTENSION_VERSION: '~4'
}

// This *should* work, but I keep getting a "circular dependency detected" error and it doesn't work
// resource appResource 'Microsoft.Web/sites@2021-03-01' existing = { name: functionAppName }
// var BASE_SLOT_APPSETTINGS = list('${appResource.id}/config/appsettings', appResource.apiVersion).properties

resource siteConfig 'Microsoft.Web/sites/config@2021-02-01' = {
  name: '${functionAppName}/appsettings'
  properties: union(BASE_SLOT_APPSETTINGS, customAppSettings)
}
