namespace IoT.Simulator;

public class Parameters
{
    /// <summary>
    /// Environment: DEV/QA/PROD
    /// </summary>
    public string EnvironmentCode { get; set; }

    /// <summary>
    /// Specified Device ID (uses MAC address if not specified)
    /// </summary>
    public string DeviceId { get; set; }

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
    public string TransportProtocol { get; set; }

    /// <summary>
    /// If this interval is specified, the simulator will send heartbeats at this interval (in seconds) 
    /// </summary>
    public int HeartbeatInterval { get; set; }
}
