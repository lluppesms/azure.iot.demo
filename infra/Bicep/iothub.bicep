// --------------------------------------------------------------------------------
// This BICEP file will create a linked IoT Hub and DPS Service
// --------------------------------------------------------------------------------
// NOTE: there is no way yet to automate DPS Enrollment Group creation.
//   After DPS is created, you will need to manually create a group based on
//   the certificate that is created.
// --------------------------------------------------------------------------------
param iotHubName string = 'myIoTHubAccountName'
param iotStorageAccountName string = 'myIotStorageAccountName'
param iotStorageContainerName string = 'iothubuploads'
param location string = resourceGroup().location
param commonTags object = {}
@allowed(['F1','S1','S2','S3'])
param sku string = 'S1'
@allowed(['Allow','Deny'])
param allowStorageNetworkAccess string = 'Allow'
//param serviceBusName string = ''

@description('The workspace to store audit logs.')
param workspaceId string = ''

// --------------------------------------------------------------------------------
var templateTag = { TemplateFile: '~iothub.bicep' }
var tags = union(commonTags, templateTag)

// --------------------------------------------------------------------------------
// var serviceBusAccessKeyName = 'RootManageSharedAccessKey'
// var queueName = 'connectionevents'
// resource serviceBusResource 'Microsoft.ServiceBus/namespaces@2021-11-01' existing = { name: serviceBusName }
//var serviceBusEndpoint = '${serviceBusResource.id}/AuthorizationRules/${serviceBusAccessKeyName}' 
//var serviceBusKey = '${listKeys(serviceBusEndpoint, serviceBusResource.apiVersion).primaryKey}'
//var serviceBusConnectionString = 'Endpoint=sb://${serviceBusResource.name}.servicebus.windows.net/;SharedAccessKeyName=${serviceBusAccessKeyName};SharedAccessKey=${serviceBusKey};EntityPath=${queueName}' 
// resource serviceBusQueueResource 'Microsoft.ServiceBus/namespaces/queues@2022-01-01-preview' existing = {
//   parent: serviceBusResource
//   name: queueName
// }

// --------------------------------------------------------------------------------
// create a storage account for the Iot Hub to use
resource iotStorageAccountResource 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: iotStorageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  tags: tags
  kind: 'StorageV2'
  properties: {
      networkAcls: {
          bypass: 'AzureServices'
          defaultAction: allowStorageNetworkAccess
          ipRules: []
          // ipRules: (empty(ipRules) ? json('[]') : ipRules)
          virtualNetworkRules: []
          //virtualNetworkRules: ((virtualNetworkType == 'External') ? json('[{"id": "${subscription().id}/resourceGroups/${vnetResource}/providers/Microsoft.Network/virtualNetworks/${vnetResource.name}/subnets/${subnetName}"}]') : json('[]'))
      }
      supportsHttpsTrafficOnly: true
      encryption: {
          services: {
              file: {
                  keyType: 'Account'
                  enabled: true
              }
              blob: {
                  keyType: 'Account'
                  enabled: true
              }
          }
          keySource: 'Microsoft.Storage'
      }
      accessTier: 'Hot'
      allowBlobPublicAccess: false
      minimumTlsVersion: 'TLS1_2'
  }
}
var iotStorageKey = iotStorageAccountResource.listKeys().keys[0].value
var iotStorageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${iotStorageAccountResource.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${iotStorageKey}'

// --------------------------------------------------------------------------------
// create a container inside that storage account
resource iotStorageBlobResource 'Microsoft.Storage/storageAccounts/blobServices@2019-06-01' = {
  parent: iotStorageAccountResource
  name: 'default'
  properties: {
      cors: {
          corsRules: [
          ]
      }
      deleteRetentionPolicy: {
          enabled: true
          days: 7
      }
  }
}
resource iotStorageContainerResource 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-05-01' = {
  parent: iotStorageBlobResource
  name: iotStorageContainerName
  properties: {
    publicAccess: 'None'
    metadata: {}
  }
}

