namespace IoT.Processor.Models;

public class MyConfiguration
{
    /// <summary>
    /// Application Title
    /// </summary>
    public string ApplicationTitle { get; set; }

    /// <summary>
    /// Should the app write data to Cosmos?
    /// </summary>
    public string WriteToCosmosYN { get; set; }

    /// <summary>
    /// Should the app write data to SignalR?
    /// </summary>
    public string WriteToSignalRYN { get; set; }

    /// <summary>
    /// Should the app write data to Cosmos?
    /// </summary>
    public bool WriteToCosmos()
    {
        if (WriteToCosmosYN.Length > 1)
        {
            WriteToCosmosYN = WriteToCosmosYN[..1];
        }
        return string.IsNullOrEmpty(WriteToCosmosYN) || WriteToCosmosYN.Equals("Y", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Should the app write data to SignalR?
    /// </summary>
    public bool WriteToSignalR()
    {
        if (WriteToSignalRYN.Length > 1)
        {
            WriteToSignalRYN = WriteToSignalRYN[..1];
        }
        return string.IsNullOrEmpty(WriteToSignalRYN) || WriteToSignalRYN.Equals("Y", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Echo out values
    /// </summary>
    public void DisplayValues(string dataSource)
    {
        MyLogger.LogInfo($"MyConfig.ApplicationTitle: {ApplicationTitle}", dataSource);
    }
}
