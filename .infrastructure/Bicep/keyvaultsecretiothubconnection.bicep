// --------------------------------------------------------------------------------
// This BICEP file will create KeyVault secret for an IoT Hub Connection
// --------------------------------------------------------------------------------
param keyVaultName string
param keyName string
param iotHubName string

// --------------------------------------------------------------------------------
resource iotHubResource 'Microsoft.Devices/IotHubs@2021-07-02' existing = { name: iotHubName }
var iotHubConnectionString = 'HostName=${iotHubResource.name}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=${listKeys(iotHubResource.id, iotHubResource.apiVersion).value[0].primaryKey}'

// --------------------------------------------------------------------------------
resource keyvaultResource 'Microsoft.KeyVault/vaults@2021-11-01-preview' existing = { 
  name: keyVaultName
  resource iotHubSecret 'secrets' = {
    name: keyName
    properties: {
      value: iotHubConnectionString
    }
  }
}
