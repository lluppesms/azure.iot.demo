namespace IoT.Simulator;

/// <summary>
/// Firmware Update Simulator -- this logic will need to be replace with REAL firmware update logic
/// </summary>
public partial class Simulator
{
    /// <summary>
    /// Check to see if the device has the proper firmware version installed
    /// </summary>
    public async Task<bool> FirmwareUpdateCheck(TwinCollection properties = null)
    {
        Utilities.DisplayMessage($"        Retrieving Local Firmware Version...", ConsoleColor.Yellow);
        var localFirmwareVersion = GetLocalFirmwareVersion();
        Utilities.DisplayMessage($"          Local Firmware Version: {localFirmwareVersion}", ConsoleColor.Yellow);

        TwinCollection desired;
        TwinCollection reported;
        if (properties != null)
        {
            desired = properties;
        }
        else
        {
            (desired, reported) = device.ReadDeviceTwin(false).GetAwaiter().GetResult();
        }
        if (desired == null)
        {
            Utilities.DisplayErrorMessage("          Could not retrieve Device Twin - aborting firmware update!");
            return false;
        }

        //  use this version of code if it's just a single level attribute...
        //var desiredFirmwareVersion = device.ConvertDeviceTwinPropertyToString(desired, "FirmwareVersion", "1.0.0");
        //var desiredFirmwareDownloadUrl = device.ConvertDeviceTwinPropertyToString(desired, "FirmwareDownloadUrl", "");

        //  version of code if it's just a multi-level attribute...
        var desiredFirmwareVersion = string.Empty;
        var desiredFirmwareDownloadUrl = string.Empty;
        if (desired.Contains("Firmware"))
        {
            JObject desiredFirmwareConfig = desired["Firmware"];
            if (desiredFirmwareConfig != null)
            {
                desiredFirmwareVersion = desiredFirmwareConfig["Version"] != null ? (string)desiredFirmwareConfig["Version"] : string.Empty;
                desiredFirmwareDownloadUrl = desiredFirmwareConfig["DownloadUrl"] != null ? (string)desiredFirmwareConfig["DownloadUrl"] : string.Empty;
            }
        }
        if (string.IsNullOrEmpty(desiredFirmwareVersion) || string.IsNullOrEmpty(desiredFirmwareDownloadUrl))
        {
            Utilities.DisplayErrorMessage("          There is no desired Device Twin Firmware setting - aborting firmware update!");
            Utilities.DisplayErrorMessage("          Expecting a Device Twin property that looks like this: ");
            Utilities.DisplayErrorMessage("          \"Firmware\": {");
            Utilities.DisplayErrorMessage("            \"Version\": \"1.0.1\",");
            Utilities.DisplayErrorMessage("            \"DownloadUrl\": \"http://somedownloadpath...\"");
            Utilities.DisplayErrorMessage("          }");
            return false;
        }
        Utilities.DisplayMessage($"          Desired Firmware Version: {desiredFirmwareVersion}", ConsoleColor.Yellow);
        if (localFirmwareVersion != desiredFirmwareVersion)
        {
            Utilities.DisplayMessage($"        Cloud expects device to be at version {desiredFirmwareVersion} and it is not.", ConsoleColor.Blue);
            Utilities.DisplayMessage($"          Downloading from: {desiredFirmwareDownloadUrl}", ConsoleColor.Blue);
            if (await DownloadNewFirmwareVersion(desiredFirmwareVersion, desiredFirmwareDownloadUrl))
            {
                Utilities.DisplayMessage($"        Firmware Version: {desiredFirmwareVersion} has been applied!", ConsoleColor.Blue);
            }
            else
            {
                Utilities.DisplayMessage($"        Failure Downloading Firmware Version: {desiredFirmwareVersion}!", ConsoleColor.Red);
            }
        }
        else
        {
            Utilities.DisplayMessage($"        Firmware Version is already set to {desiredFirmwareVersion}!", ConsoleColor.Blue);
        }
        return true;
    }

    /// <summary>
    /// Gets the current firmware version from the local device
    /// </summary>
    private static string GetLocalFirmwareVersion()
    {
        // return contents of local firmware file if it exists
        var firmwareFilePath = Utilities.GetApplicationDirectory() + Constants.FirmwareFileName;
        if (File.Exists(firmwareFilePath))
        {
            return File.ReadAllText(firmwareFilePath);
        }
        // if local firmware file doesn't exist, then create one...
        Utilities.DisplayMessage($"          Installing initial firmware version...", ConsoleColor.Green);
        var firmwareVersion = "1.0.0";
        File.WriteAllText(firmwareFilePath, firmwareVersion);
        return firmwareVersion;
    }

    /// <summary>
    /// Downloads the desired firmware version from the cloud
    /// </summary>
    private async Task<bool> DownloadNewFirmwareVersion(string firmwareVersion, string desiredFirmwareDownloadUrl)
    {
        //Future Implementation: download the firmware from the server and apply...
        Utilities.DisplayMessage($"            Download not implemented yet!", ConsoleColor.Red);

        //For now: just write the version number to the file...
        await Task.FromResult(true);
        var firmwareFilePath = Utilities.GetApplicationDirectory() + Constants.FirmwareFileName;
        File.WriteAllText(firmwareFilePath, firmwareVersion);
        return true;
    }
}
