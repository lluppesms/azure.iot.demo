// --------------------------------------------------------------------------------
// This BICEP file will create a KeyVault
// FYI: To purge a KV with soft delete enabled: > az keyvault purge --name kvName
// --------------------------------------------------------------------------------
param orgPrefix string = 'org'
param appPrefix string = 'app'
@allowed(['dev','demo','qa','stg','prod'])
param environmentCode string = 'dev'
param appSuffix string = '1'
param location string = resourceGroup().location
param runDateTime string = utcNow()
param templateFileName string = '~keyVault.bicep'

param adminUserObjectIds array = []
param applicationUserObjectIds array = []

// --------------------------------------------------------------------------------
var keyVaultName = '${orgPrefix}${appPrefix}vault${environmentCode}${appSuffix}'

var adminAccessPolicies = [for adminUser in adminUserObjectIds: {
  objectId: adminUser
  tenantId: subscription().tenantId
  permissions: {
    certificates: [ 'all' ]
    secrets: [ 'all' ]
    keys: [ 'all' ]
  }
}]
var applicationUserPolicies = [for appUser in applicationUserObjectIds: {
  objectId: appUser
  tenantId: subscription().tenantId
  permissions: {
    secrets: [ 'get' ]
  }
}]
var accessPolicies = union(adminAccessPolicies, applicationUserPolicies)

// --------------------------------------------------------------------------------
resource keyvaultResource 'Microsoft.KeyVault/vaults@2021-11-01-preview' = {
  name: keyVaultName
  location: location
  tags: {
    LastDeployed: runDateTime
    TemplateFile: templateFileName
    Organization: orgPrefix
    Application: appPrefix
    Environment: environmentCode
  }
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId

    // Use Access Policies model
    enableRbacAuthorization: false      
    // add function app and web app identities in the access policies so they can read the secrets
    accessPolicies: accessPolicies

    enabledForDeployment: false          // VMs can retrieve certificates
    enabledForTemplateDeployment: false  // ARM can retrieve values
    enablePurgeProtection: true         // Not allowing to purge key vault or its objects after deletion
    enableSoftDelete: false
    createMode: 'default'               // Creating or updating the key vault (not recovering)
  }
}

output keyVaultName string = keyvaultResource.name
output keyVaultId string = keyvaultResource.id
