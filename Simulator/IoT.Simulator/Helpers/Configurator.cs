namespace IoT.Simulator;

public static class Configurator
{
    /// <summary>
    /// Read configuration file based on first command line argument, or return help
    /// </summary>
    public static ProcessingParameters Read(string[] args)
    {
        Utilities.DisplayMessageInABox(Constants.ProgramName, ConsoleColor.Green);

        var configFileName = args.Length > 0 ? args[0] : string.Empty;
        if (configFileName.Contains("help", StringComparison.InvariantCultureIgnoreCase) || configFileName.Contains("?"))
        {
            DisplayHelp();
            return null;
        }
        var applicationDirectory = Utilities.GetApplicationDirectory();
        var fullConfigFileName = applicationDirectory + configFileName;

        if (!File.Exists(fullConfigFileName))
        {
            Utilities.DisplayClippyErrorMessage($"Are you sure that's right?  I couldn't find this config file!\n  {fullConfigFileName}");

            //Utilities.DisplayErrorMessage("No config file found...");
            //Utilities.DisplayErrorMessage("Searched for " + configFileName);
            //Utilities.DisplayErrorMessage($"  in directory: {applicationDirectory}");
            ////Utilities.DisplayErrorMessage($"  Assembly Directory: {AppContext.BaseDirectory}");
            ////Utilities.DisplayErrorMessage($"  Module: {Path.GetFileName(Environment.ProcessPath)}");
            DisplayHelp();
            return null;
        }
        var parms = new ProcessingParameters();
        Utilities.DisplayMessage("Reading config data from " + fullConfigFileName);
        var configRawData = File.ReadAllText(fullConfigFileName);
        var config = JsonConvert.DeserializeObject<Parameters>(configRawData);
        if (config != null)
        {
            parms.HomeDirectory = applicationDirectory;
            parms.EnvironmentCode = ValidateEnvironmentCode(config.EnvironmentCode);

            parms.DeviceIdSupplied = string.IsNullOrEmpty(config.DeviceId);
            parms.DeviceId = string.IsNullOrEmpty(config.DeviceId) ? Utilities.GetMacAddress() : config.DeviceId;

            parms.DeviceConnectionString = config.DeviceConnectionString;
            parms.PfxFileName = config.PfxFileName;
            parms.PfxFilePassword = config.PfxFilePassword;
            parms.DpsScopeId = ValidateDPSScopeId(config.DpsScopeId);
            parms.SymmetricKey = config.SymmetricKey;

            parms.IoTHubUri = config.IoTHubUri;
            if (!string.IsNullOrEmpty(parms.IoTHubUri) && !parms.IoTHubUri.Contains(".azure-devices.net", StringComparison.InvariantCultureIgnoreCase))
            {
                parms.IoTHubUri += ".azure-devices.net";
            }
            if (string.IsNullOrEmpty(parms.DeviceConnectionString) && !string.IsNullOrEmpty(parms.SymmetricKey) && !string.IsNullOrEmpty(parms.IoTHubUri))
            {
                parms.DeviceConnectionString = $"HostName={parms.IoTHubUri};DeviceId={parms.DeviceId};SharedAccessKey={parms.SymmetricKey}";
            }

            var authMethod1Found = !string.IsNullOrEmpty(parms.DeviceConnectionString) ? 1 : 0;
            var authMethod2Found = !string.IsNullOrEmpty(parms.IoTHubUri) && !string.IsNullOrEmpty(parms.SymmetricKey) ? 1 : 0;
            var authMethod3Found = !string.IsNullOrEmpty(parms.DpsScopeId) && !string.IsNullOrEmpty(parms.PfxFileName) ? 1 : 0;
            if (authMethod1Found + authMethod2Found + authMethod3Found > 1)
            {
                Utilities.DisplayMessage("\n" +
                    "  Note: For Device Authorization, a config file should supply ONE of the following: \n" +
                    "    1. DeviceConnectionString, or \n" +
                    "    2. IoTHubUri + SymmetricKey, or \n" +
                    "    3. DpsScopeId + PfxFileName + PfxFilePassword", ConsoleColor.Red);
            }

            parms.HeartbeatInterval = config.HeartbeatInterval;

            parms.TransportProtocol = config.TransportProtocol?.ToLower() switch
            {
                "mqtt" => TransportType.Mqtt,
                "mqtt_websocket_only" => TransportType.Mqtt_WebSocket_Only,
                "mqtt_tcp_only" => TransportType.Mqtt_Tcp_Only,
                "amqp" => TransportType.Amqp,
                "amqp_websocket_only" => TransportType.Amqp_WebSocket_Only,
                "amqp_tcp_only" => TransportType.Amqp_Tcp_Only,
                "http" => TransportType.Http1,
                _ => TransportType.Amqp_WebSocket_Only,
            };

            // put other parameter population here...
        }
        parms.DisplayConfigurationValues();
        
        Utilities.RemoveOldLogFile();

        return parms;
    }

