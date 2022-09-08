namespace IoT.Simulator;

public partial class Simulator
{
    /// <summary>
    /// The IoT Device Class
    /// </summary>
    public IoTDevice device;

    /// <summary>
    /// The next heartbeat time
    /// </summary>
    private DateTime NextHeartbeatTime = DateTime.Now.AddSeconds(-15);

    /// <summary>
    /// Time Between Heartbeats
    /// </summary>
    private int HeartbeatInterval;

    public async void Run(ProcessingParameters parms)
    {
        device = new IoTDevice();
        SetupEventListeners(device);
        
        if (!await device.CreateCloudConnection(parms)) return;
        await Execute_SendRegistration();
        await PromptUserForActions(parms);
        
        RemoveEventListeners(device);
        await device.CloseDeviceClientConnection();
    }
}