using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

public class Z2MDiscovery
{
    private const string DevicesTopic = "zigbee2mqtt/bridge/devices";

    private readonly Z2MConfig _config;

    public Z2MDiscovery(Z2MConfig config)
    {
        _config = config;
    }

    public async Task<Z2MDiscoveryDevice[]> DiscoverDevices(CancellationToken cancellationToken = default)
    {
        var devicesReceived = false;
        string? devicesData = null;

        var mqttFactory = new MqttFactory(); // TODO cache this somewhere + pass a custom logger?
        using var mqttClient = mqttFactory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == DevicesTopic)
            {
                var payloadBytes = e.ApplicationMessage.PayloadSegment.Array;
                var payloadString = Encoding.UTF8.GetString(payloadBytes ?? Array.Empty<byte>());

                devicesReceived = true;
                devicesData = payloadString;
            }

            return Task.CompletedTask;
        };

        // Connect
        var connectOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Server, _config.Port)
            .WithClientId($"discovery-{Guid.NewGuid()}")
            .Build();

        _ = await mqttClient.ConnectAsync(connectOptions, cancellationToken);

        // Subscribe
        var subOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => { f.WithTopic(DevicesTopic); }).Build();

        _ = await mqttClient.SubscribeAsync(subOptions, cancellationToken);

        // Wait for data
        while (!devicesReceived)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        // Disconnect
        var disconnectOptions = new MqttClientDisconnectOptionsBuilder()
            .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
            .Build();

        await mqttClient.DisconnectAsync(disconnectOptions, cancellationToken);

        // Parse data
        if (String.IsNullOrWhiteSpace(devicesData))
            throw new Exception("Did not receive any device data");

        var devices = JsonSerializer.Deserialize<Z2MDiscoveryDevice[]>(devicesData)
                      ?? throw new JsonException("Failed to deserialize device data");

        return devices;
    }
}