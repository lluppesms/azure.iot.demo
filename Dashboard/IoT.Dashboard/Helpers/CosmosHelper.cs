namespace IoT.Dashboard.Helpers;

using Constants = IoT.Dashboard.Models.Constants;
using Container = Microsoft.Azure.Cosmos.Container;

/// <summary>
/// Encapsulates all of the Cosmos related plumbing
/// </summary>
public class CosmosHelper
{
    #region Variables
    /// <summary>
    /// Configuration
    /// </summary>
    private AppSettings settings;

    /// <summary>
    /// Cosmos Client
    /// </summary>
    private static CosmosClient client = null;

    /// <summary>
    /// Reference to a Cosmos Data Container
    /// </summary>
    private Container myDataContainer = null;

    ///// <summary>
    ///// Reference to a Cosmos Device Container
    ///// </summary>
    //private Container myDeviceContainer = null;
    #endregion

    #region Initialization
    /// <summary>
    /// Constructor
    /// </summary>
    public CosmosHelper(AppSettings diSetting)
    {
        settings = diSetting;
    }
    #endregion

    /// <summary>
    /// Read Cosmos Data Container
    /// </summary>
    public async Task<(List<DeviceData>, string)> ReadManyRecords(string deviceId, string eventTypeCode, int startHoursToSubtract = 1, int endHoursToSubtract = 0, int maxRows = 25)
    {
        if (string.IsNullOrEmpty(deviceId) || !Utilities.ValidateIoTDeviceId(deviceId))
        {
            var badDataMessage = $"Cosmos: Scanning {Constants.Cosmos.DataContainerName}: No deviceId or invalid deviceId specified!";
            MyLogger.LogError(badDataMessage, Constants.Source.CosmosHelper);
            return (new List<DeviceData>(), badDataMessage);
        }
        var data = new List<DeviceData>();
        var stopWatch = new Stopwatch();

        var startDate = DateTime.UtcNow.AddHours(-1 * startHoursToSubtract);
        var endDate = DateTime.UtcNow.AddHours(-1 * endHoursToSubtract);

        eventTypeCode = Utilities.IsOnlyNumbersOrLetters(eventTypeCode, 30);
        var typeMsg = !string.IsNullOrEmpty(eventTypeCode) ? eventTypeCode : "ALL";

        try
        {
            stopWatch.Start();
            var endHoursMsg = (endHoursToSubtract > 0) ? $" to {endHoursToSubtract} hours" : "";
            MyLogger.LogInfo($"Cosmos: Scanning {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName} for {deviceId} type '{typeMsg}' last {startHoursToSubtract} hours{endHoursMsg} ago", Constants.Source.CosmosHelper);
            if (await GetCosmosDataContainerReference() && myDataContainer != null)
            {
                var typeFilter = !string.IsNullOrEmpty(eventTypeCode) ? " and c.eventTypeCode = @eventTypeCode" : "";
                var sqlQueryText = $"select * from DeviceData c where c.deviceId = @deviceId and c.eventDateTime >= @startDateTime and c.eventDateTime <= @endDateTime {typeFilter} order by c.eventDateTime DESC";
                var queryDefinition = new QueryDefinition(sqlQueryText)
                    .WithParameter("@deviceId", deviceId)
                    .WithParameter("@startDateTime", startDate)
                    .WithParameter("@endDateTime", endDate);
                if (!string.IsNullOrEmpty(typeFilter))
                {
                    queryDefinition.WithParameter("@eventTypeCode", eventTypeCode);
                }
                using var queryResultSetIterator = myDataContainer.GetItemQueryIterator<DeviceData>(queryDefinition, requestOptions: new QueryRequestOptions() { MaxItemCount = maxRows });
                while (queryResultSetIterator.HasMoreResults)
                {
                    var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (var item in currentResultSet)
                    {
                        data.Add(item);
                    }
                }
            }
            stopWatch.Stop();
            var msg = $"Read {data.Count} records for device {deviceId} of type {typeMsg} for last {startHoursToSubtract} hours{endHoursMsg} ago in {stopWatch.ElapsedMilliseconds}ms";
            MyLogger.LogInfo(msg, Constants.Source.CosmosHelper);
            return (data, msg);
        }
        catch (Exception ex)
        {
            var baseMsg = $"Error reading SQL DeviceData from Cosmos Data Container!";
            var errorMsg = $"{baseMsg} {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName}. Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, Constants.Source.CosmosHelper);
            return (new List<DeviceData>(), errorMsg);
        }
    }

