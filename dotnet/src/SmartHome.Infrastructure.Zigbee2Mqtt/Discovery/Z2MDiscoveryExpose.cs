using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery.Converter;
using System.Text.Json.Serialization;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

[JsonConverter(typeof(Z2MDiscoveryExposeJsonConverter))]
public abstract class Z2MDiscoveryExpose
{
    [JsonPropertyName("type")] public required string Type { get; init; }
}