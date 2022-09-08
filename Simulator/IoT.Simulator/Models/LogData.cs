namespace IoT.Simulator.Models;

/// <summary>
/// Device LogData
/// </summary>
public class LogData
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

    [JsonProperty("eventTypeSubCode", NullValueHandling = NullValueHandling.Ignore)]
    public string EventTypeSubCode { get; set; }

    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public string AdditionalData { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public LogData()
    {
        EventDateTime = DateTime.Now;
        DeviceId = string.Empty;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Log;
        AdditionalData = string.Empty;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public LogData(string deviceId, string eventTypeSubCode, string data)
    {
        EventDateTime = DateTime.Now;
        DeviceId = deviceId;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Log;
        EventTypeSubCode = eventTypeSubCode;
        AdditionalData = data;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public LogData(string deviceId, string eventTypeCode, string eventTypeSubCode, string data)
    {
        EventDateTime = DateTime.Now;
        DeviceId = deviceId;
        MessageId = Guid.NewGuid();
        EventTypeCode = eventTypeCode;
        EventTypeSubCode = eventTypeSubCode;
        AdditionalData = data;
    }
}
