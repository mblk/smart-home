using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartHome.Infrastructure.Mqtt.Connector;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

public class Z2MDeviceDiscoverer : IZ2MDeviceDiscoverer
{
    private const string DevicesTopic = "zigbee2mqtt/bridge/devices";

    private readonly IMqttConnector _mqttConnector;
    private readonly ILogger<Z2MDeviceDiscoverer> _logger;

    public Z2MDeviceDiscoverer(IMqttConnector mqttConnector, ILogger<Z2MDeviceDiscoverer> logger)
    {
        _mqttConnector = mqttConnector;
        _logger = logger;
    }

    public async Task<IEnumerable<Z2MDiscoveryDevice>> DiscoverDevices(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var devicesReceived = false;
        string? devicesData = null;

        // Subscribe
        var subscription = await _mqttConnector.Subscribe("discovery", DevicesTopic,
            onDataReceived, cancellationToken);

        void onDataReceived(string data)
        {
            devicesReceived = true;
            devicesData = data;
        }

        // Wait for data
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (!devicesReceived)
        {
            // TODO add timeout?
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        // Disconnect
        await subscription.DisposeAsync();

        // Parse data
        if (String.IsNullOrWhiteSpace(devicesData))
            throw new Exception("Did not receive any device data");

        var devices = JsonSerializer.Deserialize<Z2MDiscoveryDevice[]>(devicesData)
                      ?? throw new JsonException("Failed to deserialize device data");

        _logger.LogInformation("Found {Count} devices in {Time} ms", devices.Length, sw.ElapsedMilliseconds);

        return devices;
    }
}