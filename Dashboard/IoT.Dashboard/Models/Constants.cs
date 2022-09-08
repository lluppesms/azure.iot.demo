namespace IoT.Dashboard.Models;
public class Constants
{
    public class DashboardTabs
    {
        public const string State = "deviceState";
		public const string Commands = "deviceCommands";
		public const string Data = "deviceData";
		public const string Files = "deviceFiles";
    }
    public class DeviceCommands
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

    /// <summary>
    /// Cosmos Database Constants
    /// </summary>
    public static class Cosmos
    {
        public const string DatabaseName = "IoTDatabase";
        public const string DataContainerName = "DeviceData";
        public const string DeviceContainerName = "DeviceList";
    }

    /// <summary>
    /// Blob Storage Constants
    /// </summary>
    public static class BlobStorage
    {
        public const string FileUploads = "iothubuploads"; // "iotfileuploads";
    }

    /// <summary>
    /// Local Storage Constants
    /// </summary>
    public static class LocalStorage
    {
        public const string SessionObject = "Session";
    }

    /// <summary>
    /// Logging Source
    /// </summary>
    public static class Source
    {
        public const string CosmosHelper = "CosmosHelper";
        public const string StorageHelper = "StorageHelper";
    }
}
