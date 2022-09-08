namespace IoT.Simulator;

public class Constants
{
    /// <summary>
    /// The name of the program
    /// </summary>
    public const string ProgramName = "IoT Device Simulator";
    
    /// <summary>
    /// Log File Name
    /// </summary>
    public const string LogFileName = "IoT-Simulator.log";

    /// <summary>
    /// Heartbeat File Name
    /// </summary>
    public const string HeartbeatFileName = "Heartbeats.json";

    /// <summary>
    /// Get Datified Log File Name
    /// </summary>
    public static string GetLogFileName()
    {
        return Constants.LogFileName.Replace(".log", $"{DateTime.Now.ToString("-yyyy-MM-dd", CultureInfo.InvariantCulture)}.log");
    }

    /// <summary>
    /// Firmware File Name
    /// </summary>
    public const string FirmwareFileName = "Firmware.txt";

    /// <summary>
    /// Global endpoint for Azure Device Provisioning Service
    /// </summary>
    public const string GlobalDPSEndpoint = "global.azure-devices-provisioning.net";

    /// <summary>
    /// Environments
    /// </summary>
    public class Environments
    {
        /// <summary>
        /// Development Environment
        /// </summary>
        public const string Dev = "DEV";

        /// <summary>
        /// QA Environment
        /// </summary>
        public const string QA = "QA";

        /// <summary>
        /// Production Environment
        /// </summary>
        public const string Prod = "PROD";
    }

    /// <summary>
    /// Event Types
    /// </summary>
    public static class EventType
    {
        public const string Register = "Register";
        public const string Heartbeat = "Heartbeat";
        public const string Firmware = "Firmware";
        public const string Command = "Command";
        public const string Method = "Method";
        public const string Message = "Message";
        public const string Log = "Log";
        public const string Unknown = "";
    }

    /// <summary>
    /// Commands that can be processed by this simulator
    /// </summary>
    public class Commands
    {
        /// <summary>
        /// Send a device heartbeat
        /// </summary>
        public const string Heartbeat = "HEARTBEAT";

        /// <summary>
        /// Upload a log file to the cloud
        /// </summary>
        public const string Log = "LOG";

        /// <summary>
        /// Download and apply a firmware update from the cloud
        /// </summary>
        public const string FirmwareUpdate = "FIRMWAREUPDATE";

        /// <summary>
        /// Display the current IP Address
        /// </summary>
        public const string IpAddress = "IPADDRESS";

        /// <summary>
        /// Read the Cloud Device Twin
        /// </summary>
        public const string ReadTwin = "READTWIN";

        /// <summary>
        /// Write to the Cloud Device Twin
        /// </summary>
        public const string WriteTwin = "WRITETWIN";

        /// <summary>
        /// Open the Pod Bay Doors, Hal
        /// </summary>
        public const string PodBayDoors = "PODBAYDOORS";
    }
}
