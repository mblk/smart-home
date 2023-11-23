using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public class Z2MDeviceFactory
{
    private readonly Z2MConfig _config;
    private readonly Z2MDiscoveryDevice[] _discoveredDevices;

    public Z2MDeviceFactory(Z2MConfig config, Z2MDiscoveryDevice[] discoveredDevices)
    {
        _config = config;
        _discoveredDevices = discoveredDevices;
    }

    public IZ2MLight GetLight(string friendlyName)
    {
        var device = _discoveredDevices.SingleOrDefault(x => x.FriendlyName == friendlyName);
        if (device != null)
            return new Z2MLight(_config, device);

        Console.WriteLine($"ERROR: Device not found: {friendlyName}");
        return new Z2MLightFallback();
    }

    public IZ2MButton GetButton(string friendlyName)
    {
        var device = _discoveredDevices.SingleOrDefault(x => x.FriendlyName == friendlyName);
        if (device != null)
            return new Z2MButton(_config, device);

        Console.WriteLine($"ERROR: Device not found: {friendlyName}");
        return new Z2MButtonFallback();
    }

    public IZ2MOccupancySensor GetOccupancySensor(string friendlyName)
    {
        var device = _discoveredDevices.SingleOrDefault(x => x.FriendlyName == friendlyName);
        if (device != null)
            return new Z2MOccupancySensor(_config, device);

        Console.WriteLine($"ERROR: Device not found: {friendlyName}");
        return new Z2MOccupancySensorFallback();
    }
}