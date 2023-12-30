using SmartHome.Infrastructure.Mqtt.Connector;
using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public abstract class Z2MDevice
{
    private readonly Z2MDiscoveryDevice _device;
    private readonly IMqttConnector _mqttConnector;

    protected Z2MDiscoveryDevice Device => _device;

    protected Z2MDevice(Z2MDiscoveryDevice device, IMqttConnector mqttConnector)
    {
        _device = device;
        _mqttConnector = mqttConnector;
    }

    protected async Task Publish(string payload)
    {
        var clientId = _device.FriendlyName;
        var topic = $"zigbee2mqtt/{_device.FriendlyName}/set";
        
        await _mqttConnector.Publish(clientId, topic, payload);
    }

    protected async Task Subscribe(Action<string> action)
    {
        var clientId = _device.FriendlyName;
        var topic = $"zigbee2mqtt/{Device.FriendlyName}";

        // XXX client not disposed
        _ = await _mqttConnector.Subscribe(clientId, topic, action);
    }
}