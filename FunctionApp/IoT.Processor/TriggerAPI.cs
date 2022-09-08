namespace IoT.Processor;

/// <summary>
/// API Trigger
/// </summary>
public class TriggerAPI
{
    #region Variables
    /// <summary>
    /// App Insights Data Source Attribute
    /// </summary>
    private readonly string DataSource = Constants.TriggerSource.API;

    /// <summary>
    /// Class that contains all the business logic to process an incoming record
    /// </summary>
    private readonly MessageProcessor messageProcessor = null;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializer
    /// </summary>
    public TriggerAPI(IOptions<MyConfiguration> diConfig, IOptions<MySecrets> diSecrets, ILogger<TriggerAPI> logger)
    {
        MyLogger.InitializeLogger(logger);
        messageProcessor = new MessageProcessor(diConfig.Value, diSecrets.Value);
    }
    #endregion

    /// <summary>
    /// API Trigger Function
    /// </summary>
    [FunctionName("TriggerAPI")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
    {
        var requestBody = string.Empty;
        try
        {
            // Expecting data like this: {"deviceId":"device1","eventTypeCode":"Register","eventDateTime":"2022-06-27T11:30"}
            //                       or: {"deviceId":"device1","eventTypeCode":"Heartbeat","eventDateTime":"2022-06-27T11:30","temperature":70.1}
            requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            MyLogger.LogInfo($"{DataSource} Trigger was activated with {requestBody}!", DataSource);

            // for local testing only: if you simply hit the API in a browser, supply ?eventTypeCode=Heartbeat or Register or File it will autofill request with test data
            if (string.IsNullOrEmpty(requestBody) && req.Host.ToString().StartsWith("http://localhost:7071/api/TriggerAPI", StringComparison.InvariantCultureIgnoreCase))
            {
                requestBody = GetSampleData(req);
            }

            (var success, var responseMessage) = await messageProcessor.ProcessMessage(requestBody, DataSource);
            return success ? new OkObjectResult(responseMessage) : new BadRequestObjectResult(responseMessage);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error receiving message from {DataSource}: {requestBody}  Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, DataSource);
            return new BadRequestObjectResult(errorMsg);
        }
    }

    /// <summary>
    /// Supply some sample data for easy testing in browser from localhost
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    private static string GetSampleData(HttpRequest req)
    {
        // Expecting data like this: {"deviceId":"device1","eventTypeCode":"Register","eventDateTime":"2022-06-27T11:30"}
        //                       or: {"deviceId":"device1","eventTypeCode":"Heartbeat","eventDateTime":"2022-06-27T11:30","temperature":70.1}
        string eventType = req.Query["eventTypeCode"];
        if (string.IsNullOrEmpty(eventType)) { eventType = Constants.EventType.Register; }
        var body = eventType.ToUpper().Trim() switch
        {
            "HEARTBEAT" => JsonConvert.SerializeObject(new Heartbeat("localtest")),
            "REGISTER" => JsonConvert.SerializeObject(new Register("localtest")),
            "FILE" => JsonConvert.SerializeObject(new StorageNotification($"Heartbeats-{DateTime.Today:yy-MM-dd-HH-mm-ss}.json", "localtest")),
            _ => string.Empty
        };
        return body;
    }
}
