﻿// --------------------------------------------------------------------------------
// Main file that deploys all Azure Resources for one environment
// --------------------------------------------------------------------------------
// Note: To deploy this Bicep manually:
// 	 az login
//   az account set --subscription <subscriptionId>
//   az deployment group create -n main-deploy-20220823T110000Z --resource-group rg_iotdemo_dev --template-file 'main.bicep' --parameters orgPrefix=lll appPrefix=iotdemo environmentCode=dev keyVaultOwnerUserId1=xxxxxxxx-xxxx-xxxx keyVaultOwnerUserId2=xxxxxxxx-xxxx-xxxx
//   az deployment group create -n main-deploy-20220823T110000Z --resource-group rg_iotdemo_qa  --template-file 'main.bicep' --parameters orgPrefix=lll appPrefix=iotdemo environmentCode=qa  keyVaultOwnerUserId1=xxxxxxxx-xxxx-xxxx keyVaultOwnerUserId2=xxxxxxxx-xxxx-xxxx
// --------------------------------------------------------------------------------
// Bicep Container Registry Notes: 
// If you use a Bicep Container Registry, your service principal may need to be in the
//   "acr pull" role for the container registry.
// To use a Bicep Container Registry, use this syntax:
//   module storageModule 'br/mybiceprepository:storageaccount:2022-08-31.335' = {
// Tip: To list the available bicep container registry image tags:
//   $registryName = 'lllbicepregistry'
//   Write-Host "Scanning for repository tags in $registryName"
//   az acr repository list --name $registryName -o tsv | Foreach-Object { 
//     $thisModule = $_
//     az acr repository show-tags --name $registryName --repository $_ --output tsv  | Foreach-Object { 
//       Write-Host "$thisModule`:$_"
//     }
//   }
// --------------------------------------------------------------------------------
param environmentCode string = 'dev'
param location string = resourceGroup().location
param orgPrefix string = 'org'
param appPrefix string = 'app'
param appSuffix string = '' // '-1' 
param storageSku string = 'Standard_LRS'
param functionAppSku string = 'Y1'
param functionAppSkuFamily string = 'Y'
param functionAppSkuTier string = 'Dynamic'
param keyVaultOwnerUserId1 string = ''
param keyVaultOwnerUserId2 string = ''
param runDateTime string = utcNow()
param webSiteSku string = 'B1'

// --------------------------------------------------------------------------------
var deploymentSuffix = '-${runDateTime}'

