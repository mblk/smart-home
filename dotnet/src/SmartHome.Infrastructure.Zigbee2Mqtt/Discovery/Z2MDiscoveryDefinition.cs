using System.Text.Json.Serialization;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

// ReSharper disable once ClassNeverInstantiated.Global
public class Z2MDiscoveryDefinition
{
    [JsonPropertyName("description")] public string? Description { get; init; }

    [JsonPropertyName("exposes")] public required Z2MDiscoveryExpose[] Exposes { get; init; }
}