// --------------------------------------------------------------------------------
// This BICEP file will create a Stream Analytics Job 
// --------------------------------------------------------------------------------

param orgPrefix string = 'org'
param appPrefix string = 'app'
@allowed(['dev','qa','stg','prod'])
param environmentCode string = 'dev'
param appSuffix string = '1'
param location string = resourceGroup().location
param runDateTime string = utcNow()
param templateFileName string = '~streaming.bicep'
param sku string = 'F1'

param iotHubName string
param svcBusName string
param svcBusQueueName string

// --------------------------------------------------------------------------------
var saJobName = '${orgPrefix}-${appPrefix}-stream-${environmentCode}${appSuffix}'

// --------------------------------------------------------------------------------
resource iotHubResource 'Microsoft.Devices/IotHubs@2021-07-02' existing = { name: iotHubName }
var iotHubAccessKey = '${listKeys(iotHubResource.id, '2021-07-02').value[0].primaryKey}'

resource svcBusResource 'Microsoft.ServiceBus/namespaces@2021-11-01' existing = { name: svcBusName }
var svcBusAccessKeyEndpoint = '${svcBusResource.id}/AuthorizationRules/RootManageSharedAccessKey'
var svcBusAccessKey = '${listKeys(svcBusAccessKeyEndpoint, svcBusResource.apiVersion).primaryKey}'

// --------------------------------------------------------------------------------
resource saJobResource 'Microsoft.StreamAnalytics/streamingjobs@2021-10-01-preview' = {
  name: saJobName
  location: location
  tags: {
    LastDeployed: runDateTime
    TemplateFile: templateFileName
    Organization: orgPrefix
    Application: appPrefix
    Environment: environmentCode
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    sku: {
      name: 'Standard'
    }
    outputStartMode: 'CustomTime'
    outputStartTime: '2022-06-20T18:37:42Z'
    eventsOutOfOrderPolicy: 'Adjust'
    outputErrorPolicy: 'Stop'
    eventsOutOfOrderMaxDelayInSeconds: 0
    eventsLateArrivalMaxDelayInSeconds: 5
    dataLocale: 'en-US'
    compatibilityLevel: '1.2'
    contentStoragePolicy: 'SystemAccount'
    jobType: 'Cloud'
    inputs: [
      {
        name: 'iothub'
        properties: {
          type: 'Stream'
          datasource: {
            type: 'Microsoft.Devices/IotHubs'
            properties: {
              iotHubNamespace: iotHubName
              sharedAccessPolicyName: 'iothubowner'
              sharedAccessPolicyKey: iotHubAccessKey
              endpoint: 'messages/events'
              consumerGroupName: '$Default'
            }
          }
          compression: {
            type: 'None'
          }
          serialization: {
            type: 'Json'
            properties: {
              encoding: 'UTF8'
            }
          }
        }
      }
    ]
    outputs:  [
      {
        name: 'svcbus'
        properties: {
          datasource: {
            type: 'Microsoft.ServiceBus/Queue'
            properties: {
              queueName: svcBusQueueName
              propertyColumns: []
              systemPropertyColumns: {
              }
              serviceBusNamespace: svcBusName
              sharedAccessPolicyName: 'RootManageSharedAccessKey'
              sharedAccessPolicyKey: svcBusAccessKey
              authenticationMode: 'ConnectionString'
            }
          }
          serialization: {
            type: 'Json'
            properties: {
              encoding: 'UTF8'
              format: 'LineSeparated'
            }
          }
        }
      }
    ]
    transformation: {
      name: 'basequery'
      properties: {
        query: 'SELECT * INTO [svcbus] FROM [iothub]'
        streamingUnits: 1
      }
    }  
  }
}

output saJobName string = saJobName
