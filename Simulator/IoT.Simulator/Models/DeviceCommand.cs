namespace IoT.Simulator.Models;

public class DeviceCommand
{
    [JsonProperty("command")]
    public string Command { get; set; }
    
    [JsonProperty("Data")]
    public string AdditionalData { get; set; }
}