    /// <summary>
    /// Validate/qualify environment code parameter
    /// </summary>
    public static string ValidateEnvironmentCode(string value)
    {
        var env = string.IsNullOrEmpty(value) ? Constants.Environments.Dev : value.ToUpper().Trim();
        if (env.Contains(Constants.Environments.QA))
        {
            return Constants.Environments.QA;
        }
        if (env.Contains(Constants.Environments.Prod))
        {
            return Constants.Environments.Prod;
        }
        return Constants.Environments.Dev;
    }

    /// <summary>
    /// Validate/qualify environment code parameter
    /// </summary>
    public static string ValidateDPSScopeId(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            value = value.ToLower().Trim();
            var r = new Regex("^[a-z0-9-]*$");
            var isValid = r.IsMatch(value);
            if (isValid)
            {
                return value;
            }
            Utilities.DisplayErrorMessage($"DPS Scope Id {value} is not valid! Value must is alphanumeric, lowercase, and may contain hyphens");
            return string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Display Help Information
    /// </summary>
    public static void DisplayHelp()
    {
        var color = ConsoleColor.Yellow;
        var codeColor = ConsoleColor.Blue;
        Utilities.DisplayMessage("\nThis program expects a command line parameter with a configuration file name, like this:", color);
        Utilities.DisplayMessage("  IoT.Simulator.exe config-dev.json", codeColor);
        Utilities.DisplayMessage("\nThe configuration file should contain JSON data similar to this: (not all are required)", color);
        Utilities.DisplayMessage("  Note: For Device Authorization, supply either: ", color);
        Utilities.DisplayMessage("    DeviceConnectionString, or IoTHubUri+SymmetricKey, or DpsScopeId+PfxFileName+PfxFilePassword", color);
        Utilities.DisplayMessage("\n  {", codeColor);
        Utilities.DisplayMessage("    \"EnvironmentCode\":         \"DEV\",", codeColor);
        Utilities.DisplayMessage("    \"DeviceId\":                \"<myDeviceId>\",", codeColor);
        Utilities.DisplayMessage("    \"HeartbeatInterval\":       60,", codeColor);
        Utilities.DisplayMessage("    \"DeviceConnectionString\":  \"HostName=<myIotHub>.azure-devices.net;DeviceId=<myDeviceId>;SharedAccessKey=<myKey>\",", codeColor);
        Utilities.DisplayMessage("    \"IotHubUri\":               \"<myIotHub>\",", codeColor);
        Utilities.DisplayMessage("    \"SymmetricKey\":            \"<myKey>\",", codeColor);
        Utilities.DisplayMessage("    \"DpsScopeId\":              \"<myDpsScopeId>\",", codeColor);
        Utilities.DisplayMessage("    \"PfxFileName\":             \"<myCertificateFileName>\",", codeColor);
        Utilities.DisplayMessage("    \"PfxFilePassword\":         \"<myCertificatePassword>\",", codeColor);
        Utilities.DisplayMessage("  }", codeColor);
    }
}