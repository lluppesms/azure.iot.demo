namespace IoT.Simulator;

public class ProcessingParameters
{
    /// <summary>
    /// Environment: DEV/QA/PROD
    /// </summary>
    public string EnvironmentCode { get; set; }

    /// <summary>
    /// Calculated value that tells the application home directory
    /// </summary>
    public string HomeDirectory { get; set; }

    /// <summary>
    /// Device ID (Specified or MAC address if not specified)
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// Was the Device ID supplied in the config file?
    /// </summary>
    public bool DeviceIdSupplied { get; set; }

    /// <summary>
    /// Device's IoT Hub connection string
    /// </summary>
    public string DeviceConnectionString { get; set; }

    /// <summary>
    /// Name of a certificate file to use for authorization
    /// </summary>
    public string PfxFileName { get; set; }

    /// <summary>
    /// Password for the certificate file
    /// </summary>
    public string PfxFilePassword { get; set; }

    /// <summary>
    /// Scope Id for the Device Provisioning Service
    /// </summary>
    public string DpsScopeId { get; set; }

    /// <summary>
    /// Symmetric Key (used with IoTHubUri and DeviceId to create an IoT Connection String)
    /// </summary>
    public string SymmetricKey { get; set; }

    /// <summary>
    /// Name of IoT Hub to connect to
    /// </summary>
    public string IoTHubUri { get; set; }

    /// <summary>
    /// Transport protocol to use with the IoT hub. Possible values include: Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, and Amqp_Tcp_Only.
    /// </summary>
    public TransportType TransportProtocol { get; set; }

    /// <summary>
    /// If this interval is specified, the simulator will send heartbeats at this interval (in seconds) 
    /// </summary>
    public int HeartbeatInterval { get; set; }

    /// <summary>
    /// Initializer
    /// </summary>
    public ProcessingParameters()
    {
        EnvironmentCode = Constants.Environments.Dev;
        HomeDirectory = String.Empty;
        DeviceId = String.Empty;
    }

    /// <summary>
    /// Displays the current values to the user
    /// </summary>
    public void DisplayConfigurationValues()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\nConfiguration:");
        sb.AppendLine($"  Environment:             {EnvironmentCode}");
        sb.AppendLine($"  Directory:               {HomeDirectory}");
        sb.AppendLine($"  DeviceId:                {DeviceId}");
        if (!string.IsNullOrEmpty(PfxFileName))
        {
            sb.AppendLine($"  PfxFileName:             {PfxFileName}");
            sb.AppendLine($"  PfxFilePassword:         ******");
        }
        if (!string.IsNullOrEmpty(DpsScopeId))
        {
            sb.AppendLine($"  DpsScopeId:              {DpsScopeId}");
        }
        if (!string.IsNullOrEmpty(IoTHubUri))
        {
            sb.AppendLine($"  IoTHubName:              {IoTHubUri}");
        }
        if (!string.IsNullOrEmpty(DeviceConnectionString))
        {
            sb.AppendLine($"  DeviceConnectionString:  {Utilities.GetSanitizedConnectionString(DeviceConnectionString)}");
        }
        if (!string.IsNullOrEmpty(SymmetricKey)) 
        {
            var displayKey = SymmetricKey.Length > 3 ? SymmetricKey[..3] + "*****" : SymmetricKey;
            sb.AppendLine($"  SymmetricKey:            {displayKey}");
        }
        sb.AppendLine($"  TransportProtocol:       {TransportProtocol}");
        sb.AppendLine(HeartbeatInterval > 0 ? $"  Heartbeat Every:         {HeartbeatInterval} seconds" : "  Heartbeat:               Upon request");

        sb.AppendLine($"  Log File:                {Constants.GetLogFileName()}");
        sb.AppendLine(string.Empty);
        Utilities.DisplayMessage(sb.ToString(), ConsoleColor.Magenta);
    }
}
