# Code Signing in CI/CD Pipeline
This documents the steps needed to do code signing in your pipeline.

## Creating a PFX File
To create a self-signed cert, run this PowerShell command:
```
  > $New-SelfSignedCertificate -DNSName "www.luppes.com" -CertStoreLocation Cert:\CurrentUser\My -Type CodeSigningCert -Subject "CN=Luppes Demo, O=Luppes Demo, C=US"
```
Note the thumbprint in the output from this command and use that in the next set of commands.

To export the certificate in the local store to a Personal Information Exchange (PFX) file, use the Export-PfxCertificate cmdlet. When using Export-PfxCertificate, you must either create and use a password or use the "-ProtectTo" parameter to specify which users or groups can access the file without a password.

```
  > $password = ConvertTo-SecureString -String 1234 -Force -AsPlainText 
  > Export-PfxCertificate -cert "Cert:\CurrentUser\My\<myThumbprint>" -FilePath C:\Certs\LuppesDemoSigningCert.pfx -Password $password
```

## NOTE: Using the Certificate In Your App
  To use a certificate to sign your app package, the "Subject" in the certificate must match the "Publisher" section in your app's manifest.
  For example, the "Identity" section in your app's AppxManifest.xml file should look something like this:
  <Identity Name="Contoso.AssetTracker" Version="1.0.0.0" Publisher="CN=Contoso Software, O=Contoso Corporation, C=US"/>

## Viewing A Certificate
To view a cert in Certificate Manager, import the cert, then get the thumbprint
  > Set-Location Cert:\CurrentUser\My
  > Get-ChildItem | Format-Table Subject, FriendlyName, Thumbprint

## Build Process - Variable Group Contents
The build job uses a command to sign your executable with variables, and the best way to control these variables is to put them into a Variable Group which can be secured and read only by the pipeline. Create a variable group named "CodeSigning", and populate these variables:

- KeyVaultUrl: https://<yourVaultName>.vault.azure.net/
- CertName: <yourCertName>
- SigningAppRegAppId: <AppReg ClientId guid>
- SigningAppRegClientSecret: <secret value>
- ActiveDirectoryTenantId: <AD TenantId guid>
- TimestampUrl: http://timestamp.digicert.com
- SubscriptionName: <your subscription / Service Connection>
- StorageAccountName: <name of storage account to store output>

## Build Process - The Actual Build Steps
In your pipeline, add a VariableGroup "CodeSigning" to make this work (go to edit -> Triggers to get to a detailed editor, then add via Variables tab to add the group...)

These are the steps to pull in the sign tool and sign the code:
```
    variables:
    - name: exeName
        value: IoT.Simulator
    - group: CodeSigning
    ...
      - task: DotNetCoreCLI@2
        displayName: 'Install AzureSignTool'
        inputs:
          command: custom
          custom: tool
          arguments: 'install --global azuresigntool'
        continueOnError: true
    ...
      - task: PowerShell@2
        displayName: 'Sign win-x64 application'
        inputs:
          targetType: 'inline'
          script: |
            cd $(build.artifactstagingdirectory)/packages
            azuresigntool sign s/$(exeName).exe -kvu $(KeyVaultUrl) -kvi $(SigningAppRegAppId) -kvs $(SigningAppRegClientSecret) -kvt $(ActiveDirectoryTenantId) -kvc $(CertName) -tr $(TimestampUrl) -v
        continueOnError: true
    ...
```

## Reference Documents
https://docs.microsoft.com/en-us/windows/msix/package/create-certificate-package-signing

https://codesigningstore.com/how-to-generate-self-signed-code-signing-certificate

https://docs.microsoft.com/en-us/windows/msix/desktop/cicd-keyvault
 