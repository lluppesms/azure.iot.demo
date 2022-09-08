namespace IoT.Dashboard.Models;

/// <summary>
/// Application Settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Environment Name
    /// </summary>
    public string EnvironmentName { get; set; }

    /// <summary>
    /// Application Insights Instrumentation Key
    /// </summary>
    public string ApplicationInsightsKey { get; set; }

    /// <summary>
    /// IoT Hub Connection String
    /// </summary>
    public string IoTHubConnectionString { get; set; }

    /// <summary>
    /// Storage Connection String
    /// </summary>
    public string StorageConnectionString { get; set; }

    /// <summary>
    /// Cosmos Connection String
    /// </summary>
    public string CosmosConnectionString { get; set; }

    /// <summary>
    /// SignalR Connection String
    /// </summary>
    public string SignalRConnectionString { get; set; }

    /// <summary>
    /// Application Settings
    /// </summary>
    public AppSettings()
    {
    }

    /// <summary>
    /// Application Settings
    /// </summary>
    public AppSettings(string ioTHubConnectionString, string storageConnectionString, string cosmosConnectionString, string signalRConnectionString, string applicationInsightsKey)
    {
        IoTHubConnectionString = ioTHubConnectionString;
        StorageConnectionString = storageConnectionString;
        CosmosConnectionString = cosmosConnectionString;
        SignalRConnectionString = signalRConnectionString;
        ApplicationInsightsKey = applicationInsightsKey;
    }
}
