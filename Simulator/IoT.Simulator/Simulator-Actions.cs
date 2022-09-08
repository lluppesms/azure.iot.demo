using ICSharpCode.SharpZipLib.Zip;

namespace IoT.Simulator;

public partial class Simulator
{
    /// <summary>
    /// Send Heartbeat to Cloud
    /// </summary>
    private async Task<bool> Execute_SendRegistration()
    {
        Utilities.DisplayMessage($"\n      Sending Device Registration...", ConsoleColor.Yellow);
        var registration = JsonConvert.SerializeObject(new Register(device.DeviceId));
        Utilities.DisplayMessage($"        {registration}", ConsoleColor.Blue);
        var registrationSent = await device.SendMessage(registration);
        var twinUpdated = (await device.WriteDeviceTwinProperty("DateTimeLastAppLaunch", DateTime.UtcNow, false) != null);
        return (registrationSent && twinUpdated);
    }

    /// <summary>
    /// Send Heartbeat to Cloud
    /// </summary>
    private async Task<bool> Execute_SendHeartbeat()
    {
        Utilities.DisplayMessage($"\n      Sending Heartbeat...", ConsoleColor.Yellow);
        var heartbeat = JsonConvert.SerializeObject(new Heartbeat(device.DeviceId));
        Utilities.DisplayMessage($"        {heartbeat}", ConsoleColor.Blue);
        return await device.SendMessage(heartbeat);
    }

    /// <summary>
    /// Send Log record to Cloud
    /// </summary>
    private async Task<bool> Execute_Log_Command(string eventTypeCode, string eventTypeSubCode, string data)
    {
        Utilities.DisplayMessage($"\n      Sending Event Log entry...", ConsoleColor.Yellow);
        var logEntry = JsonConvert.SerializeObject(new LogData(device.DeviceId, eventTypeCode, eventTypeSubCode, data));
        Utilities.DisplayMessage($"        {logEntry}", ConsoleColor.Blue);
        return await device.SendMessage(logEntry);
    }

    /// <summary>
    /// Send Batch of Heartbeats to Cloud
    /// </summary>
    private async Task<bool> Execute_SendHeartbeatBatch(bool zipped = false)
    {
        var heartBeatCount = 6;
        var heartBeatTime = DateTime.Now.AddSeconds(-60);
        var heartbeatFileName = Utilities.DateifyFileName(Constants.HeartbeatFileName);
        var heartbeatFilePath = Utilities.GetApplicationDirectory() + heartbeatFileName;
        var sb = new StringBuilder();
        var zipMsg = zipped ? "zipped " : string.Empty;
        
        Utilities.DisplayMessage($"\n      Sending {zipMsg}Batch of {heartBeatCount} Heartbeats...", ConsoleColor.Yellow);
        for (int i = 0; i < heartBeatCount; i++)
        {
            heartBeatTime = heartBeatTime.AddSeconds(10);
            sb.AppendLine(JsonConvert.SerializeObject(new Heartbeat(device.DeviceId, heartBeatTime)));
        }
        var heartbeats = sb.ToString();
        
        if (zipped)
        {
            heartbeatFilePath = heartbeatFilePath.Replace(".json", ".zip");
            using var fs = File.Create(heartbeatFilePath);
            using var outStream = new ZipOutputStream(fs);
            outStream.PutNextEntry(new ZipEntry(heartbeatFileName));
            using var sw = new StreamWriter(outStream);
            sw.Write(heartbeats);
        }
        else
        {
            File.WriteAllText(heartbeatFilePath, heartbeats);
        }
        Utilities.DisplayMessage($"      Upload {zipMsg}heartbeat file to cloud...");
        var uploaded = await device.UploadFile(heartbeatFilePath);
        File.Delete(heartbeatFilePath);
        return uploaded;
    }

    /// <summary>
    /// Upload Log File to Cloud
    /// </summary>
    private async Task<bool> Execute_Upload_Log_File()
    {
        Utilities.DisplayMessage("      Upload log file to cloud...");
        var logFilePath = Utilities.GetApplicationDirectory() + Constants.GetLogFileName();
        return await device.UploadFile(logFilePath);
    }
    
    /// <summary>
    /// Perform Firmware Update Check
    /// </summary>
    private async Task<bool> Execute_Firmware_Update(TwinCollection desiredProperties = null)
    {
        Utilities.DisplayMessage("      Checking Firmware Status...");
        return await FirmwareUpdateCheck(desiredProperties);
    }

    /// <summary>
    /// Display Device Twin Properties
    /// </summary>
    private async Task<(TwinCollection desired, TwinCollection reported)> Execute_Read_Device_Twin(bool displayValues = true)
    {
        return await device.ReadDeviceTwin(displayValues);
    }

    /// <summary>
    /// Write value to Device Twin
    /// </summary>
    private async Task<Twin> Execute_Write_Device_Twin(bool displayValues = true)
    {
        Utilities.DisplayMessage("      Setting DateTimeLastAppLaunch on Device Twin properties...", ConsoleColor.Yellow);
        return await device.WriteDeviceTwinProperty("DateTimeLastAppLaunch", DateTime.UtcNow, true);
    }

    /// <summary>
    /// Show the current IP Address
    /// </summary>
    private void Execute_Show_IpAddress()
    {
        var ip = Utilities.GetIPAddress();
        Utilities.DisplayMessage($"      IP Address: {ip}", ConsoleColor.Yellow);
        return;
    }

    /// <summary>
    /// Open the Pod Bay Doors
    /// </summary>
    private void Execute_OpenPodBayDoors()
    {
        Utilities.DisplayMessage($"        Open the pod bay doors, Hal.", ConsoleColor.Blue);
        Utilities.DisplayMessage($"            I’m sorry, Dave. I’m afraid I can’t do that.", ConsoleColor.Yellow);
        Utilities.DisplayMessage($"        What’s the problem?", ConsoleColor.Blue);
        Utilities.DisplayMessage($"            I think you know what the problem is just as well as I do.", ConsoleColor.Yellow);
        return;
    }
}
