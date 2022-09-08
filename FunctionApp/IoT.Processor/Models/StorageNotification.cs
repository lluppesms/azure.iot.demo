namespace IoT.Processor.Models;

/// <summary>
/// Storage File Created Notification
/// </summary>
public class StorageNotification
{
    /// <summary>
    /// Topic
    /// </summary>
    public string topic { get; set; }

    /// <summary>
    /// Subject
    /// </summary>
    public string subject { get; set; }

    /// <summary>
    /// Event Type
    /// </summary>
    public string eventType { get; set; }

    /// <summary>
    /// Event Time
    /// </summary>
    public DateTime eventTime { get; set; }

    /// <summary>
    /// Event Id
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// Metadata 
    /// </summary>
    public BlobFileData data { get; set; }

    public StorageNotification()
    {
        topic = string.Empty;
        subject = string.Empty;
        data = new BlobFileData();
        id = Guid.NewGuid().ToString();
    }
    public StorageNotification(string fileName, string deviceId)
    {
        topic = $"/subscriptions/Guid.NewGuid().ToString()/resourceGroups/rg_iot/providers/Microsoft.Storage/storageAccounts/iothubstorage1";
        subject = $"/blobServices/default/containers/{Constants.Storage.FileUploadFolder}/blobs/{deviceId}/{fileName}";
        data = new BlobFileData();
        eventType = "Microsoft.Storage.BlobCreated";
        id = Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Blob File Data
/// </summary>
public class BlobFileData
{
    /// <summary>
    /// Length
    /// </summary>
    public int contentLength { get; set; }

    /// <summary>
    /// URL
    /// </summary>
    public string url { get; set; }
}