    /// <summary>
    /// Read One Cosmos Record
    /// </summary>
    public async Task<(string, string)> ReadOneRecord(Guid messageId, string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey) || messageId == Guid.Empty)
        {
            var badDataMessage = $"Cosmos: Scanning {Constants.Cosmos.DataContainerName}: No message key specified!";
            MyLogger.LogError(badDataMessage, Constants.Source.CosmosHelper);
            return (string.Empty, badDataMessage);
        }

        var stopWatch = new Stopwatch();
        var data = string.Empty;
        try
        {
            stopWatch.Start();
            MyLogger.LogInfo($"Cosmos: Scanning {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName} for message {messageId}", Constants.Source.CosmosHelper);
            if (await GetCosmosDataContainerReference() && myDataContainer != null)
            {
                var sqlQueryText = $"select * from DeviceData c where c.id = '{messageId}' and c.partitionKey = '{partitionKey}'";
                var resultSet = myDataContainer.GetItemQueryIterator<JObject>(new QueryDefinition(sqlQueryText));
                var item = (await resultSet.ReadNextAsync()).First();
                if (item != null)
                {
                    item.Remove("_rid");
                    item.Remove("_self");
                    item.Remove("_etag");
                    item.Remove("_attachments");
                    item.Remove("_ts");
                }
                data = $"{item}";
            }
            stopWatch.Stop();
            var msg = $"Read 1 record {messageId} in {stopWatch.ElapsedMilliseconds}ms";
            MyLogger.LogInfo(msg, Constants.Source.CosmosHelper);
            return (data, msg);
        }
        catch (Exception ex)
        {
            var baseMsg = $"Error reading one DeviceData record from Cosmos Data Container!";
            var errorMsg = $"{baseMsg} {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName}. Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, Constants.Source.CosmosHelper);
            return (string.Empty, baseMsg);
        }
    }

    /// <summary>
    /// Get reference to the Cosmos Data Container
    /// </summary>
    private async Task<bool> GetCosmosDataContainerReference()
    {
        if (myDataContainer != null) return true;
        (var success, myDataContainer) = await GetCosmosContainerReference(settings.CosmosConnectionString, Constants.Cosmos.DatabaseName, Constants.Cosmos.DataContainerName, "/partitionKey");
        return success;
    }

    ///// <summary>
    ///// Get reference to the Cosmos Device Container
    ///// </summary>
    //private async Task<bool> GetCosmosDeviceContainerReference(string deviceId, string dataSource)
    //{
    //    if (myDeviceContainer != null) return true;
    //    (var success, myDeviceContainer) = await GetCosmosContainerReference(settings.CosmosConnectionString, Constants.Cosmos.DatabaseName, Constants.Cosmos.DeviceContainerName, "/partitionKey", deviceId, dataSource);
    //    return success;
    //}

    /// <summary>
    /// Get a reference to a Cosmos Container
    /// </summary>
    private static async Task<(bool, Container)> GetCosmosContainerReference(string connectString, string databaseName, string containerName, string partitionKey)
    {
        try
        {
            if (client == null)
            {
                client = new CosmosClientBuilder(connectString)
                .WithSerializerOptions(new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase })
                .Build();
            }
            var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            var container = await database.Database.CreateContainerIfNotExistsAsync(containerName, partitionKey);
            var myContainer = client.GetContainer(databaseName, containerName);
            return (true, myContainer);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error creating Cosmos Container Reference {Utilities.GetSanitizedConnectionString(connectString)} -> {databaseName}.{containerName}. Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, Constants.Source.CosmosHelper);
            return (false, null);
        }
    }

    ///// <summary>
    ///// Read Cosmos Data Container
    ///// </summary>
    //public async Task<(List<DeviceData>, string)> ReadManyRecordsLinq(string deviceId, string eventTypeCode, int startHoursToSubtract = 1, int endHoursToSubtract = 0, int maxRows = 25)
    //{
    //    if (string.IsNullOrEmpty(deviceId) || !Utilities.ValidateIoTDeviceId(deviceId))
    //    {
    //        var badDataMessage = $"Cosmos: Scanning {Constants.Cosmos.DataContainerName}: No deviceId or invalid deviceId specified!";
    //        MyLogger.LogError(badDataMessage, Constants.Source.CosmosHelper);
    //        return (new List<DeviceData>(), badDataMessage);
    //    }

    //    var data = new List<DeviceData>();
    //    var stopWatch = new Stopwatch();

    //    var startDate = DateTime.UtcNow.AddHours(-1 * startHoursToSubtract);
    //    var endDate = DateTime.UtcNow.AddHours(-1 * endHoursToSubtract);

    //    eventTypeCode = Utilities.IsOnlyNumbersOrLetters(eventTypeCode, 30);
    //    var typeMsg = !string.IsNullOrEmpty(eventTypeCode) ? eventTypeCode : "ALL";

    //    try
    //    {
    //        stopWatch.Start();
    //        var endHoursMsg = (endHoursToSubtract > 0) ? $" to {endHoursToSubtract} hours" : "";
    //        var logMsg = $"Cosmos: Scanning {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName} for {deviceId} type '{typeMsg}' last {startHoursToSubtract} hours{endHoursMsg} ago";
    //        MyLogger.LogInfo(logMsg, Constants.Source.CosmosHelper);
    //        if (await GetCosmosDataContainerReference() && myDataContainer != null)
    //        {
    //            using (var setIterator = myDataContainer.GetItemLinqQueryable<DeviceData>()
    //                  .Where(d =>
    //                    d.DeviceId == deviceId &&
    //                    d.EventDateTime >= startDate &&
    //                    d.EventDateTime <= endDate &&
    //                    (string.IsNullOrEmpty(eventTypeCode) || d.EventTypeCode == eventTypeCode)
    //                  )
    //                  .OrderByDescending(d => d.EventDateTime)
    //                  .Take(maxRows)
    //                  .ToFeedIterator<DeviceData>())
    //            {
    //                var rowCount = 0;
    //                while (setIterator.HasMoreResults)
    //                {
    //                    foreach (var item in await setIterator.ReadNextAsync())
    //                    {
    //                        rowCount++;
    //                        item.RowId = rowCount;
    //                        data.Add(item);
    //                        if (rowCount >= maxRows)
    //                        {
    //                            break;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        stopWatch.Stop();
    //        var msg = $"Read {data.Count} records for device {deviceId} of type {typeMsg} for last {startHoursToSubtract} hours{endHoursMsg} ago in {stopWatch.ElapsedMilliseconds}ms";
    //        MyLogger.LogInfo(msg, Constants.Source.CosmosHelper);
    //        return (data, msg);
    //    }
    //    catch (Exception ex)
    //    {
    //        var baseMsg = $"Error reading LINQ DeviceData from Cosmos Data Container!";
    //        var errorMsg = $"{baseMsg} {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName}. Error: {Utilities.GetExceptionMessage(ex)}";
    //        MyLogger.LogError(errorMsg, Constants.Source.CosmosHelper);
    //        return (new List<DeviceData>(), baseMsg);
    //    }
    //}

    ///// <summary>
    ///// Send to Cosmos Data Container
    ///// </summary>
    //public async Task<(bool, string)> SendDataToCosmos(string deviceId, string partitionKeyDate, Guid messageId, dynamic data, string dataSource)
    //{
    //    var success = false;
    //    try
    //    {
    //        if (data == null || string.IsNullOrEmpty(deviceId))
    //        {
    //            var badDataMessage = data == null ? "No Cosmos message to write!" : "Invalid Cosmos message - no DeviceId found!";
    //            MyLogger.LogError(badDataMessage, dataSource, deviceId);
    //            return (false, badDataMessage);
    //        }

    //        MyLogger.LogInfo($"Cosmos: Writing message {messageId} for device {deviceId} to {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName}", dataSource, deviceId);
    //        if (await GetCosmosDataContainerReference(deviceId, dataSource) && myDataContainer != null)
    //        {
    //            // Warning: partition key with only deviceId will eventually fill up partition and cause everything to crash, so append current date to the key!
    //            data.PartitionKey = $"{deviceId}-{partitionKeyDate}";
    //            var response = await myDataContainer.CreateItemAsync(data, new PartitionKey(data.PartitionKey));
    //            success = response.StatusCode == System.Net.HttpStatusCode.Created;
    //            return (success, success ? "Cosmos Write Success" : "Cosmos Write Failure");
    //        }
    //        var noContainerMsg = "Error connecting to Cosmos Data Container!";
    //        MyLogger.LogInfo(noContainerMsg, dataSource, deviceId);
    //        return (false, noContainerMsg);
    //    }
    //    catch (Exception ex)
    //    {
    //        var baseMsg = $"Error writing to Cosmos Data Container!";
    //        var errorMsg = $"{baseMsg} {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName}. Error: {Utilities.GetExceptionMessage(ex)}";
    //        MyLogger.LogError(errorMsg, dataSource, deviceId);
    //        return (false, baseMsg);
    //    }
    //}

    ///// <summary>
    ///// Send Device Information to Cosmos
    ///// </summary>
    //public async Task<(bool, string)> SendDeviceToCosmos(string deviceId, string partitionKeyDate, Guid messageId, Register data, string dataSource)
    //{
    //    var success = false;
    //    try
    //    {
    //        if (data == null || string.IsNullOrEmpty(deviceId))
    //        {
    //            var badDataMessage = data == null ? "No Cosmos message to write!" : "Invalid Cosmos message - no DeviceId found!";
    //            MyLogger.LogError(badDataMessage, dataSource, deviceId);
    //            return (false, badDataMessage);
    //        }

    //        MyLogger.LogInfo($"Cosmos: Writing device registration message {messageId} for {deviceId} to {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DeviceContainerName}", dataSource, deviceId);
    //        if (await GetCosmosDeviceContainerReference(deviceId, dataSource) && myDeviceContainer != null)
    //        {
    //            data.PartitionKey = deviceId;
    //            var existingDevice = myDeviceContainer.GetItemLinqQueryable<Register>(true).Where(x => x.PartitionKey == deviceId).ToList().FirstOrDefault();
    //            if (existingDevice != null)
    //            {
    //                data.MessageId = existingDevice.MessageId;
    //            }
    //            var response = await myDeviceContainer.UpsertItemAsync(data, new PartitionKey(data.PartitionKey));
    //            success = response.StatusCode == System.Net.HttpStatusCode.OK;
    //            return (success, success ? "Cosmos Write Success" : "Cosmos Write Failure");
    //        }
    //        var noContainerMsg = "Error connecting to Cosmos Data Container!";
    //        MyLogger.LogInfo(noContainerMsg, dataSource, deviceId);
    //        return (false, noContainerMsg);
    //    }
    //    catch (Exception ex)
    //    {
    //        var baseMsg = $"Error writing to Cosmos Device Container!";
    //        var errorMsg = $"{baseMsg} {Constants.Cosmos.DatabaseName}.{Constants.Cosmos.DataContainerName}. Error: {Utilities.GetExceptionMessage(ex)}";
    //        MyLogger.LogError(errorMsg, dataSource, deviceId);
    //        return (false, baseMsg);
    //    }
    //}
}
