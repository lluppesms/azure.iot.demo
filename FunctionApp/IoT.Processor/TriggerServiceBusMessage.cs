namespace IoT.Processor;

/// <summary>
/// Service Bus Trigger
/// </summary>
public class TriggerServiceBusMessage
{
    #region Variables
    /// <summary>
    /// App Insights Data Source Attribute
    /// </summary>
    private readonly string DataSource = Constants.TriggerSource.ServiceBusMessage;

    /// <summary>
    /// Class that contains all the business logic to process an incoming record
    /// </summary>
    private readonly MessageProcessor messageProcessor = null;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializer
    /// </summary>
    public TriggerServiceBusMessage(IOptions<MyConfiguration> diConfig, IOptions<MySecrets> diSecrets, ILogger<TriggerAPI> logger)
    {
        MyLogger.InitializeLogger(logger);
        messageProcessor = new MessageProcessor(diConfig.Value, diSecrets.Value);
    }
    #endregion

    /// <summary>
    /// Service Bus Trigger Function
    /// </summary>
    [FunctionName("TriggerServiceBusMessage")]
    public async Task Run([ServiceBusTrigger("iotmsgs", Connection = "ServiceBusConnectionString")] string requestBody, ILogger log)
    {
        try
        {
            MyLogger.LogInfo($"{DataSource} Trigger was activated with {requestBody}!", DataSource);

            // Expecting data like this: {"deviceId":"device1","eventTypeCode":"Register","eventDateTime":"2022-06-27T11:30"}
            //                       or: {"deviceId":"device1","eventTypeCode":"Heartbeat","eventDateTime":"2022-06-27T11:30","temperature":70.1}
            (var success, var responseMessage) = await messageProcessor.ProcessMessage(requestBody, DataSource);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error receiving message from {DataSource}: {requestBody}  Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, DataSource);
        }
    }
}
