namespace IoT.Dashboard.Models;
public class DeviceData
{
    [JsonProperty("rowId", NullValueHandling = NullValueHandling.Ignore)]
    public int RowId { get; set; }

    [JsonProperty("deviceId", NullValueHandling = NullValueHandling.Ignore)]
    public string DeviceId { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public Guid MessageId { get; set; }

    [JsonProperty("partitionKey", NullValueHandling = NullValueHandling.Ignore)]
    public string PartitionKey { get; set; }

    [JsonProperty("eventDateTime", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? EventDateTime { get; set; }

    [JsonProperty("eventTypeCode", NullValueHandling = NullValueHandling.Ignore)]
    public string EventTypeCode { get; set; }
    
    [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
    public float? Temperature { get; set; }

    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public string AdditionalData { get; set; }
}