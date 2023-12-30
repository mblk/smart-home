namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

public interface IZ2MDeviceDiscoverer
{
    Task<IEnumerable<Z2MDiscoveryDevice>> DiscoverDevices(CancellationToken cancellationToken = default);
}