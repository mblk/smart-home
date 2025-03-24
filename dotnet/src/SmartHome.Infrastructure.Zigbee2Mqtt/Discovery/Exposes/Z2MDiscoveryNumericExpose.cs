using System.Text.Json.Serialization;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery.Exposes;

public class Z2MDiscoveryNumericExpose : Z2MDiscoveryExpose
{
    [JsonPropertyName("access")] public required int Access { get; init; }

    [JsonPropertyName("description")] public string? Description { get; init; }

    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("property")] public required string Property { get; init; }

    [JsonPropertyName("value_min")] public int? ValueMin { get; init; }

    [JsonPropertyName("value_max")] public int? ValueMax { get; init; }
}