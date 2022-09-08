namespace IoT.Processor.Models;

/// <summary>
/// Device Initial Registration
/// </summary>
public class Register
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public Guid MessageId { get; set; }

    [JsonProperty("partitionKey", NullValueHandling = NullValueHandling.Ignore)]
    public string PartitionKey { get; set; }

    [JsonProperty("deviceId", NullValueHandling = NullValueHandling.Ignore)]
    public string DeviceId { get; set; }

    [JsonProperty("eventDateTime", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? EventDateTime { get; set; }

    [JsonProperty("eventTypeCode", NullValueHandling = NullValueHandling.Ignore)]
    public string EventTypeCode { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Register()
    {
        EventDateTime = DateTime.Now;
        DeviceId = string.Empty;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Register;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Register(string deviceId)
    {
        EventDateTime = DateTime.Now;
        DeviceId = deviceId;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Register;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Register(string deviceId, DateTime specifiedTime)
    {
        EventDateTime = specifiedTime;
        DeviceId = deviceId;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Register;
    }

    /// <summary>
    /// Is this a valid message?
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(DeviceId);
    }
}
