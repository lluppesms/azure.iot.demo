namespace IoT.Processor.Models;

/// <summary>
/// Basic Input model
/// </summary>
public class BasicInput
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public Guid MessageId { get; set; }

    [JsonProperty("partitionKey", NullValueHandling = NullValueHandling.Ignore)]
    public string PartitionKey { get; set; }

    /// <summary>
    /// Device Id is required in most of the expected data packets
    /// </summary>
    [JsonProperty("deviceId")]
    public string DeviceId { get; set; }

    /// <summary>
    /// Alternate Event Time Stamp
    /// </summary>
    [JsonProperty("eventDateTime", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? EventDateTime { get; set; }

    /// <summary>
    /// Event Type used to determine how the message is processed
    /// </summary>    
    [JsonProperty("eventTypeCode", NullValueHandling = NullValueHandling.Ignore)]
    public string EventTypeCode { get; set; }

    /// <summary>
    /// Event SubType used to determine how the message is processed
    /// </summary>    
    [JsonProperty("eventSubTypeCode", NullValueHandling = NullValueHandling.Ignore)]
    public string EventSubTypeCode { get; set; }

    /// <summary>
    /// Event Type used to determine how the message is processed
    /// </summary>    
    [JsonProperty("eventType", NullValueHandling = NullValueHandling.Ignore)]
    public string EventType { get; set; }

    /// <summary>
    /// Event Time Stamp
    /// </summary>
    [JsonProperty("timeStamp", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? TimeStamp { get; set; }

    /// <summary>
    /// Alternate Event Time Stamp
    /// </summary>
    [JsonProperty("readingDateTime", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? ReadingDateTime { get; set; }

    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public string AdditionalData { get; set; }

    /// <summary>
    /// Is this a valid message?
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(DeviceId);
    }
    /// <summary>
    /// Cleans up mis-named data
    /// </summary>
    public void ScrubData(string rawMessageData)
    {
        if (EventDateTime == null)
        {
            if (TimeStamp != null)
            {
                EventDateTime = TimeStamp;
            }
            else
            {
                if (ReadingDateTime != null)
                {
                    EventDateTime = ReadingDateTime;
                }
            }
        }
        if (string.IsNullOrEmpty(EventTypeCode) && !string.IsNullOrEmpty(EventType))
        {
            EventTypeCode = EventType;
        }
        if (EventTypeCode.StartsWith(Constants.EventType.Storage))
        {
            EventTypeCode = Constants.EventType.Storage;
        }
        if (string.IsNullOrEmpty(DeviceId) && EventTypeCode == Constants.EventType.Storage)
        {
            var fileMsg = JsonConvert.DeserializeObject<StorageNotification>(rawMessageData);
            if (!string.IsNullOrEmpty(fileMsg.subject))
            {
                DeviceId = Utilities.DeriveDeviceIdFromBlobName(fileMsg.subject);
            }
        }
    }
}
