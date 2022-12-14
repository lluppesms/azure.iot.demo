# Set up a Azure DevOps Variable Groups

The Azure DevOps pipelines in this project need these variable groups.

To create them, customize and run the following commands in an Azure Cloud Shell.

These commands *may* be needed when you begin:

``` bash
  az login
  az devops configure --defaults organization=https://dev.azure.com/<yourAzDOOrg>/ 
  az devops configure --defaults project='<yourAzDOProject>' 
```

These commands actually create the variable groups:

``` bash
  az login
  az devops configure --defaults organization=https://dev.azure.com/<yourAzDOOrg>/ 
  az devops configure --defaults project='<yourAzDOProject>' 

  az pipelines variable-group create 
    --organization=https://dev.azure.com/<yourAzDOOrg>/ 
    --project='<yourAzDOProject>' 
    --name IoTDemo
    --variables 
        orgPrefix='<yourInitials>' 
        appPrefix='iotdemo' 
        appSuffix=''
        serviceConnectionName='<yourServiceConnection>' 
        subscriptionId='<yourSubscriptionId>' 
        subscriptionName='<yourSubscriptionName>' 
        location='eastus' 
        storageSku='Standard_LRS' 
        functionName='process'
        functionAppSku='Y1' 
        functionAppSkuFamily='Y' 
        functionAppSkuTier='Dynamic' 
        webSiteSku='B1'
        keyVaultOwnerUserId1='<userGuid>'
        keyVaultOwnerUserId2='<userGuid>'
```
