using System.Text;
using MQTTnet;
using MQTTnet.Client;
using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public abstract class Z2MDevice
{
    private readonly Z2MConfig _config;
    private readonly Z2MDiscoveryDevice _device;

    protected Z2MDiscoveryDevice Device => _device;

    protected Z2MDevice(Z2MConfig config, Z2MDiscoveryDevice device)
    {
        _config = config;
        _device = device;
    }

    protected async Task Publish(string payload)
    {
        var mqttFactory = new MqttFactory(); // TODO cache this somewhere + pass a custom logger?

        using var mqttClient = mqttFactory.CreateMqttClient();

        // Connect
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Server, _config.Port)
            .WithClientId($"{_device.FriendlyName}-{Guid.NewGuid()}")
            .Build();

        _ = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        // Publish
        await mqttClient.PublishStringAsync($"zigbee2mqtt/{_device.FriendlyName}/set", payload);
    }

    protected async Task Subscribe(Action<string> action)
    {
        var topic = $"zigbee2mqtt/{Device.FriendlyName}";
        
        var mqttFactory = new MqttFactory(); // TODO cache this somewhere + pass a custom logger?

        var mqttClient = mqttFactory.CreateMqttClient();
        
        // XXX client not disposed
        
        mqttClient.ApplicationMessageReceivedAsync += ea =>
        {
            if (ea.ApplicationMessage.Topic == topic)
            {
                var payloadBytes = ea.ApplicationMessage.PayloadSegment.Array;
                var payloadString = Encoding.UTF8.GetString(payloadBytes ?? Array.Empty<byte>());

                try
                {
                    action(payloadString);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            return Task.CompletedTask;
        };

        // Connect
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Server, _config.Port)
            .WithClientId($"{_device.FriendlyName}-{Guid.NewGuid()}")
            .Build();

        _ = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        // Subscribe
        var subOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => { f.WithTopic(topic); }).Build();

        _ = await mqttClient.SubscribeAsync(subOptions, CancellationToken.None);
    }
}