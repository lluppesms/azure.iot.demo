namespace IoT.Simulator;

using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;

#region Delegate Definitions
/// <summary>
/// Delegate for error reporting
/// </summary>
public delegate void ErrorReportEvent(System.Exception e);

/// <summary>
/// Delegate for Informational message
/// </summary>
public delegate void MessageReceivedEvent(string msg);

/// <summary>
/// Delegate for Command message
/// </summary>
public delegate Task CommandReceivedEvent(string commandName);

/// <summary>
/// Delegate for Command with properties message
/// </summary>
public delegate Task CommandReceivedWithPropertiesEvent(string commandName, TwinCollection desiredProperties);

/// <summary>
/// Delegate for Twin Changed message
/// </summary>
public delegate Task TwinChangedEvent(TwinCollection desiredProperties);
#endregion

public class IoTDeviceBase
{
    #region Variables
    public string DeviceId { get; set; }
    public DeviceClient DeviceClient { get; set; }
    public TransportType TransportProtocol { get; set; }

    protected string PfxFileName { get; set; }
    protected string PfxFilePassword { get; set; }
    protected string DeviceConnectionString { get; set; }
    protected string IoTHubUri { get; set; }
    protected string DpsScopeId { get; set; }
    protected string SymmetricKey { get; set; }

    protected bool ValidateParameters(ProcessingParameters parms)
    {
        if (string.IsNullOrEmpty(parms.DeviceId))
        {
            Utilities.DisplayMessage("Device Name must be supplied - unable to continue!", ConsoleColor.Red);
            return false;
        }
        if (!Utilities.ValidateIoTDeviceId(parms.DeviceId))
        {
            return false;
        }
        if (string.IsNullOrEmpty(parms.DeviceConnectionString) && string.IsNullOrEmpty(parms.SymmetricKey) && string.IsNullOrEmpty(parms.PfxFileName))
        {
            Utilities.DisplayMessage("No key or certificate in config - unable to continue!", ConsoleColor.Red);
            return false;
        }
        PopulateLocalVariables(parms);
        return true;
    }
    protected void PopulateLocalVariables(ProcessingParameters parms)
    {
        DeviceId = parms.DeviceId;
        PfxFileName = parms.PfxFileName;
        PfxFilePassword = parms.PfxFilePassword;
        IoTHubUri = parms.IoTHubUri;
        DeviceConnectionString = parms.DeviceConnectionString;
        TransportProtocol = parms.TransportProtocol;
        DpsScopeId = parms.DpsScopeId;
        SymmetricKey = parms.SymmetricKey;
    }
    #endregion

    #region Event Definitions
    /// <summary>
    /// Event fired when an error occurs
    /// </summary>
    public event ErrorReportEvent OnError;

    /// <summary>
    /// Raise the OnError event
    /// </summary>
    public void RaiseOnError(Exception e)
    {
        OnError?.Invoke(e);
    }

    /// <summary>
    /// Event fired when data is sent
    /// </summary>
    public event MessageReceivedEvent OnMessageReceived;

    /// <summary>
    /// Raise the OnMessageSent event
    /// </summary>
    public void RaiseOnMessageReceived(string msg)
    {
        OnMessageReceived?.Invoke(msg);
    }

    /// <summary>
    /// Event fired when command is sent
    /// </summary>
    public event CommandReceivedEvent OnCommandReceived;

    /// <summary>
    /// Raise the OnCommandSent event
    /// </summary>
    public void RaiseCommand(string commandName)
    {
        OnCommandReceived?.Invoke(commandName);
    }

    /// <summary>
    /// Event fired when command is sent with device properties
    /// </summary>
    public event CommandReceivedWithPropertiesEvent OnCommandReceivedWithProperties;

    /// <summary>
    /// Raise the OnCommandSent event with desired properties
    /// </summary>
    public void RaiseCommandWithProperties(string commandName, TwinCollection desiredProperties)
    {
        OnCommandReceivedWithProperties?.Invoke(commandName, desiredProperties);
    }