// --------------------------------------------------------------------------------
// create an IoT Hub and link it to the Storage Container
resource iotHubResource 'Microsoft.Devices/IotHubs@2022-04-30-preview' = {
  name: iotHubName
  location: location
  tags: tags
  sku: {
    name: sku
    capacity: 1
  }
  identity: {
    type: 'SystemAssigned' // type: 'None'
  }
  properties: {
    // // this snipped creates a Message Routing Custom endpoint
    // routing: {
    //   endpoints: {
    //     serviceBusQueues: [
    //       {
    //         connectionString: serviceBusConnectionString
    //         authenticationType: 'keyBased'
    //         name: 'connectionEventsQueue'
    //       }
    //     ]
    //   }
    //   routes: [
    //     {
    //       name: 'RouteConnectionsToEventGrid'
    //       // Valid sources are: devicemessages, deviceconnectionstateevents, twinchangeevents, digitaltwinchangeevents, devicejoblifecycleevents, devicelifecycleevents, or invalid 
    //       source: 'DeviceConnectionStateEvents'
    //       condition: 'true'
    //       endpointNames: [ 'connectionEventsQueue' ]
    //       isEnabled: true
    //     }
    //   ]
    // }
    // // end - Message Routing Custom endpoint
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
    // Old IoT Hub defaults to Baltimore CyberTrust Root which will expire in 2025 
    // You must migrate to the DigiCert Global G2 root and these next two lines will do that.
    // To avoid service disruption it must be migrated by September 15th 2023.
    features: 'RootCertificateV2'
    rootCertificate: {
      enableRootCertificateV2: true
    }
    minTlsVersion: '1.2'
    disableLocalAuth: false
    allowedFqdnList: []
    enableDataResidency: false
  }
}

// // this isn't quite right yet...  can't seem to figure out the magic to automate this...
// // I get errors like "BadRequest" or "Invalid ARM id" or other non-helpful messages
// //https://learn.microsoft.com/en-us/azure/templates/microsoft.eventgrid/eventsubscriptions?pivots=deployment-language-bicep
// // When you create it manually and look at the ARM in the Advanced Editor in the portal looks like this..., 
// // but I can't get the Bicep to replicate this and make it work...! :(
// // {
// // 	"name": "testSubscription",
// // 	"properties": {
// // 		"topic": "/subscriptions/xxxx/resourceGroups/rg_iot_demo/providers/Microsoft.Devices/IotHubs/lll-iot-hub-demo",
// // 		"destination": {
// // 			"endpointType": "ServiceBusQueue",
// // 			"properties": {
// // 				"resourceId": "/subscriptions/xxxx/resourceGroups/rg_iot_demo/providers/Microsoft.ServiceBus/namespaces/lll-iot-svcbus-demo/queues/connectionevents"
// // 			}
// // 		},
// // 		"filter": {
// // 			"includedEventTypes": [
// // 				"Microsoft.Devices.DeviceConnected",
// // 				"Microsoft.Devices.DeviceDisconnected"
// // 			],
// // 			"advancedFilters": [],
// // 			"enableAdvancedFilteringOnArrays": true
// // 		},
// // 		"labels": [],
// // 		"eventDeliverySchema": "EventGridSchema"
// // 	}
// // }
// resource connectionEventSubscription 'Microsoft.EventGrid/eventSubscriptions@2022-06-15' = {
//   name: 'connectionEventGridSubscription'
//   scope: iotHubResource
//   properties: {
//     deliveryWithResourceIdentity: {
//       // identity: {
//       //   type: 'SystemAssigned'
//       // } 
//       destination: {
//         endpointType: 'ServiceBusQueue'
//         properties: {
//           resourceId: serviceBusResource.id
//           //resourceId: serviceBusQueueResource.id
//           // deliveryAttributeMappings: [
//           //   {
//           //     name: 'queueName'
//           //     type: 'Dynamic'
//           //     properties: {
//           //       sourceField: queueName
//           //     }
//           //   }
//           // ]
//           // }
//           //queueName: queueName
//         }
//       }
//     }
//     filter: {
//       includedEventTypes: [ 'Microsoft.Devices.DeviceDeleted','Microsoft.Devices.DeviceConnected' ]
//       enableAdvancedFilteringOnArrays: true
//     } 
//     eventDeliverySchema: 'EventGridSchema'
//     retryPolicy: {
//       eventTimeToLiveInMinutes: 10
//       maxDeliveryAttempts: 5
//     }
//   }
// }

// --------------------------------------------------------------------------------
resource iotHubAuditLogging 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${iotHubResource.name}-auditlogs'
  scope: iotHubResource
  properties: {
    workspaceId: workspaceId
    logs: [
      {
        category: 'C2DCommands'
        enabled: true
        retentionPolicy: {
          days: 30
          enabled: true 
        }
      }
      {
        category: 'DirectMethods'
        enabled: true
        retentionPolicy: {
          days: 30
          enabled: true 
        }
      }
    ]
  }
}

resource iotHubMetricLogging 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${iotHubResource.name}-metrics'
  scope: iotHubResource
  properties: {
    workspaceId: workspaceId
    metrics: [
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

// --------------------------------------------------------------------------------
output name string = iotHubResource.name
output id string = iotHubResource.id
output apiVersion string = iotHubResource.apiVersion
output storageAccountName string = iotStorageAccountName
output storageContainerName string = iotStorageContainerName
// output serviceBusName string = serviceBusName
// output serviceBusAccessKeyName string = serviceBusAccessKeyName
