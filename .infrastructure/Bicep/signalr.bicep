// --------------------------------------------------------------------------------
// This BICEP file will create a SignalR host
// --------------------------------------------------------------------------------
param orgPrefix string = 'org'
param appPrefix string = 'app'
@allowed(['dev','qa','stg','prod'])
param environmentCode string = 'dev'
param appSuffix string = '1'
param location string = resourceGroup().location
param runDateTime string = utcNow()
param templateFileName string = '~signalr.bicep'
param sku string = 'Free_F1'	 // Required, the name of the SKU. Allowed values: Standard_S1, Free_F1

param skuTier	string = 'Free'  // Optional tier of this particular SKU. 'Standard' or 'Free' or 'Premium'
//param skuCapacity int = 1    // Optional, integer. The unit count of the resource. 1 by default. Allowed: Free: 1; Standard: 1,2,5,10,20,50,100

// --------------------------------------------------------------------------------
var signalRName = '${orgPrefix}-${appPrefix}-signal-${environmentCode}${appSuffix}'

// --------------------------------------------------------------------------------
resource signalRResource 'Microsoft.SignalRService/SignalR@2022-02-01' = {
  name: signalRName
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
    tier: skuTier
    capacity: 1
  }
  kind: 'SignalR'
  properties: {
    tls: {
      clientCertEnabled: false
    }
    features: [
      {
        flag: 'ServiceMode'
        value: 'Default'
        properties: {
        }
      }
      {
        flag: 'EnableConnectivityLogs'
        value: 'True'
        properties: {
        }
      }
    ]
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
    upstream: {
    }
    networkACLs: {
      defaultAction: 'Deny'
      publicNetwork: {
        allow: [
          'ServerConnection'
          'ClientConnection'
          'RESTAPI'
          'Trace'
        ]
      }
      privateEndpoints: []
    }
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    disableAadAuth: false
  }
}

// --------------------------------------------------------------------------------
output signalRName string = signalRResource.name
output signalRId string = signalRResource.id
output signalRApiVersion string = signalRResource.apiVersion
