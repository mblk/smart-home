using System.Text.Json.Serialization;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

public class Z2MDiscoveryLightExpose : Z2MDiscoveryExpose
{
    [JsonPropertyName("features")] public required Z2MDiscoveryExpose[] Features { get; set; }
}