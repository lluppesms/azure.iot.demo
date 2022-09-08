namespace IoT.Processor;

/// <summary>
/// Trigger Function for File Uploads -- Service Bus fed by Event Grid
/// </summary>
public class TriggerFileUpload
{
    #region Variables
    /// <summary>
    /// App Insights Data Source Attribute
    /// </summary>
    private readonly string DataSource = Constants.TriggerSource.FileUpload;

    /// <summary>
    /// Class that contains all the business logic to process an incoming record
    /// </summary>
    private readonly MessageProcessor messageProcessor = null;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializer
    /// </summary>
    public TriggerFileUpload(IOptions<MyConfiguration> diConfig, IOptions<MySecrets> diSecrets, ILogger<TriggerAPI> logger)
    {
        MyLogger.InitializeLogger(logger);
        messageProcessor = new MessageProcessor(diConfig.Value, diSecrets.Value);
    }
    #endregion

    /// <summary>
    /// Trigger Function for File Uploads -- Service Bus fed by Event Grid
    /// </summary>
    [FunctionName("TriggerFileUpload")]
    public async Task Run([ServiceBusTrigger("filemsgs", Connection = "ServiceBusConnectionString")] string requestBody, ILogger log)
    {
        try
        {
            MyLogger.LogInfo($"{DataSource} Trigger was activated with {requestBody}!", DataSource);
            if (!requestBody.Contains("azure-webjobs-hosts/blobs/timers", StringComparison.InvariantCultureIgnoreCase))
            {
                MyLogger.LogInfo($"FileProcessor received message: {requestBody}", Constants.TriggerSource.FileUpload);
                // Expecting data like this: {"topic":"/subscriptions/<subscriptionidguid>/resourceGroups/rg_iotdev/providers/Microsoft.Storage/storageAccounts/iothubstoragename","subject":"/blobServices/default/containers/iothubstoragename/blobs/devicename/Heartbeats.json","eventType":"Microsoft.Storage.BlobCreated","id":"bd729b2c-201e-0076-0bf3-8b354f06abcf"}
                (var success, var responseMessage) = await messageProcessor.ProcessFile(requestBody, DataSource);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error receiving message from {DataSource}: {requestBody}  Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, DataSource);
        }
    }
}