    /// <summary>
    /// Event fired when twin has changed
    /// </summary>
    public event TwinChangedEvent OnTwinChanged;

    /// <summary>
    /// Raise the OnTwinChanged event
    /// </summary>
    public void RaiseOnTwinChanged(TwinCollection desiredProperties)
    {
        OnTwinChanged?.Invoke(desiredProperties);
    }
    #endregion

    /// <summary>
    /// Crreate device client
    /// </summary>
    public async Task<bool> CreateCloudConnection(ProcessingParameters parms)
    {
        if (!ValidateParameters(parms)) return false;
        try
        {
            DeviceClient = null;
            if (!string.IsNullOrEmpty(DeviceConnectionString))
            {
                Utilities.DisplayMessage($"    Registering device via IoT Hub Device Connection String:\n    {Utilities.GetSanitizedConnectionString(DeviceConnectionString)}", ConsoleColor.Yellow);
                DeviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportProtocol);
            }

            if (DeviceClient == null && !string.IsNullOrEmpty(SymmetricKey) && !string.IsNullOrEmpty(IoTHubUri))
            {
                if (string.IsNullOrEmpty(IoTHubUri))
                {
                    Utilities.DisplayMessage("Symmetric Key supplied but no IoT Hub Name supplied in config - unable to continue!", ConsoleColor.Red);
                    return false;
                }
                Utilities.DisplayMessage($"    Registering device via IoT Hub Device Symmetric Key...", ConsoleColor.Yellow);
                var ioTConnectionString = $"HostName={IoTHubUri};SharedAccessKey={SymmetricKey}";
                DeviceClient = DeviceClient.CreateFromConnectionString(ioTConnectionString, DeviceId, TransportProtocol);
            }

            if (DeviceClient == null && !string.IsNullOrEmpty(PfxFileName) && !string.IsNullOrEmpty(DpsScopeId))
            {
                var fullPfxFileName = Utilities.GetApplicationDirectory() + PfxFileName;
                if (!File.Exists(fullPfxFileName))
                {
                    Utilities.DisplayErrorMessage($"    Could not find PFX file {PfxFileName} specified in config!");
                    return false;
                }
                var deviceCert = new X509Certificate2(PfxFileName, PfxFilePassword);
                if (deviceCert == null)
                {
                    return false;
                }
                DeviceClient = await CreateDPSDeviceClient(deviceCert, parms);
            }

            if (DeviceClient == null)
            {
                Utilities.DisplayMessage($"    Registration method failed! Supply a valid Symmetric Key+IoTHub, PFX+ScopeId, or connection string!", ConsoleColor.Red);
                return false;
            }

            await OpenDeviceClient();
            ShowAvailableCommands();
            await SetupCallBackMethods();
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            Utilities.DisplayErrorMessage($"  Exception: {errorMsg}");
            return false;
        }

    }

    /// <summary>
    /// Create a provisioning client for the DPS
    /// </summary>
    private async Task<DeviceClient> CreateDPSDeviceClient(X509Certificate2 deviceCert, ProcessingParameters parms)
    {
        await Task.FromResult(true);
        //var chainCerts = new X509Certificate2Collection();
        //chainCerts.Add(new X509Certificate2(parameters.RootCertPath));
        //chainCerts.Add(new X509Certificate2(parameters.Intermediate1CertPath));
        //chainCerts.Add(new X509Certificate2(parameters.Intermediate2CertPath));
        //var deviceCert = new X509Certificate2(PfxFileName, PfxFilePassword);
        //var auth = new DeviceAuthenticationWithX509Certificate(DeviceId, deviceCert, chainCerts);
        if (!string.IsNullOrWhiteSpace(DpsScopeId))
        {
            Utilities.DisplayMessage($"    Attempting to register device with DPS {Constants.GlobalDPSEndpoint} Scope {DpsScopeId}...", ConsoleColor.Yellow);
            var securityProvider = new SecurityProviderX509Certificate(deviceCert);
            var provClient = GetDPSProvisioningClient(deviceCert, securityProvider, parms.TransportProtocol, Constants.GlobalDPSEndpoint, DpsScopeId);
            if (provClient == null)
            {
                Utilities.DisplayErrorMessage("Unable to connect to DPS using specified protocols...!");
                return null;
            }

            var result = provClient.RegisterAsync().GetAwaiter().GetResult();
            if (result.Status == ProvisioningRegistrationStatusType.Assigned)
            {
                Utilities.DisplayMessage("      Device Registration successful!", ConsoleColor.Green);
                Utilities.DisplayMessage($"        Status: {result.Status};  Hub={result.AssignedHub}; DeviceID={result.DeviceId}", ConsoleColor.Green);
                if (DeviceId != result.DeviceId)
                {
                    if (parms.DeviceIdSupplied)
                    {
                        Utilities.DisplayMessageInABox($"NOTE: Device Id provided in config ({DeviceId}) does not match certificate\n* Device Id ({result.DeviceId}).  Changing to use Certificate value!", ConsoleColor.Red);
                    }
                    else
                    {
                        Utilities.DisplayMessageInABox($"NOTE: Changing Simulator to use Device Id configured in Certificate ({result.DeviceId})", ConsoleColor.Red);
                    }
                    DeviceId = result.DeviceId;
                }
                IoTHubUri = result.AssignedHub;

                Utilities.DisplayMessage("    Authenticating Device using X509 authentication...", ConsoleColor.Yellow);
                var certAuth = new DeviceAuthenticationWithX509Certificate(DeviceId, (securityProvider as SecurityProviderX509).GetAuthenticationCertificate());
                Utilities.DisplayMessage("      Authenticated... creating DeviceClient", ConsoleColor.Green);
                var deviceClient = DeviceClient.Create(IoTHubUri, certAuth, parms.TransportProtocol);
                return deviceClient;
            }
            else
            {
                Utilities.DisplayErrorMessage($"      DPS Registration Failed!  {result.Status}: Code: {result.ErrorCode}; Message: {result.ErrorMessage}");
                return null;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get DPS Provisioning Client used to connect to a DPS endpoint
    /// </summary>
    private static ProvisioningDeviceClient GetDPSProvisioningClient(X509Certificate2 cert, SecurityProviderX509Certificate securityProvider, TransportType parmTransportType, string dpsEndpoint, string dpsScope)
    {
        ProvisioningDeviceClient provClient;
        if (parmTransportType == TransportType.Amqp_WebSocket_Only || parmTransportType == TransportType.Amqp_Tcp_Only)
        {
            using var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpWithWebSocketFallback);
            provClient = ProvisioningDeviceClient.Create(dpsEndpoint, dpsScope, securityProvider, transport);
        }
        else if (parmTransportType == TransportType.Mqtt || parmTransportType == TransportType.Mqtt_WebSocket_Only)
        {
            using var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpWithWebSocketFallback);
            provClient = ProvisioningDeviceClient.Create(dpsEndpoint, dpsScope, securityProvider, transport);
        }
        else
        {
            using var transport = new ProvisioningTransportHandlerHttp();
            provClient = ProvisioningDeviceClient.Create(dpsEndpoint, dpsScope, securityProvider, transport);
        }
        return provClient;
    }

    /// <summary>
    /// Open the Device Client Connection
    /// </summary>
    protected async Task<bool> OpenDeviceClient()
    {
        await Task.FromResult(true);
        try
        {
            Utilities.DisplayMessage("    Created DeviceClient... opening connection...", ConsoleColor.Yellow);
            DeviceClient.OpenAsync().Wait();
            //await deviceClient.OpenAsync();
            Utilities.DisplayMessage("    DeviceClient created and opened", ConsoleColor.Yellow);
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            Utilities.DisplayErrorMessage($"  Exception Opening DeviceClient: {errorMsg}");
            return false;
        }
    }

    /// <summary>
    /// Read Device Twin
    /// </summary>
    public async Task<(TwinCollection desired, TwinCollection reported)> ReadDeviceTwin(bool displayValues = true)
    {
        await Task.FromResult(false);
        try
        {
            Utilities.DisplayMessage("      Retrieving device twin data...", ConsoleColor.Yellow);
            var twin = DeviceClient.GetTwinAsync().GetAwaiter().GetResult();
            //await method call is failing here... the program just exits and this fails to work.  use Wait() instead and it works... ???
            //var twin = await DeviceClient.GetTwinAsync().ConfigureAwait(false);
            if (displayValues)
            {
                Utilities.DisplayMessage("\tDevice Twin values:", ConsoleColor.Yellow);
                Utilities.DisplayMessage($"\t{twin.ToJson()}", ConsoleColor.White);
            }
            return (twin.Properties.Desired, twin.Properties.Reported);
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            Utilities.DisplayErrorMessage($"      Error Reading Device Twin: {errorMsg}");
            return (null, null);
        }
    }

    /// <summary>
    /// Convert Device Twin Property to String
    /// </summary>
    public string ConvertDeviceTwinPropertyToString(TwinCollection twinProperties, string propertyName, string defaultValue = "")
    {
        try
        {
            if (twinProperties.Contains(propertyName))
            {
                var prop = twinProperties[propertyName];
                if (prop != null)
                {
                    return (string)prop;
                }
            }
            Utilities.DisplayMessage($"          Device Twin did not specify property {propertyName}! Using default value...", ConsoleColor.Red);
            return defaultValue;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            Utilities.DisplayErrorMessage($"      Error Reading Device Twin Property {propertyName}! Using default value: {errorMsg}");
            return defaultValue;
        }
    }

    /// <summary>
    /// Call Write to Device Twin Command
    /// </summary>
    public async Task<Twin> WriteDeviceTwinProperty(string propertyName, object propertyValue, bool displayValues = true)
    {
        await Task.FromResult(false);
        try
        {
            Utilities.DisplayMessage($"      Setting Device Twin Reported Property '{propertyName}'...", ConsoleColor.Yellow);
            var reportedProperties = new TwinCollection();
            reportedProperties[propertyName] = propertyValue;
            DeviceClient.UpdateReportedPropertiesAsync(reportedProperties).Wait();
            //await method call is failing here... the program just exits and this fails to work.  use Wait() instead and it works... ???
            //  await device.deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

            var twin = DeviceClient.GetTwinAsync().GetAwaiter().GetResult();
            if (displayValues)
            {
                Utilities.DisplayMessage("\tDevice Twin values:", ConsoleColor.Yellow);
                Utilities.DisplayMessage($"\t{twin.ToJson()}", ConsoleColor.White);
            }
            return twin;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            Utilities.DisplayErrorMessage($"      Error Writing to Device Twin: {errorMsg}");
            return null;
        }
    }

    /// <summary>
    /// Send Message to IoT Hub
    /// </summary>
    public async Task<bool> SendMessage(string messageContent)
    {
        await Task.FromResult(true);
        try
        {
            var msg = new Message(Encoding.ASCII.GetBytes(messageContent));
            Utilities.DisplayMessage("      Sending Message to IoT Hub...", ConsoleColor.Yellow);
            msg.Properties.Add("messagetype", "normal");
            DeviceClient.SendEventAsync(msg).Wait();
            //await method call is failing here... the program just exits and this fails to work.  use Wait() instead and it works... ???
            //  await device.deviceClient.SendEventAsync(msg).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            Utilities.DisplayErrorMessage($"    Error Sending Message: {errorMsg}");
            return false;
        }
    }

    /// <summary>
    /// Send Batch of Messages to IoT Hub
    /// </summary>
    public async Task<bool> SendMessageBatch(string[] messageContentArray)
    {
        await Task.FromResult(true);
        try
        {
            Utilities.DisplayMessage("      Sending Batch of Messages to IoT Hub...", ConsoleColor.Yellow);
            var msgList = new List<Message>();
            foreach (var item in messageContentArray)
            {
                var msg = new Message(Encoding.ASCII.GetBytes(item));
                msg.Properties.Add("batch", "true");
                msgList.Add(msg);
            }

            DeviceClient.SendEventBatchAsync(msgList).Wait();
            //await method call is failing here... the program just exits and this fails to work.  use Wait() instead and it works... ???
            //  await device.deviceClient.SendEventBatchAsync(msgList).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            Utilities.DisplayErrorMessage($"    Error Sending Batch Messages: {errorMsg}");
            return false;
        }
    }

    /// <summary>
    /// Upload a file to the IoT Hub
    /// </summary>
    public async Task<bool> UploadFile(string filePath)
    {
        var fileUploadTime = Stopwatch.StartNew();
        var isSuccess = false;
        var uploadMsg = string.Empty;

        await Task.FromResult(true);
        try
        {
            using var fileStreamSource = new FileStream(filePath, FileMode.Open);
            var fileName = Path.GetFileName(fileStreamSource.Name);
            Utilities.DisplayMessage($"      Uploading file {fileName}", ConsoleColor.Yellow);

            // Note: GetFileUploadSasUriAsync and CompleteFileUploadAsync will use HTTPS as protocol regardless of the DeviceClient protocol selection.
            Utilities.DisplayMessage("        Getting File Upload URI from IoT Hub...", ConsoleColor.Yellow);
            var sasUri = DeviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest { BlobName = fileName }).GetAwaiter().GetResult();
            //await method call is failing here... the program just exits and this fails to work.  use Wait() instead and it works... ???
            //var sasUri = await deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest { BlobName = fileName });
            var uploadUri = sasUri.GetBlobUri();
            Utilities.DisplayMessage($"        File will be sent to {uploadUri}", ConsoleColor.Yellow);

            try
            {
                var blockBlobClient = new BlockBlobClient(uploadUri);
                var response = blockBlobClient.UploadAsync(fileStreamSource, new BlobUploadOptions()).GetAwaiter().GetResult();
                //await method call is failing here... the program just exits and this fails to work.  use Wait() instead and it works... ???
                //var response = await blockBlobClient.UploadAsync(fileStreamSource, new BlobUploadOptions());
                isSuccess = response.GetRawResponse().Status == 201;
                uploadMsg = isSuccess ? "Success" : "Failure";
                Utilities.DisplayMessage(isSuccess ? "        File has been uploaded!" : "        File upload failed!", isSuccess ? ConsoleColor.White : ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                uploadMsg = ex.Message;
                Utilities.DisplayErrorMessage($"    Failed to upload file to Azure Storage: {uploadMsg}");
            }
            finally
            {
                // Note that this is done even when the file upload fails. IoT Hub has a fixed number of SAS URIs allowed active
                // at any given time. Once you are done with the file upload, you should free your SAS URI so that other
                // SAS URIs can be generated. If a SAS URI is not freed through this API, then it will free itself eventually
                // based on how long SAS URIs are configured to live on your IoT Hub.
                var fileUploadCompletionNotification = new FileUploadCompletionNotification
                {
                    CorrelationId = sasUri.CorrelationId,
                    IsSuccess = isSuccess,
                    StatusCode = isSuccess ? 200 : 500,
                    StatusDescription = uploadMsg
                };
                DeviceClient.CompleteFileUploadAsync(fileUploadCompletionNotification).GetAwaiter().GetResult();
                //await method call is failing here... the program just exits and this fails to work.  use Wait() instead and it works... ???
                //await deviceClient.CompleteFileUploadAsync(fileUploadCompletionNotification);
                Utilities.DisplayMessage("        Notified IoT Hub that the file upload finished and SAS URI can be freed", isSuccess ? ConsoleColor.Yellow : ConsoleColor.Red);
            }

            Utilities.DisplayMessage(isSuccess ? "      Successfully uploaded the file to Azure Storage" : "    Upload file to Azure Storage failed!", isSuccess ? ConsoleColor.White : ConsoleColor.Red);
            fileUploadTime.Stop();
            Utilities.DisplayMessage($"      Time to upload file: {fileUploadTime.Elapsed}.");
            return isSuccess;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            Utilities.DisplayErrorMessage($"    Error Preparing to upload file: {errorMsg}");
            return false;
        }
    }

    /// <summary>
    /// Status Change Handler
    /// </summary>
    protected static void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        var msg = $"\nIoT Device Connection status changed to {status}. Reason: {reason}.\n";
        if (!msg.Contains("Reason: Client_Close"))
        {
            Utilities.DisplayMessage(msg);
        }
    }

    /// <summary>
    /// Close the device and release the connections when exiting program
    /// </summary>
    public async Task CloseDeviceClientConnection()
    {
        await Task.FromResult(true);
        if (DeviceClient != null)
        {
            RemoveCallBackMethods().Wait();
            DeviceClient.CloseAsync().Wait();
            DeviceClient.Dispose();
        }
    }

    /// <summary>
    /// (Override this!) Setup any callback methods that may be called from the cloud
    /// </summary>
    protected virtual async Task<bool> SetupCallBackMethods()
    {
        Utilities.DisplayMessage($"\n *** No callback methods defined!", ConsoleColor.Green);

        DeviceClient.SetMethodDefaultHandlerAsync(OnDefaultMethodCalled, DeviceClient).GetAwaiter().GetResult();

        // set up monitor for device twin
        var updatedProperties = new TwinCollection();
        updatedProperties["LastTimeAppLaunched"] = DateTime.Now;
        DeviceClient.UpdateReportedPropertiesAsync(updatedProperties).GetAwaiter().GetResult();
        DeviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null).GetAwaiter().GetResult();

        //// Check for COMMAND messages (Warning: BLOCKING calls); Do not forget to return a 'Complete' or 'reject' or 'abandon'
        var thread = new Thread(() => OnDeviceMessageReceived(DeviceClient));
        thread.Start();

        return await Task.FromResult(true);
    }

    /// <summary>
    /// (Override this!) Tear down any callback methods that may be called from the cloud
    /// </summary>
    protected virtual async Task<bool> RemoveCallBackMethods()
    {
        Utilities.DisplayMessage($"\n *** No callback methods removed!", ConsoleColor.Green);
        return await Task.FromResult(true);
    }

    /// <summary>
    /// (Override this!) Handle any method calls that are not defined
    /// </summary>
    protected virtual Task<MethodResponse> OnDefaultMethodCalled(MethodRequest methodRequest, object userContext)
    {
        Utilities.DisplayMessage($"No default method handler defined... {methodRequest.Name} was called with body {methodRequest.DataAsJson}");
        return Task.FromResult(new MethodResponse(new byte[0], 200));
    }

    /// <summary>
    /// (Override this!) Fired when Device Twin Desired property changes in the cloud
    /// </summary>
    protected virtual async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
    {
        Utilities.DisplayMessage($"\n *** No Device Twin Property Change handler actions defined!", ConsoleColor.Green);
        await Task.FromResult(true);
        return;
    }

    /// <summary>
    /// (Override this!) Handle Device Messages
    /// </summary>
    protected virtual async void OnDeviceMessageReceived(DeviceClient client)
    {
        await Task.FromResult(true);
        Utilities.DisplayMessage("Enabling generic processor for Cloud to Device messages...");
        while (true)
        {
            // The following line is blocking until a timeout occurs (?)
            using var message = client.ReceiveAsync().GetAwaiter().GetResult();
            if (message == null)
            {
                Utilities.DisplayMessage("Command Handler Timeout... command is null!");
                continue;
            }

            var data = Encoding.UTF8.GetString(message.GetBytes());
            Utilities.DisplayMessage(string.Empty);
            Utilities.DisplayMessageInABox($"An unexpected message was received!", ConsoleColor.Red);
            Utilities.DisplayMessage($"Body: {data}", ConsoleColor.Blue);

            // mark the message as handled
            client.CompleteAsync(message).GetAwaiter().GetResult();
            //await client.RejectAsync(message); // drops the message as unhandled
            //await client.AbandonAsync(message); // puts message back on queue
        }
    }

    /// <summary>
    /// (Override this!) Show list of available commands
    /// </summary>
    protected virtual void ShowAvailableCommands()
    {
        Utilities.DisplayMessage("\n *** No command list defined!", ConsoleColor.Green);
    }
}
