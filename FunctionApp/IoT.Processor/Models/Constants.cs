namespace IoT.Processor.Models;

public static class Constants
{
    /// <summary>
    /// Name of data/action source for use in logging
    /// </summary>
    public static class TriggerSource
    {
        public const string Unknown = "Unknown";
        public const string API = "API";
        public const string ServiceBusMessage = "SvcBusMessage";
        //public const string ServiceBusFileUpload = "SvcBusFile";
        public const string FileUpload = "FileUpload";
        public const string CosmosHelper = "Cosmos";
        public const string SignalRHelper = "SignalR";
    }

    /// <summary>
    /// Storage Constants
    /// </summary>
    public static class Storage
    {
        public const string FileUploadFolder = "iothubuploads";
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
    /// Event Types
    /// </summary>
    public static class EventType
    {
        public const string Register = "Register";
        public const string Heartbeat = "Heartbeat";
        public const string Firmware = "Firmware";
        public const string Unknown = "";
        public const string Storage = "Microsoft.Storage";
    }
}
