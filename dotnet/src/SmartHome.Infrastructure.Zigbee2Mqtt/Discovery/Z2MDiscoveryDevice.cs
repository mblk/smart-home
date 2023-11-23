using System.Text.Json.Serialization;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

// ReSharper disable once ClassNeverInstantiated.Global
public class Z2MDiscoveryDevice
{
    [JsonPropertyName("disabled")] public required bool Disabled { get; init; }

    [JsonPropertyName("friendly_name")] public required string FriendlyName { get; init; }

    [JsonPropertyName("ieee_address")] public required string IeeeAddress { get; init; }

    [JsonPropertyName("type")] public required string Type { get; init; }

    [JsonPropertyName("definition")] public Z2MDiscoveryDefinition? Definition { get; init; }
}