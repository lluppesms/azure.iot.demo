namespace IoT.Simulator;
public partial class Simulator
{
    /// <summary>
    /// Sets up device event handlers
    /// </summary>
    public void SetupEventListeners(IoTDevice device)
    {
        device.OnMessageReceived += Device_OnMessageReceived;
        device.OnCommandReceived += Device_OnCommandReceived;
        device.OnCommandReceivedWithProperties += Device_OnCommandReceivedWithProperties;
        device.OnError += Device_OnError;
        device.OnTwinChanged += Device_OnTwinChanged;
    }

    /// <summary>
    /// Removes device event handlers
    /// </summary>
    public void RemoveEventListeners(IoTDevice device)
    {
        device.OnMessageReceived -= Device_OnMessageReceived;
        device.OnCommandReceived -= Device_OnCommandReceived;
        device.OnCommandReceivedWithProperties -= Device_OnCommandReceivedWithProperties;
        device.OnError -= Device_OnError;
        device.OnTwinChanged -= Device_OnTwinChanged;
    }

    /// <summary>
    /// Handle a Device Message sent event
    /// </summary>
    protected async virtual void Device_OnMessageReceived(string msg)
    {
        Utilities.DisplayMessage($"  C2D Message was received by device: {msg}", ConsoleColor.Gray);
        await Execute_Log_Command(Constants.EventType.Message, "Message", $"C2D Message Received: {msg}");
    }

    /// <summary>
    /// Handle a Device Command sent event
    /// </summary>
    protected async virtual Task Device_OnCommandReceived(string commandName)
    {
        var processed = false;
        Utilities.DisplayMessageInABox($"Remote Command was received: {commandName}", ConsoleColor.Red);
        switch (commandName.ToUpper().Trim())
        {
            case Constants.Commands.Heartbeat:
                Utilities.DisplayConsoleOnlyMessage("\n    Executing Remote Heartbeat Command...", ConsoleColor.Green);
                await Execute_SendHeartbeat();
                processed = true;
                break;
            case Constants.Commands.Log:
                Utilities.DisplayConsoleOnlyMessage("\n      Executing Remote File Upload...", ConsoleColor.Green);
                await Execute_Upload_Log_File();
                processed = true;
                break;
            case Constants.Commands.IpAddress:
                Utilities.DisplayConsoleOnlyMessage("\n      Executing Remote Show IP Address Command...", ConsoleColor.Green);
                Execute_Show_IpAddress();
                processed = true;
                break;
            case Constants.Commands.PodBayDoors:
                Utilities.DisplayConsoleOnlyMessage("\n      Executing Remote Open Pod Bay Doors Command...", ConsoleColor.Green);
                Execute_OpenPodBayDoors();
                processed = true;
                break;
            case Constants.Commands.ReadTwin:
                Utilities.DisplayConsoleOnlyMessage("\n      Executing Remote Display Device Twin Command...", ConsoleColor.Green);
                await Execute_Read_Device_Twin();
                processed = true;
                break;
            case Constants.Commands.WriteTwin:
                Utilities.DisplayConsoleOnlyMessage("\n      Executing Remote Write to Device Twin Command...", ConsoleColor.Green);
                await Execute_Write_Device_Twin();
                processed = true;
                break;
            default:
                Utilities.DisplayConsoleOnlyMessage($"\n      Remote Command {commandName} was not recognized!", ConsoleColor.Red);
                processed = false;
                break;
        }
        await Execute_Log_Command(Constants.EventType.Command, commandName, $"C2D Remote Command {commandName} was received and was " + (processed ? string.Empty : "not ") + "processed!");
    }

    /// <summary>
    /// Handle a Device Command sent event
    /// </summary>
    protected async virtual Task Device_OnCommandReceivedWithProperties(string commandName, TwinCollection desiredProperties)
    {
        var processed = false;
        Utilities.DisplayMessage($"Remote Command was received: {commandName}", ConsoleColor.Gray);
        switch (commandName.ToUpper().Trim())
        {
            case Constants.Commands.FirmwareUpdate:
                Utilities.DisplayConsoleOnlyMessage("\n      Executing Remote Firmware Update Check...", ConsoleColor.Green);
                await Execute_Firmware_Update(desiredProperties);
                processed = true;
                break;
            default:
                Utilities.DisplayConsoleOnlyMessage($"\n      Remote Command {commandName} was not recognized!", ConsoleColor.Red);
                processed = false;
                break;
        }
        await Execute_Log_Command(Constants.EventType.Command, commandName, $"C2D Remote Command {commandName} was received and was " + (processed ? string.Empty : "not ") + "processed!");
    }

    /// <summary>
    /// Handle a Device Error event
    /// </summary>
    protected virtual void Device_OnError(Exception e)
    {
        Utilities.DisplayError(e);
    }

    /// <summary>
    /// Handle a Device Twin changed event
    /// </summary>
    protected async virtual Task Device_OnTwinChanged(TwinCollection desiredProperties)
    {
        await Task.FromResult(true);
        //if (desiredProperties.Contains("Firmware"))
        //{
        //    await FirmwareUpdateCheck(desiredProperties);
        //}
        Utilities.DisplayMessageInABox("Device Twin properties changed!", ConsoleColor.Red);
        Utilities.DisplayMessage($"\t{desiredProperties.ToJson()}", ConsoleColor.White);
        return;
    }
}
