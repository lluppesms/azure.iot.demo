namespace IoT.Simulator.Models;

public class DeviceData
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("ipAddress")]
    public string IpAddress { get; set; }
}
