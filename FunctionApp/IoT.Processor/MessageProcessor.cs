namespace IoT.Processor;

/// <summary>
/// Contains all of the logic to process a message
/// </summary>
public class MessageProcessor
{
    #region Variables
    /// <summary>
    /// Cosmos Helper 
    /// </summary>
    private readonly CosmosHelper cosmosHelper;

    /// <summary>
    /// SignalR Helper 
    /// </summary>
    private readonly SignalRHelper signalRHelper;

    /// <summary>
    /// Configuration Settings
    /// </summary>
    private MyConfiguration myConfig = null;

    /// <summary>
    /// Secrets
    /// </summary>
    private MySecrets mySecrets = null;
    #endregion

    #region Initialization
    /// <summary>
    /// Class Initializer
    /// </summary>
    public MessageProcessor(MyConfiguration parmConfig, MySecrets parmSecrets)
    {
        mySecrets = parmSecrets;
        myConfig = parmConfig;
        cosmosHelper = new CosmosHelper(myConfig, mySecrets);
        signalRHelper = new SignalRHelper(myConfig, mySecrets);
    }
    #endregion

    /// <summary>
    /// Process one message
    /// </summary>
    public async Task<(bool success, string responseMessage)> ProcessMessage(string message, string dataSource, StringBuilder sb = null)
    {
        var deviceId = string.Empty;
        var responseMessage = string.Empty;
        string recordType;
        string partitionKeyDate;

        try
        {
            (var validData, deviceId, recordType, partitionKeyDate, responseMessage) = GetBasicInfo(message, dataSource, sb);
            if (!validData)
            {
                return (false, responseMessage);
            }

            MyLogger.LogInfo($"Processing {recordType} for data {deviceId}: {message}", dataSource, deviceId, sb);
            var success = false;
            switch (recordType)
            {
                case Constants.EventType.Register:
                    (success, responseMessage) = await ProcessRegistration(deviceId, message, partitionKeyDate, dataSource, sb);
                    break;
                case Constants.EventType.Storage:
                    (success, responseMessage) = await ProcessFile(message, dataSource);
                    break;
                case Constants.EventType.Heartbeat:
                    (success, responseMessage) = await ProcessHeartbeat(deviceId, message, partitionKeyDate, dataSource, sb);
                    break;
                default:
                    (success, responseMessage) = await ProcessOther(deviceId, message, partitionKeyDate, dataSource, sb);
                    break;
            }

            responseMessage += success ? "; message processed successfully!" : "; error processing message!";
            MyLogger.LogInfoOrError(success, responseMessage, responseMessage, dataSource, deviceId, sb);
            return (success, responseMessage);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error processing message from {dataSource} for {deviceId}: {message}  Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, deviceId, sb);
            return (false, responseMessage);
        }
    }

    /// <summary>
    /// Process one file
    /// </summary>
    public async Task<(bool success, string responseMessage)> ProcessFile(string message, string dataSource, StringBuilder sb = null)
    {
        var deviceId = string.Empty;
        var responseMessage = string.Empty;
        var success = false;

        try
        {
            var msg = JsonConvert.DeserializeObject<StorageNotification>(message);
            deviceId = Utilities.DeriveDeviceIdFromBlobName(msg.subject);

            if (msg.eventType == "Microsoft.Storage.BlobDeleted")
            {
                responseMessage = $"File '{msg.subject}' was deleted!";
                MyLogger.LogInfo(responseMessage, dataSource, deviceId);
                return (success, responseMessage);
            }
            if (msg.subject.EndsWith(".log", StringComparison.InvariantCultureIgnoreCase))
            {
                responseMessage = $"Log File ignored: '{msg.subject}'";
                MyLogger.LogInfo(responseMessage, dataSource, deviceId);
                return (success, responseMessage);
            }

            var containerNameLocation = msg.subject.IndexOf(Constants.Storage.FileUploadFolder);
            if (containerNameLocation <= 0)
            {
                //// When a timer trigger runs it also creates a file - ignore names like the following, but log other names for further examination
                ////   "subject":"/blobServices/default/containers/azure-webjobs-hosts/blobs/timers/<functionApp>/<functionName>/status",
                if (!msg.subject.Contains("azure-webjobs-hosts/blobs/timers"))
                {
                    MyLogger.LogWarning($"Invalid File Upload: '{msg.subject}' is being ignored...", dataSource, deviceId);
                }
            }

            var offset = containerNameLocation + Constants.Storage.FileUploadFolder.Length + 6; //// length of "blobs/"
            var fileName = msg.subject[offset..];
            (success, responseMessage) = await ProcessFileContents(fileName, dataSource, sb);
            return (success, responseMessage);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error processing file from {deviceId}: {message}  Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, deviceId);
            return (false, responseMessage);
        }
    }

    /// <summary>
    /// Get the basic info from the message
    /// </summary>
    private static (bool validData, string deviceId, string recordType, string recordDateYYYYMMDD, string responseMessage) GetBasicInfo(string message, string dataSource, StringBuilder sb = null)
    {
        var responseMessage = string.Empty;
        var recordDateYYYYMMDD = $"{DateTime.Now.Year}{DateTime.Now.Month:D2}{DateTime.Now.Day:D2}";

        if (string.IsNullOrEmpty(message)) { return (false, string.Empty, string.Empty, string.Empty, "No message body found!"); }

        var input = JsonConvert.DeserializeObject<BasicInput>(message);
        if (input == null)
        {
            responseMessage = "Invalid message!";
            MyLogger.LogError(responseMessage, dataSource, string.Empty, sb);
            return (false, string.Empty, string.Empty, recordDateYYYYMMDD, responseMessage);
        }
        input.ScrubData(message);
        // Look for timestamp in the record and pass back the date format of that stamp for a partition key, or use today's date if not found...
        if (input.EventDateTime != null)
        {
            recordDateYYYYMMDD = ((DateTime)input.EventDateTime).ToString("yyyyMMdd");
        }
        if (!input.IsValid())
        {
            responseMessage = "Invalid message - no DeviceId found in message!";
            MyLogger.LogError(responseMessage, dataSource, string.Empty, sb);
            return (false, string.Empty, string.Empty, recordDateYYYYMMDD, responseMessage);
        }
        return (true, input.DeviceId, input.EventTypeCode, recordDateYYYYMMDD, responseMessage);
    }

    /// <summary>
    /// Process one heartbeat
    /// </summary>
    private async Task<(bool, string)> ProcessRegistration(string deviceId, string message, string partitionKeyDate, string dataSource, StringBuilder sb = null)
    {
        var responseMessage = string.Empty;
        var success = false;
        var toCosmos = false;
        var toSignalR = false;
        string cosmosMessage;
        string signalRMessage;

        try
        {
            var registration = JsonConvert.DeserializeObject<Register>(message);
            if (registration.IsValid())
            {
                responseMessage = $"Device '{deviceId}' Registration received, Device Time: {registration.EventDateTime:yyyy-MM-dd HH:mm:ss}Z";
                registration.MessageId = registration.MessageId == Guid.Empty ? Guid.NewGuid() : registration.MessageId;
                if (myConfig.WriteToCosmos())
                {
                    (toCosmos, cosmosMessage) = await cosmosHelper.SendDeviceToCosmos(registration.DeviceId, partitionKeyDate, registration.MessageId, registration, dataSource, sb);
                    responseMessage += toCosmos ? "; written to Cosmos" : $"; write to Cosmos failed! {cosmosMessage}";
                    success = true;
                }
                else
                {
                    responseMessage += $"; Cosmos not enabled";
                }
                if (myConfig.WriteToSignalR())
                {
                    (toSignalR, signalRMessage) = await signalRHelper.SendToSignalR(registration.DeviceId, registration.MessageId, message, dataSource, sb);
                    responseMessage += toSignalR ? "; written to SignalR" : $"; write to SignalR failed! {signalRMessage}";
                    success = true;
                }
                else
                {
                    responseMessage += $"; SignalR not enabled";
                }
                if (myConfig.WriteToCosmos() && myConfig.WriteToSignalR())
                {
                    success = toCosmos && toSignalR;
                }
            }
            else
            {
                responseMessage = "Body did not convert to a valid device registration!";
            }
            return (success, responseMessage);
        }
        catch (Exception ex)
        {
            var baseMsg = $"Error processing device registration from {dataSource} for {deviceId}: {message}";
            var errorMsg = $"{baseMsg}  Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, deviceId, sb);
            return (false, baseMsg);
        }
    }

    /// <summary>
    /// Process one heartbeat
    /// </summary>
    private async Task<(bool, string)> ProcessHeartbeat(string deviceId, string message, string partitionKeyDate, string dataSource, StringBuilder sb = null)
    {
        bool validData;
        var responseMessage = string.Empty;
        var success = false;
        var toCosmos = false;
        var toSignalR = false;
        string cosmosMessage;
        string signalRMessage;
        var messageId = Guid.Empty;

        try
        {
            var heartbeat = JsonConvert.DeserializeObject<Heartbeat>(message);
            if (heartbeat != null && heartbeat.IsValid())
            {
                heartbeat.MessageId = heartbeat.MessageId == Guid.Empty ? Guid.NewGuid() : heartbeat.MessageId;
                messageId = heartbeat.MessageId;
                if (heartbeat.Temperature == null && string.IsNullOrEmpty(heartbeat.AdditionalData))
                {
                    responseMessage = $"Body does not have a valid data (needs Temperature or AdditionalData)!";
                    validData = false;
                }

                if (heartbeat.Temperature != null)
                {
                    responseMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}: Received Temperature of {heartbeat.Temperature} on {heartbeat.EventDateTime} for device {heartbeat.DeviceId}; MessageId: {heartbeat.MessageId}";
                    validData = true;
                }
                else
                {
                    responseMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}: Received Unstructured Data of {heartbeat.AdditionalData} on {heartbeat.EventDateTime} for device {heartbeat.DeviceId}; MessageId: {heartbeat.MessageId}";
                    validData = true;
                }

                if (validData)
                {
                    if (myConfig.WriteToCosmos())
                    {
                        (toCosmos, cosmosMessage) = await cosmosHelper.SendDataToCosmos(heartbeat.DeviceId, partitionKeyDate, heartbeat.MessageId, heartbeat, dataSource, sb);
                        responseMessage += toCosmos ? "; written to Cosmos" : $"; write to Cosmos failed! {cosmosMessage}";
                        success = true;
                    }
                    else
                    {
                        responseMessage += $"; Cosmos not enabled";
                    }
                    if (myConfig.WriteToSignalR())
                    {
                        (toSignalR, signalRMessage) = await signalRHelper.SendToSignalR(heartbeat.DeviceId, heartbeat.MessageId, message, dataSource, sb);
                        responseMessage += toSignalR ? "; written to SignalR" : $"; write to SignalR failed! {signalRMessage}";
                        success = true;
                    }
                    else
                    {
                        responseMessage += $"; SignalR not enabled";
                    }
                    if (myConfig.WriteToCosmos() && myConfig.WriteToSignalR())
                    {
                        success = toCosmos && toSignalR;
                    }
                }
            }
            else
            {
                responseMessage = "Body did not convert to a valid heartbeat!";
            }
            return (success, responseMessage);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error processing heartbeat MessageId {messageId} from {dataSource} for {deviceId}: {message}  Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, deviceId, sb);
            return (false, responseMessage);
        }
    }

    /// <summary>
    /// Process other type of message
    /// </summary>
    private async Task<(bool, string)> ProcessOther(string deviceId, string message, string partitionKeyDate, string dataSource, StringBuilder sb = null)
    {
        var responseMessage = string.Empty;
        var success = false;
        var toCosmos = false;
        var toSignalR = false;
        string cosmosMessage;
        string signalRMessage;
        var messageId = Guid.Empty;

        try
        {
            var record = JsonConvert.DeserializeObject<BasicInput>(message);
            if (record != null && record.IsValid())
            {
                record.MessageId = record.MessageId == Guid.Empty ? Guid.NewGuid() : record.MessageId;
                messageId = record.MessageId;
                responseMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}: Received Unstructured Data of {record.AdditionalData} on {record.EventDateTime} for device {record.DeviceId}; MessageId: {record.MessageId}";

                if (myConfig.WriteToCosmos())
                {
                    (toCosmos, cosmosMessage) = await cosmosHelper.SendDataToCosmos(record.DeviceId, partitionKeyDate, record.MessageId, record, dataSource, sb);
                    responseMessage += toCosmos ? "; written to Cosmos" : $"; write to Cosmos failed! {cosmosMessage}";
                    success = true;
                }
                else
                {
                    responseMessage += $"; Cosmos not enabled";
                }
                if (myConfig.WriteToSignalR())
                {
                    (toSignalR, signalRMessage) = await signalRHelper.SendToSignalR(record.DeviceId, record.MessageId, message, dataSource, sb);
                    responseMessage += toSignalR ? "; written to SignalR" : $"; write to SignalR failed! {signalRMessage}";
                    success = true;
                }
                else
                {
                    responseMessage += $"; SignalR not enabled";
                }
                if (myConfig.WriteToCosmos() && myConfig.WriteToSignalR())
                {
                    success = toCosmos && toSignalR;
                }
            }
            else
            {
                responseMessage = "Body did not convert to a valid record!";
            }
            return (success, responseMessage);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error processing generic MessageId {messageId} from {dataSource} for {deviceId}: {message}  Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, deviceId, sb);
            return (false, responseMessage);
        }
    }

    /// <summary>
    /// Process an incoming file
    /// </summary>
    private async Task<(bool, string)> ProcessFileContents(string fileName, string dataSource, StringBuilder sb = null)
    {
        var deviceId = string.Empty;
        var fileLogSb = new StringBuilder();
        var recordsInFile = 0;
        var recordsProcessedSuccessfully = 0;
        var recordsFailed = 0;
        var success = false;

        try
        {
            deviceId = Utilities.DeriveDeviceIdFromFileName(fileName);

            string logTxt;
            if (!fileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase) && !fileName.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
            {
                if (fileName.EndsWith(".log", StringComparison.InvariantCultureIgnoreCase))
                {
                    logTxt = $"  File {fileName} is being ignored because it is a LOG file!";
                    MyLogger.LogWarning(logTxt, dataSource, deviceId, sb);
                    return (true, logTxt);
                }
                logTxt = $"  File {fileName} is being ignored because it did not end with JSON or ZIP or LOG!";
                MyLogger.LogWarning(logTxt, dataSource, deviceId, sb);
                return (true, logTxt);
            }

            var blob = await Utilities.GetBlobFromStorageContainer(fileName, Constants.Storage.FileUploadFolder, mySecrets.IotStorageAccountConnectionString, dataSource, sb);
            if (blob == null)
            {
                logTxt = $"{fileName} alert message was received but file was not found!";
                MyLogger.LogError(logTxt, Constants.TriggerSource.FileUpload, deviceId, sb);
                return (false, logTxt);
            }

            var stream = new MemoryStream();
            if (fileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                var zipStream = new MemoryStream();
                await blob.DownloadToStreamAsync(zipStream);
                stream = Utilities.GetUncompressedDataStream(zipStream);
            }
            else
            {
                await blob.DownloadToStreamAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            fileLogSb.AppendLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}: Starting to process file {fileName}...");
            using (var sr = new StreamReader(stream))
            {
                while (sr.Peek() >= 0)
                {
                    var data = sr.ReadLine();
                    if (data.Length > 5 && !string.IsNullOrEmpty(data) && data != "System.Byte[]")
                    {
                        recordsInFile++;
                        if (data.EndsWith("},"))
                        {
                            data = data.Substring(0, data.Length - 1);
                        }

                        (var recordSuccess, var recordResponseMessage) = await ProcessMessage(data, dataSource, sb);
                        fileLogSb.AppendLine($"{recordSuccess} {recordResponseMessage}");
                        recordsProcessedSuccessfully += recordSuccess ? 1 : 0;
                        recordsFailed += recordSuccess ? 0 : 1;
                    }
                }
                success = recordsInFile > 0 && (recordsProcessedSuccessfully == recordsInFile);
                var successStatus = success ? "successfully" : "unsuccessfully";
                var elapsedMs = stopWatch.ElapsedMilliseconds;
                logTxt = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}: Finished {successStatus} processing {fileName} for device {deviceId}; Elapsed: {elapsedMs} ms; {recordsInFile} records in file; {recordsProcessedSuccessfully} records processed successfully; {recordsFailed} failed!";
                MyLogger.LogInfoOrError(success, logTxt, logTxt, dataSource, deviceId, sb);
                var logFileName = fileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase) ? fileName[..^4] + ".log" : fileName[..^5] + ".log";
                var logContents = fileLogSb.ToString();
                using (var logFileStream = Utilities.GenerateStreamFromString(logContents))
                {
                    await Utilities.WriteBlobToStorageContainer(Constants.Storage.FileUploadFolder, logFileName, logFileStream, mySecrets.IotStorageAccountConnectionString, dataSource, sb);
                }
            }
            return (success, $"Done processing file {fileName}!");
        }
        catch (Exception ex)
        {
            var fileMessage = $"Error processing {fileName}: {recordsInFile} records read, {recordsProcessedSuccessfully} processed, {recordsFailed} failed.";
            var errorMsg = $"{fileMessage} Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, deviceId, sb);
            return (false, fileMessage);
        }
    }
}
