namespace IoT.Processor.Models;

/// <summary>
/// Device Heartbeat
/// </summary>
public class Heartbeat
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

    [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
    public float? Temperature { get; set; }

    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public string AdditionalData { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Heartbeat()
    {
        EventDateTime = DateTime.Now;
        DeviceId = string.Empty;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Heartbeat;
        Temperature = float.MinValue;
        AdditionalData = string.Empty;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Heartbeat(string deviceId)
    {
        EventDateTime = DateTime.Now;
        DeviceId = deviceId;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Heartbeat;
        var seed = Utilities.ReturnOnlyNumbers(MessageId.ToString());
        var random = new Random(seed);
        var randomNumber = random.Next(20) + 50;
        var confidence = random.Next(30) + 70;
        Temperature = randomNumber;
        AdditionalData = "{" + $"'temperature:' {Temperature}, 'confidence:' {confidence} " + "}";
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Heartbeat(string deviceId, DateTime specifiedTime)
    {
        EventDateTime = specifiedTime;
        DeviceId = deviceId;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Heartbeat;
        var seed = Utilities.ReturnOnlyNumbers(MessageId.ToString());
        var random = new Random(seed);
        var randomNumber = random.Next(20) + 50;
        var confidence = random.Next(30) + 70;
        Temperature = randomNumber;
        AdditionalData = "{" + $"'temperature:' {Temperature}, 'confidence:' {confidence} " + "}";
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Heartbeat(string deviceId, float reading)
    {
        EventDateTime = DateTime.Now;
        DeviceId = deviceId;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Heartbeat;
        Temperature = reading;
        AdditionalData = "{" + $"'temperature:' {Temperature} " + "}";
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Heartbeat(string deviceId, float reading, float confidence)
    {
        EventDateTime = DateTime.Now;
        DeviceId = deviceId;
        MessageId = Guid.NewGuid();
        EventTypeCode = Constants.EventType.Heartbeat;
        Temperature = reading;
        AdditionalData = "{" + $"\"temperature:\" {Temperature}, \"confidence:\" {confidence} " + "}";
    }

    /// <summary>
    /// Is this a valid message?
    /// </summary>
    public bool IsValid()
    {
        if (Temperature == float.MinValue) { Temperature = null; }
        return !string.IsNullOrEmpty(DeviceId) && ((Temperature != null && Temperature != float.MinValue) || !string.IsNullOrEmpty(AdditionalData));
    }
}
