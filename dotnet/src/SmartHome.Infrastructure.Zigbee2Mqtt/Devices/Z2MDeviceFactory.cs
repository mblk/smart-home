using Microsoft.Extensions.Logging;
using SmartHome.Infrastructure.Mqtt.Connector;
using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public interface IZ2MDeviceFactory
{
    IZ2MLight GetLight(string friendlyName);
    IZ2MButton GetButton(string friendlyName);
    IZ2MOccupancySensor GetOccupancySensor(string friendlyName);
}

public class Z2MDeviceFactory : IZ2MDeviceFactory
{
    private readonly IReadOnlyDictionary<string, Z2MDiscoveryDevice> _discoveredDevices;
    private readonly IMqttConnector _mqttConnector;
    private readonly ILogger<Z2MDeviceFactory> _logger;

    public Z2MDeviceFactory(IEnumerable<Z2MDiscoveryDevice> discoveredDevices, IMqttConnector mqttConnector,
        ILogger<Z2MDeviceFactory> logger)
    {
        _discoveredDevices = discoveredDevices.ToDictionary(x => x.FriendlyName, x => x);
        _mqttConnector = mqttConnector;
        _logger = logger;
    }

    public IZ2MLight GetLight(string friendlyName)
    {
        if (_discoveredDevices.TryGetValue(friendlyName, out var device))
            return new Z2MLight(device, _mqttConnector);
        
        _logger.LogError("Device not found: {Name}", friendlyName);
        return new Z2MLightFallback();
    }

    public IZ2MButton GetButton(string friendlyName)
    {
        if (_discoveredDevices.TryGetValue(friendlyName, out var device))
            return new Z2MButton(device, _mqttConnector);

        _logger.LogError("Device not found: {Name}", friendlyName);
        return new Z2MButtonFallback();
    }

    public IZ2MOccupancySensor GetOccupancySensor(string friendlyName)
    {
        if (_discoveredDevices.TryGetValue(friendlyName, out var device))
            return new Z2MOccupancySensor(device, _mqttConnector);

        _logger.LogError("Device not found: {Name}", friendlyName);
        return new Z2MOccupancySensorFallback();
    }
}