using System.Text.Json.Serialization;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

public class Z2MDiscoveryBinaryExpose : Z2MDiscoveryExpose
{
    [JsonPropertyName("access")] public required int Access { get; init; }

    [JsonPropertyName("description")] public string? Description { get; init; }

    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("property")] public required string Property { get; init; }

    [JsonPropertyName("value_off")]
    [JsonConverter(typeof(Z2MDiscoveryBinaryExposeValueJsonConverter))]
    public object? ValueOff { get; init; }

    [JsonPropertyName("value_on")]
    [JsonConverter(typeof(Z2MDiscoveryBinaryExposeValueJsonConverter))]
    public object? ValueOn { get; init; }
}