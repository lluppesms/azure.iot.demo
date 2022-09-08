// --------------------------------------------------------------------------------
// This BICEP file will create a Azure Function
// --------------------------------------------------------------------------------

param orgPrefix string = 'org'
param appPrefix string = 'app'
@allowed(['dev','qa','stg','prod'])
param environmentCode string = 'dev'
param appSuffix string = '1'
param location string = resourceGroup().location
param appInsightsLocation string = resourceGroup().location
param runDateTime string = utcNow()
param templateFileName string = '~website.bicep'
@allowed(['F1','B1','B2','S1','S2','S3'])
param sku string = 'F1'

param webAppName string = 'dashboard'

// --------------------------------------------------------------------------------
var linuxFxVersion = 'DOTNETCORE|6.0' // 	The runtime stack of web app
var webSiteName = toLower('${orgPrefix}-${appPrefix}-${webAppName}-${environmentCode}${appSuffix}')
var webSiteAppServicePlanName = toLower('${webSiteName}-appsvc')
var webSiteAppInsightsName = toLower('${webSiteName}-insights')

// --------------------------------------------------------------------------------
resource webSiteAppServicePlanResource 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: webSiteAppServicePlanName
  location: location
  tags: {
    LastDeployed: runDateTime
    TemplateFile: templateFileName
    Organization: orgPrefix
    Application: appPrefix
    Environment: environmentCode
  }
  sku: {
    name: sku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webSiteAppServiceResource 'Microsoft.Web/sites@2020-06-01' = {
  name: webSiteName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: webSiteAppServicePlanResource.id
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      appSettings: []
    }
  }
}

resource webSiteAppInsightsResource 'Microsoft.Insights/components@2020-02-02' = {
  name: webSiteAppInsightsName
  location: appInsightsLocation
  tags: {
    LastDeployed: runDateTime
    TemplateFile: templateFileName
    Organization: orgPrefix
    Application: appPrefix
    Environment: environmentCode
  }
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

output websiteAppPrincipalId string = webSiteAppServiceResource.identity.principalId
output webSiteName string = webSiteName
output webSiteAppInsightsName string = webSiteAppInsightsName
output webSiteAppInsightsKey string = webSiteAppInsightsResource.properties.InstrumentationKey
