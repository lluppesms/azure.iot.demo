// --------------------------------------------------------------------------------
// This BICEP file will create a linked IoT Hub and DPS Service
// --------------------------------------------------------------------------------
// NOTE: there is no way yet to automate DPS Enrollment Group creation.
//   After DPS is created, you will need to manually create a group based on
//   the certificate that is created.
// --------------------------------------------------------------------------------

param orgPrefix string = 'org'
param appPrefix string = 'app'
@allowed(['dev','demo','qa','stg','prod'])
param environmentCode string = 'dev'
param appSuffix string = '1'
param location string = resourceGroup().location
param runDateTime string = utcNow()
param templateFileName string = '~iothub.bicep'
@allowed(['F1','S1','S2','S3'])
param sku string = 'S1'

// --------------------------------------------------------------------------------
var iotHubName            = '${orgPrefix}-${appPrefix}-hub-${environmentCode}${appSuffix}'
var iotStorageAccountName = '${orgPrefix}${appPrefix}stghub${environmentCode}${appSuffix}'
var iotStorageContainerName = 'iothubuploads'

// --------------------------------------------------------------------------------
// create a storage account for the Iot Hub to use
resource iotStorageAccountResource 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: iotStorageAccountName
  location: location
  tags: {
    LastDeployed: runDateTime
    TemplateFile: templateFileName
    Organization: orgPrefix
    Application: appPrefix
    Environment: environmentCode
  }
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}
// --------------------------------------------------------------------------------
// create a container inside that storage account
resource iotStorageContainerResource 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-02-01' = {
  name: '${iotStorageAccountName}/default/${iotStorageContainerName}'
  dependsOn: [
    iotStorageAccountResource
  ]}
var iotStorageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${iotStorageAccountResource.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(iotStorageAccountResource.id, iotStorageAccountResource.apiVersion).keys[0].value}'

// --------------------------------------------------------------------------------
// create an IoT Hub and link it to the Storage Container
resource iotHubResource 'Microsoft.Devices/IotHubs@2021-07-02' = {
  name: iotHubName
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
    capacity: 1
  }
  identity: {
    type: 'None'
  }
  properties: {
    // routing: {
    //   endpoints: {
    //     serviceBusQueues: [
    //       {
    //         connectionString: svcBusConnectionString
    //         authenticationType: 'keyBased'
    //         name: 'datatosvcbusroute'
    //         // id: 'faadc3d2-62e3-40a6-b3b0-13d4c9994018'
    //         // subscriptionId: subscription().id  
    //         // resourceGroup: resourceGroup().name
    //       }
    //     ]
    //     serviceBusTopics: []
    //     eventHubs: []
    //     storageContainers: []
    //   }
    //   routes: [
    //     {
    //       name: 'RouteToEventGrid'
    //       source: 'DeviceMessages'
    //       condition: 'true'
    //       endpointNames: [
    //         'eventgrid'
    //       ]
    //       isEnabled: true
    //     }
    //   ]
    //   fallbackRoute: {
    //     name: '$fallback'
    //     source: 'DeviceMessages'
    //     condition: 'true'
    //     endpointNames: [
    //       'events'
    //     ]
    //     isEnabled: true
    //   }
    // }
    storageEndpoints: {
      '$default': {
        sasTtlAsIso8601: 'PT1H'
        connectionString: iotStorageAccountConnectionString
        containerName: iotStorageContainerName
        authenticationType: 'keyBased'
      }
    }
    messagingEndpoints: {
      fileNotifications: {
        lockDurationAsIso8601: 'PT1M'
        ttlAsIso8601: 'PT1H'
        maxDeliveryCount: 10
      }
    }
    enableFileUploadNotifications: true
    cloudToDevice: {
      maxDeliveryCount: 10
      defaultTtlAsIso8601: 'PT1H'
      feedback: {
        lockDurationAsIso8601: 'PT1M'
        ttlAsIso8601: 'PT1H'
        maxDeliveryCount: 10
      }
    }
    features: 'None'
    minTlsVersion: '1.2'
    disableLocalAuth: false
    allowedFqdnList: []
    enableDataResidency: false
  }
}

// --------------------------------------------------------------------------------
output iotHubName string = iotHubResource.name
output iotHubNameId string = iotHubResource.id
output iotHubNameApiVersion string = iotHubResource.apiVersion
output iotStorageAccountName string = iotStorageAccountName
output iotStorageContainerName string = iotStorageContainerName
