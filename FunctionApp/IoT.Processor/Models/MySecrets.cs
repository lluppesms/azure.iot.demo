namespace IoT.Processor.Models;

public class MySecrets
{
    /// <summary>
    /// IoT Hub Connection String
    /// </summary>
    public string IoTHubConnectionString { get; set; }

    /// <summary>
    /// SignalR Connection String
    /// </summary>
    public string SignalRConnectionString { get; set; }

    /// <summary>
    /// Cosmos Connection String
    /// </summary>
    public string CosmosConnectionString { get; set; }

    /// <summary>
    /// IoT Hub File Upload Storage Connection String
    /// </summary>
    public string IotStorageAccountConnectionString { get; set; }

    /// <summary>
    /// Echo out values
    /// </summary>
    public void DisplayValues(string dataSource)
    {
        MyLogger.LogInfo($"MySecrets.IoTHubConnectionString: {IoTHubConnectionString}", dataSource);
        MyLogger.LogInfo($"MySecrets.SignalRConnectionString: {SignalRConnectionString}", dataSource);
        MyLogger.LogInfo($"MySecrets.CosmosConnectionString: {CosmosConnectionString}", dataSource);
        MyLogger.LogInfo($"MySecrets.IotStorageAccountConnectionString: {IotStorageAccountConnectionString}", dataSource);
    }
}
