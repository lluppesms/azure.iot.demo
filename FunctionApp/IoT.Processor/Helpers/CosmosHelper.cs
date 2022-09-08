namespace IoT.Processor.Helpers;

/// <summary>
/// Encapsulates all of the Cosmos related plumbing
/// </summary>
public class CosmosHelper
{
    #region Variables
    /// <summary>
    /// Configuration
    /// </summary>
    private MyConfiguration config;

    /// <summary>
    /// Secrets
    /// </summary>
    private MySecrets secrets;

    /// <summary>
    /// Reference to a Cosmos Data Container
    /// </summary>
    private Container myDataContainer = null;

    /// <summary>
    /// Reference to a Cosmos Device Container
    /// </summary>
    private Container myDeviceContainer = null;
    #endregion

    #region Initialization
    /// <summary>
    /// Constructor
    /// </summary>
    public CosmosHelper(MyConfiguration diConfig, MySecrets diSecrets)
    {
        config = diConfig;
        secrets = diSecrets;
    }
    #endregion

    /// <summary>
    /// Send to Cosmos Data Container
    /// </summary>
    public async Task<(bool, string)> SendDataToCosmos(string deviceId, string partitionKeyDate, Guid messageId, dynamic data, string dataSource, StringBuilder sb)
    {
        var success = false;
        try
        {
            if (data == null || string.IsNullOrEmpty(deviceId))
            {
                var badDataMessage = data == null  ? "No Cosmos message to write!" : "Invalid Cosmos message - no DeviceId found!";
                MyLogger.LogError(badDataMessage, dataSource, deviceId);
                return (false, badDataMessage);
            }

            MyLogger.LogInfo($"CosmosDB: Writing message {messageId} for device {deviceId} to {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName}", dataSource, deviceId);
            if (await GetCosmosDataContainerReference(deviceId, dataSource, sb) && myDataContainer != null)
            {
                // Warning: partition key with only deviceId will eventually fill up partition and cause everything to crash, so append current date to the key!
                data.PartitionKey = $"{deviceId}-{partitionKeyDate}";
                var response = await myDataContainer.CreateItemAsync(data, new PartitionKey(data.PartitionKey));
                success = response.StatusCode == System.Net.HttpStatusCode.Created;
                return (success, success ? "Cosmos Write Success" : "Cosmos Write Failure");
            }
            var noContainerMsg = "Error connecting to Cosmos Data Container!";
            MyLogger.LogInfo(noContainerMsg, dataSource, deviceId);
            return (false, noContainerMsg);
        }
        catch (Exception ex)
        {
            var baseMsg = $"Error writing to Cosmos Data Container!";
            var errorMsg = $"{baseMsg} {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName}. Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, deviceId);
            return (false, baseMsg);
        }
    }

    /// <summary>
    /// Send Device Information to Cosmos
    /// </summary>
    public async Task<(bool, string)> SendDeviceToCosmos(string deviceId, string partitionKeyDate, Guid messageId, Register data, string dataSource, StringBuilder sb)
    {
        var success = false;
        try
        {
            if (data == null || string.IsNullOrEmpty(deviceId))
            {
                var badDataMessage = data == null ? "No Cosmos message to write!" : "Invalid Cosmos message - no DeviceId found!";
                MyLogger.LogError(badDataMessage, dataSource, deviceId);
                return (false, badDataMessage);
            }

            MyLogger.LogInfo($"CosmosDB: Writing device registration message {messageId} for {deviceId} to {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DeviceContainerName}", dataSource, deviceId);
            if (await GetCosmosDeviceContainerReference(deviceId, dataSource, sb) && myDeviceContainer != null)
            {
                data.PartitionKey = deviceId;
                var existingDevice = myDeviceContainer.GetItemLinqQueryable<Register>(true).Where(x => x.PartitionKey == deviceId).ToList().FirstOrDefault();
                if (existingDevice != null)
                {
                    data.MessageId = existingDevice.MessageId;
                }
                var response = await myDeviceContainer.UpsertItemAsync(data, new PartitionKey(data.PartitionKey));
                success = response.StatusCode == System.Net.HttpStatusCode.OK;
                return (success, success ? "Cosmos Write Success" : "Cosmos Write Failure");
            }
            var noContainerMsg = "Error connecting to Cosmos Data Container!";
            MyLogger.LogInfo(noContainerMsg, dataSource, deviceId);
            return (false, noContainerMsg);
        }
        catch (Exception ex)
        {
            var baseMsg = $"Error writing to Cosmos Device Container!";
            var errorMsg = $"{baseMsg} {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName}. Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, deviceId);
            return (false, baseMsg);
        }
    }

    /// <summary>
    /// Get reference to the Cosmos Data Container
    /// </summary>
    private async Task<bool> GetCosmosDataContainerReference(string deviceId, string dataSource, StringBuilder sb)
    {
        if (myDataContainer != null) return true;
        (var success, myDataContainer) = await GetCosmosContainerReference(secrets.CosmosConnectionString, Constants.Cosmos.DatabaseName, Constants.Cosmos.DataContainerName, "/partitionKey", deviceId, dataSource, sb);
        return success;
    }

    /// <summary>
    /// Get reference to the Cosmos Device Container
    /// </summary>
    private async Task<bool> GetCosmosDeviceContainerReference(string deviceId, string dataSource, StringBuilder sb)
    {
        if (myDeviceContainer != null) return true;
        (var success, myDeviceContainer) = await GetCosmosContainerReference(secrets.CosmosConnectionString, Constants.Cosmos.DatabaseName, Constants.Cosmos.DeviceContainerName, "/partitionKey", deviceId, dataSource, sb);
        return success;
    }

    /// <summary>
    /// Get a reference to a Cosmos Container
    /// </summary>
    private static async Task<(bool, Container)> GetCosmosContainerReference(string connectString, string databaseName, string containerName, string partitionKey, string deviceId, string dataSource, StringBuilder sb)
    {
        try
        {
            //MyLogger.LogInfo($"Creating Cosmos reference to {databaseName}.{containerName}!", dataSource, deviceId);
            var client = new CosmosClientBuilder(connectString)
                .WithSerializerOptions(new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase })
                .Build();
            var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            // how can I set this...?   var containerOptions = new ContainerProperties { DefaultTimeToLive = 604800 }; // 60 * 60 * 24 * 7 = 604,800 seconds = 1 week;  1 month = 18,144,000 seconds
            var container = await database.Database.CreateContainerIfNotExistsAsync(containerName, partitionKey);
            var myContainer = client.GetContainer(databaseName, containerName);
            return (true, myContainer);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error creating Cosmos Container Reference {Utilities.GetSanitizedConnectionString(connectString)} -> {databaseName}.{containerName}. Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, deviceId, sb);
            return (false, null);
        }
    }
}
