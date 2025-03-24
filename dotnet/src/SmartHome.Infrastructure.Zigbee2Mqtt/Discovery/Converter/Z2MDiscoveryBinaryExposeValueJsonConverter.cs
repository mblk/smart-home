using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery.Converter;

public class Z2MDiscoveryBinaryExposeValueJsonConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var raw = doc.RootElement.GetRawText();
        raw = raw.Trim('"', '\'');
        
        // Possible values:
        // true
        // false
        // "ON"
        // "OFF"
        // ...

        if (Boolean.TryParse(raw, out var boolValue))
            return boolValue;

        return raw;
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}