namespace IoT.Processor.Helpers;

/// <summary>
/// Encapsulates all of the SignalR related plumbing
/// </summary>
public class SignalRHelper
{
    /// <summary>
    /// Configuration
    /// </summary>
    private MyConfiguration config;

    /// <summary>
    /// Secrets
    /// </summary>
    private MySecrets secrets;

    /// <summary>
    /// Constructor
    /// </summary>
    public SignalRHelper(MyConfiguration diConfig, MySecrets diSecrets)
    {
        config = diConfig;
        secrets = diSecrets;
    }

    /// <summary>
    /// Send to SignalR
    /// </summary>
    public async Task<(bool, string)> SendToSignalR(string deviceId, Guid messageId, dynamic data, string dataSource, StringBuilder sb)
    {
        var success = await Task.FromResult(true);
        MyLogger.LogWarning($"TODO: Create SignalR code to write message {messageId} for device {deviceId} to SignalR!", dataSource, deviceId, sb);
        return (success, "NOT IMPLEMENTED!");
    }
}
