using System.Text.Json;
using System.Text.Json.Serialization;
using MQTTnet;
using MQTTnet.Client;

namespace SmartHome.Infrastructure.Zigbee2Mqtt;

public record Z2MConfig(string Server, int Port);

public class Z2MLight
{
    private readonly Z2MConfig _config;
    private readonly string _friendlyName;

    private struct LightState
    {
        [JsonPropertyName("state")] public string State { get; init; }
    }

    private struct LightConfig
    {
        [JsonPropertyName("state")] public string State { get; init; }
        [JsonPropertyName("brightness")] public int Brightness { get; init; }
        [JsonPropertyName("color")] public LightColor Color { get; init; }
    }

    private struct LightColor
    {
        [JsonPropertyName("x")] public double X { get; init; }
        [JsonPropertyName("y")] public double Y { get; init; }
    }

    public Z2MLight(Z2MConfig config, string friendlyName)
    {
        _config = config;
        _friendlyName = friendlyName;
    }

    public async Task TurnOn()
    {
        await Publish(JsonSerializer.Serialize(new LightConfig()
        {
            Brightness = 128,
            Color = new LightColor()
            {
                X = 0.5524,
                Y = 0.4079,
            },
            State = "ON",
        }));
    }

    public async Task TurnOff()
    {
        await Publish(JsonSerializer.Serialize(new LightState
        {
            State = "OFF",
        }));
    }

    private async Task Publish(string payload)
    {
        var mqttFactory = new MqttFactory(); // TODO cache this somewhere + pass a custom logger?

        using var mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Server, _config.Port)
            .WithClientId($"{_friendlyName}-{Guid.NewGuid()}")
            .Build();

        var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        await mqttClient.PublishStringAsync($"zigbee2mqtt/{_friendlyName}/set", payload);
    }
}