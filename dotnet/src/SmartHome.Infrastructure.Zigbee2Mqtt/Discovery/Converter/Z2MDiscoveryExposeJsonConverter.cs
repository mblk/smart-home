using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery.Exposes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery.Converter;

public class Z2MDiscoveryExposeJsonConverter : JsonConverter<Z2MDiscoveryExpose>
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsAssignableFrom(typeof(Z2MDiscoveryExpose));
    }

    public override Z2MDiscoveryExpose? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        if (!doc.RootElement.TryGetProperty("type", out var type))
            throw new JsonException("Type discriminator not found");

        var typeValue = type.GetString();
        var rootElement = doc.RootElement.GetRawText();

        //Console.WriteLine($"type={typeValue}");

        return typeValue switch
        {
            "binary" => JsonSerializer.Deserialize<Z2MDiscoveryBinaryExpose>(rootElement, options),
            "numeric" => JsonSerializer.Deserialize<Z2MDiscoveryNumericExpose>(rootElement, options),
            "composite" => JsonSerializer.Deserialize<Z2MDiscoveryCompositeExpose>(rootElement, options),
            "light" => JsonSerializer.Deserialize<Z2MDiscoveryLightExpose>(rootElement, options),
            _ => JsonSerializer.Deserialize<Z2MDiscoveryUnknownExpose>(rootElement, options)
        };
    }

    public override void Write(Utf8JsonWriter writer, Z2MDiscoveryExpose value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}