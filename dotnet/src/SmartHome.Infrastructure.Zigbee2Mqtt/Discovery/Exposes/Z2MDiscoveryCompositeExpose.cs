using System.Text.Json.Serialization;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery.Exposes;

public class Z2MDiscoveryCompositeExpose : Z2MDiscoveryExpose
{
    [JsonPropertyName("access")] public required int Access { get; init; }

    [JsonPropertyName("description")] public string? Description { get; init; }

    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("property")] public required string Property { get; init; }

    [JsonPropertyName("features")] public required Z2MDiscoveryExpose[] Features { get; set; }
}