module storageModule 'storageaccount.bicep' = {
  name: 'storage${deploymentSuffix}'
  params: {
    storageSku: storageSku

    templateFileName: 'storageaccount.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}

module iotHubModule 'iothub.bicep' = {
  name: 'iotHub${deploymentSuffix}'
  params: {
    templateFileName: 'iotHub.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}
module dpsModule 'dps.bicep' = {
  name: 'dps${deploymentSuffix}'
  dependsOn: [ iotHubModule ]
  params: {
    iotHubName: iotHubModule.outputs.iotHubName

    templateFileName: 'dps.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}

module signalRModule 'signalr.bicep' = {
  name: 'signalR${deploymentSuffix}'
  params: {
    templateFileName: 'signalr.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}

module servicebusModule 'servicebus.bicep' = {
  name: 'servicebus${deploymentSuffix}'
  params: {
    queueNames: [ 'iotmsgs', 'filemsgs' ]

    templateFileName: 'servicebus.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}

module streamingModule 'streaming.bicep' = {
  name: 'streaming${deploymentSuffix}'
  params: {
    iotHubName: iotHubModule.outputs.iotHubName
    svcBusName: servicebusModule.outputs.serviceBusName
    svcBusQueueName: 'iotmsgs'

    templateFileName: 'streaming.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}

var cosmosContainerArray = [
  { name: 'DeviceData', partitionKey: '/partitionKey' }
  { name: 'DeviceInfo', partitionKey: '/partitionKey' }
]
module cosmosModule 'cosmosdatabase.bicep' = {
  name: 'cosmos${deploymentSuffix}'
  params: {
    containerArray: cosmosContainerArray
    cosmosDatabaseName: 'IoTDatabase'

    templateFileName: 'cosmosdatabase.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}

module functionModule 'functionapp.bicep' = {
  name: 'function${deploymentSuffix}'
  dependsOn: [ storageModule ]
  params: {
    functionName: 'process'
    functionKind: 'functionapp'
    functionAppSku: functionAppSku
    functionAppSkuFamily: functionAppSkuFamily
    functionAppSkuTier: functionAppSkuTier
    functionStorageAccountName: storageModule.outputs.functionStorageAccountName
    appInsightsLocation: location

    templateFileName: 'functionapp.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}

module webSiteModule 'website.bicep' = {
  name: 'webSite${deploymentSuffix}'
  params: {
    appInsightsLocation: location
    sku: webSiteSku

    templateFileName: 'website.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}

module keyVaultModule 'keyvault.bicep' = {
  name: 'keyvault${deploymentSuffix}'
  dependsOn: [ functionModule, webSiteModule ]
  params: {
    adminUserObjectIds: [ keyVaultOwnerUserId1, keyVaultOwnerUserId2 ]
    applicationUserObjectIds: [ functionModule.outputs.functionAppPrincipalId, webSiteModule.outputs.websiteAppPrincipalId ]

    templateFileName: 'keyvault.bicep'
    orgPrefix: orgPrefix
    appPrefix: appPrefix
    environmentCode: environmentCode
    appSuffix: appSuffix
    location: location
    runDateTime: runDateTime
  }
}

module keyVaultSecret1 'keyvaultsecretiothubconnection.bicep' = {
  name: 'keyVaultSecret1${deploymentSuffix}'
  dependsOn: [ keyVaultModule, iotHubModule ]
  params: {
    keyVaultName: keyVaultModule.outputs.keyVaultName
    keyName: 'iotHubConnectionString'
    iotHubName: iotHubModule.outputs.iotHubName
  }
}

module keyVaultSecret2 'keyvaultsecretstorageconnection.bicep' = {
  name: 'keyVaultSecret2${deploymentSuffix}'
  dependsOn: [ keyVaultModule, iotHubModule ]
  params: {
    keyVaultName: keyVaultModule.outputs.keyVaultName
    keyName: 'iotStorageAccountConnectionString'
    storageAccountName: iotHubModule.outputs.iotStorageAccountName
  }
}

module keyVaultSecret3 'keyvaultsecretsignalrconnection.bicep' = {
  name: 'keyVaultSecret3${deploymentSuffix}'
  dependsOn: [ keyVaultModule, signalRModule ]
  params: {
    keyVaultName: keyVaultModule.outputs.keyVaultName
    keyName: 'signalRConnectionString'
    signalRName: signalRModule.outputs.signalRName
  }
}

module keyVaultSecret4 'keyvaultsecretcosmosconnection.bicep' = {
  name: 'keyVaultSecret4${deploymentSuffix}'
  dependsOn: [ keyVaultModule, cosmosModule ]
  params: {
    keyVaultName: keyVaultModule.outputs.keyVaultName
    keyName: 'cosmosConnectionString'
    cosmosAccountName: cosmosModule.outputs.cosmosAccountName
  }
}

module keyVaultSecret5 'keyvaultsecretservicebusconnection.bicep' = {
  name: 'keyVaultSecret5${deploymentSuffix}'
  dependsOn: [ keyVaultModule, servicebusModule ]
  params: {
    keyVaultName: keyVaultModule.outputs.keyVaultName
    keyName: 'serviceBusConnectionString'
    serviceBusName: servicebusModule.outputs.serviceBusName
  }
}

module keyVaultSecret6 'keyvaultsecret.bicep' = {
  name: 'keyVaultSecret6${deploymentSuffix}'
  dependsOn: [ keyVaultModule, functionModule ]
  params: {
    keyVaultName: keyVaultModule.outputs.keyVaultName
    secretName: 'functionInsightsKey'
    secretValue: functionModule.outputs.functionInsightsKey
  }
}

module keyVaultSecret7 'keyvaultsecret.bicep' = {
  name: 'keyVaultSecret7${deploymentSuffix}'
  dependsOn: [ keyVaultModule, webSiteModule ]
  params: {
    keyVaultName: keyVaultModule.outputs.keyVaultName
    secretName: 'webSiteInsightsKey'
    secretValue: webSiteModule.outputs.webSiteAppInsightsKey
  }
}  

module functionAppSettingsModule 'functionappsettings.bicep' = {
  name: 'functionAppSettings${deploymentSuffix}'
  dependsOn: [ keyVaultSecret1, keyVaultSecret2, keyVaultSecret3, keyVaultSecret4, keyVaultSecret5, keyVaultSecret6, keyVaultSecret7 ]
  params: {
    functionAppName: functionModule.outputs.functionAppName
    functionStorageAccountName: functionModule.outputs.functionStorageAccountName
    functionInsightsKey: functionModule.outputs.functionInsightsKey
    customAppSettings: {
      ServiceBusConnectionString: '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=serviceBusConnectionString)'
      'MySecrets:IoTHubConnectionString': '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=iotHubConnectionString)'
      'MySecrets:SignalRConnectionString': '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=signalRConnectionString)'
      'MySecrets:ServiceBusConnectionString': '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=serviceBusConnectionString)'
      'MySecrets:CosmosConnectionString': '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=cosmosConnectionString)'
      'MySecrets:IotStorageAccountConnectionString': '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=iotStorageAccountConnectionString)'
      'MyConfiguration:WriteToCosmosYN': 'Y'
      'MyConfiguration:WriteToSignalRYN': 'N'
    }
  }
}

module webSiteAppSettingsModule 'websiteappsettings.bicep' = {
  name: 'webSiteAppSettings${deploymentSuffix}'
  dependsOn: [ keyVaultSecret1, keyVaultSecret2, keyVaultSecret3, keyVaultSecret4, keyVaultSecret5, keyVaultSecret6, keyVaultSecret7 ]
  params: {
    webAppName: webSiteModule.outputs.webSiteName
    customAppSettings: {
      EnvironmentName: environmentCode
      IoTHubConnectionString: '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=iotHubConnectionString)'
      StorageConnectionString: '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=iotStorageAccountConnectionString)'
      CosmosConnectionString: '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=cosmosConnectionString)'
      SignalRConnectionString: '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=signalRConnectionString)'
      ApplicationInsightsKey: '@Microsoft.KeyVault(VaultName=${keyVaultModule.outputs.keyVaultName};SecretName=webSiteInsightsKey)'
    }
  }
}