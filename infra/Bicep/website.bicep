// --------------------------------------------------------------------------------
// This BICEP file will create a Azure Website
// --------------------------------------------------------------------------------
param webSiteName string = 'myWebSiteName'
param webSiteAppServicePlanName string = 'myWebSiteAppServicePlanName'
param webSiteAppInsightsName string = 'myWebSiteAppInsightsName'
param location string = resourceGroup().location
param commonTags object = {}

param appInsightsLocation string = resourceGroup().location
@allowed(['F1','B1','B2','S1','S2','S3'])
param sku string = 'F1'
param linuxFxVersion string = 'DOTNETCORE|6.0'

@description('The workspace to store audit logs.')
param workspaceId string = ''

// --------------------------------------------------------------------------------
var templateTag = { TemplateFile: '~website.bicep' }
var tags = union(commonTags, templateTag)

// --------------------------------------------------------------------------------
resource appServiceResource 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: webSiteAppServicePlanName
  location: location
  tags: tags
  sku: {
    name: sku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webSiteResource 'Microsoft.Web/sites@2020-06-01' = {
  name: webSiteName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    serverFarmId: appServiceResource.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      appSettings: []
      minTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
      remoteDebuggingEnabled: false
    }
  }
}

resource appInsightsClassicResource 'Microsoft.Insights/components@2020-02-02' = if (workspaceId == '') {
  name: webSiteAppInsightsName
  location: appInsightsLocation
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource appInsightsResource 'Microsoft.Insights/components@2020-02-02' = if (workspaceId != '') {
  name: webSiteAppInsightsName
  location: appInsightsLocation
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: workspaceId
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (workspaceId != '') {
  name: webSiteResource.name // appServiceResource.name
  scope: webSiteResource     // appServiceResource
  properties: {
    workspaceId: workspaceId
    logs: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          days: 30
          enabled: true 
        }
      }
    ]
  }
}

output principalId string = webSiteResource.identity.principalId
output name string = webSiteResource.name
output insightsName string = webSiteAppInsightsName
output insightsKey string = appInsightsResource.properties.InstrumentationKey
