namespace IoT.Dashboard.Helpers;

public class DeviceRepository
{
    #region Variables
    /// <summary>
    /// Application Settings
    /// </summary>
    public AppSettings settings { get; set; }

    /// <summary>
    /// Azure Device Manager
    /// </summary>
    private RegistryManager registryManager = null;

    /// <summary>
    /// Azure IoT Service Manager
    /// </summary>
    private ServiceClient serviceClient = null;

    /// <summary>
    /// Azure Cosmos Helper
    /// </summary>
    private CosmosHelper cosmosHelper = null;

    /// <summary>
    /// Azure Blob Storage Helper
    /// </summary>
    private BlobStorageHelper storageHelper = null;
    #endregion

    #region Initialization
    /// <summary>
    /// Constructor
    /// </summary>
    public DeviceRepository()
    {
        // default the settings
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public DeviceRepository(AppSettings diSettings)
    {
        settings = diSettings;
        CreateDeviceClient();
    }
    #endregion

    /// <summary>
    /// Crreate device client
    /// </summary>
    public bool CreateDeviceClient()
    {
        try
        {
            if (registryManager != null) { return true; };
            if (settings != null && !string.IsNullOrEmpty(settings.IoTHubConnectionString))
            {
                MyLogger.LogInfo($"  Creating device manager via IoT Hub Device Connection String:\n    {Utilities.GetSanitizedConnectionString(settings.IoTHubConnectionString)}");
                registryManager = RegistryManager.CreateFromConnectionString(settings.IoTHubConnectionString);
                if (registryManager == null) { return false; };
                serviceClient = ServiceClient.CreateFromConnectionString(settings.IoTHubConnectionString);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            MyLogger.LogError($"      Error creating device client!: {errorMsg}");
            return false;
        }
    }

    /// <summary>
    /// Read list from Cosmos Data Table
    /// </summary>
    public async Task<(List<DeviceData>, string)> GetDataList(string deviceId, string selectedEventType, int startHoursToSubtract = 1, int endHoursToSubtract = 0, int maxRows = 50, string method = "SQL")
    {
        if (cosmosHelper == null) { cosmosHelper = new CosmosHelper(settings); }
        //if (method.StartsWith("SQL", StringComparison.InvariantCultureIgnoreCase))
        //{
        (var records, var msg) = await cosmosHelper.ReadManyRecords(deviceId, selectedEventType, startHoursToSubtract, endHoursToSubtract, maxRows);
        return (records, msg);
        //}
        //else
        //{
        //    (var records, var msg) = await cosmosHelper.ReadManyRecordsLinq(deviceId, selectedEventType, startHoursToSubtract, endHoursToSubtract, maxRows);
        //    return (records, msg);
        //}
    }

    /// <summary>
    /// Read one record from Cosmos Data Table
    /// </summary>
    public async Task<(string, string)> GetDataRecord(Guid messageId, string partitionKey)
    {
        if (cosmosHelper == null) { cosmosHelper = new CosmosHelper(settings); }
        (var record, var msg) = await cosmosHelper.ReadOneRecord(messageId, partitionKey);
        return (record, msg);
    }

    /// <summary>
    /// Read list from IOT Hub Storage Account
    /// </summary>
    public async Task<(List<DeviceFile>, string)> GetRecentFiles(string deviceId)
    {
        if (storageHelper == null) { storageHelper = new BlobStorageHelper(settings); }
        (var files, var msg) = await storageHelper.ListBlobs(deviceId);
        return (files, msg);
    }

    /// <summary>
    /// Return file content stream from IOT Hub Storage Account
    /// </summary>
    public async Task<MemoryStream> GetFileStream(string fileName)
    {
        if (storageHelper == null) { storageHelper = new BlobStorageHelper(settings); }
        var blobStream = await storageHelper.GetBlobStreamFromStorageContainer(fileName);
        return blobStream;
    }

    /// <summary>
    /// Return file content string from IOT Hub Storage Account
    /// </summary>
    public async Task<string> GetFileString(string fileName)
    {
        if (storageHelper == null) { storageHelper = new BlobStorageHelper(settings); }
        var blobString = await storageHelper.GetBlobStringFromStorageContainer(fileName);
        return blobString;
    }

    /// <summary>
    /// Send Command to Device
    /// </summary>
    public async Task<(bool, string)> CallDirectMethod(string deviceId, string methodName, string messageText = "")
    {
        var methodResult = string.Empty;
        try
        {
            var methodToCall = new CloudToDeviceMethod(methodName) { ResponseTimeout = TimeSpan.FromSeconds(30) };
            if (!string.IsNullOrEmpty(messageText))
            {
                methodToCall.SetPayloadJson(JsonConvert.SerializeObject(new { message = messageText }));
            }

            var result = await serviceClient.InvokeDeviceMethodAsync(deviceId, methodToCall);
            if (result != null)
            {
                var payload = result.GetPayloadAsJson();
                //var acknowledged = JObject.Parse(payload).GetValue("acknowledged").Value<bool>();
                //methodResult = acknowledged ? "Message was acknowledged!" : "Message was not acknowledged!";
                methodResult += " " + payload.ToStringNullable();
            }
            return (true, methodResult);
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            MyLogger.LogError($"      Error direct method {methodName} on client {deviceId}!: {errorMsg}");
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Send Command to Device
    /// </summary>
    public async Task<bool> SendCommand(string deviceId, string commandText)
    {
        try
        {
            var messagePayload = $"{{\"command\":\"{Utilities.IsOnlyNumbersOrLetters(commandText, 100)}\"}}";
            var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
            {
                MessageId = Guid.NewGuid().ToString(),
                ContentEncoding = Encoding.UTF8.ToString(),
                ContentType = "application/json"
            };
            await serviceClient.SendAsync(deviceId, eventMessage);
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            MyLogger.LogError($"      Error sending command {commandText} to client {deviceId}!: {errorMsg}");
            return false;
        }
    }

    /// <summary>
    /// Send Message to Device
    /// </summary>
    public async Task<bool> SendMesssage(string deviceId, string messageText)
    {
        var messagePayload = string.Empty;
        try
        {
            messagePayload = Utilities.IsOnlyNumbersOrLettersOrSpace(messageText, 50);
            var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
            {
                MessageId = Guid.NewGuid().ToString(),
                ContentEncoding = Encoding.UTF8.ToString(),
                ContentType = "application/text"
            };
            await serviceClient.SendAsync(deviceId, eventMessage);
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            MyLogger.LogError($"      Error sending message {messagePayload} to client {deviceId}!: {errorMsg}");
            return false;
        }
    }
    /// <summary>
    /// Return list of deviceIds from IoT Hub
    /// </summary>
    public async Task<List<string>> GetListOfDevices(string filterName = "", string filterValue = "", int maxDevices = 50)
    {
        var deviceIdList = new List<string>();
        if (registryManager == null) { return deviceIdList; };
        
        var queryText = "SELECT * FROM devices";
        //var queryText = "SELECT * FROM devices WHERE tags.location.plant = 'MyPlant'";
        if (!string.IsNullOrEmpty(filterName) && !string.IsNullOrEmpty(filterValue))
        {
            queryText = string.Format("SELECT * FROM devices WHERE {0} = '{1}'", Utilities.IsOnlyNumbersOrLetters(filterName, 50), Utilities.IsOnlyNumbersOrLetters(filterValue, 100));
        }
        var query = registryManager.CreateQuery(queryText, maxDevices);
        var twins = await query.GetNextAsTwinAsync();
        foreach (var twin in twins)
        {
            deviceIdList.Add(twin.DeviceId);
        }
        return deviceIdList;
    }

    /// <summary>
    /// Get a Device
    /// </summary>
    public async Task<Device> GetDevice(string deviceId)
    {
        if (!Utilities.ValidateIoTDeviceId(deviceId))
        {
            return null;
        }
        if (registryManager == null) { return null; };
        var device = await registryManager.GetDeviceAsync(deviceId);
        return device;
    }

    /// <summary>
    /// Read Device Twin
    /// </summary>
    public async Task<(TwinCollection desired, TwinCollection reported)> ReadDeviceTwinProperties(string deviceId)
    {
        if (!Utilities.ValidateIoTDeviceId(deviceId))
        {
            return (null, null);
        }
        try
        {
            MyLogger.LogInfo("      Retrieving device twin data...");
            var twin = await registryManager.GetTwinAsync(deviceId);
            return (twin.Properties.Desired, twin.Properties.Reported);
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            MyLogger.LogError($"      Error Reading Device Twin: {errorMsg}");
            return (null, null);
        }
    }

    /// <summary>
    /// Read Device Twin
    /// </summary>
    public async Task<Twin> ReadDeviceTwin(string deviceId)
    {
        if (!Utilities.ValidateIoTDeviceId(deviceId))
        {
            return null;
        }
        try
        {
            MyLogger.LogInfo("      Retrieving device twin data...");
            var twin = await registryManager.GetTwinAsync(deviceId);
            return twin;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            MyLogger.LogError($"      Error Reading Device Twin: {errorMsg}");
            return null;
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
            MyLogger.LogError($"          Device Twin did not specify property {propertyName}! Using default value...");
            return defaultValue;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            MyLogger.LogError($"      Error Reading Device Twin Property {propertyName}! Using default value: {errorMsg}");
            return defaultValue;
        }
    }

    /// <summary>
    /// Call Write to Device Twin Command
    /// </summary>
    public async Task<Twin> WriteDeviceTwinProperty(string deviceId, string propertyName, object propertyValue, bool createIfNotFound = false)
    {
        if (!Utilities.ValidateIoTDeviceId(deviceId))
        {
            return null;
        }
        if (string.IsNullOrEmpty(propertyName) || propertyValue.ToStringNullable() == string.Empty)
        {
            return null;
        }
        try
        {
            MyLogger.LogInfo($"      Setting Device Twin Reported Property '{propertyName}'...");

            var twin = await ReadDeviceTwin(deviceId);
            if (twin == null)
            {
                if (createIfNotFound)
                {
                    var newDevice = new Device(deviceId);
                    await registryManager.AddDeviceAsync(newDevice);
                    twin = await ReadDeviceTwin(deviceId);
                }
            }

            twin.Properties.Desired[propertyName] = propertyValue == null || propertyValue.ToStringNullable().ToLower() == "null" ? null : propertyValue;
            var newTwin = await registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);
            return newTwin;
        }
        catch (Exception ex)
        {
            var errorMsg = Utilities.GetExceptionMessage(ex);
            MyLogger.LogError($"      Error Writing to Device Twin: {errorMsg}");
            return null;
        }
    }
}
