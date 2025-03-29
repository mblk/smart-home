using System.Text.Json;
using SmartHome.Infrastructure.Mqtt.Connector;
using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public class Z2MOccupancySensor : Z2MDevice, IZ2MOccupancySensor
{
    public event Z2MOccupancySensorEventHandler? Event;

    public Z2MOccupancySensor(Z2MDiscoveryDevice device, IMqttConnector mqttConnector)
        : base(device, mqttConnector)
    {
        _ = Task.Run(Worker);
    }

    private async Task Worker()
    {
        await Subscribe(OnDataReceived);
    }

    private void OnDataReceived(string topic, string data)
    {
        _ = topic;

        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data);
        if (values is null)
            return;

        if (!values.TryGetValue("occupancy", out var occupancyObject))
            return;

        switch (occupancyObject.ValueKind)
        {
            case JsonValueKind.False:
                RaiseEvent(false);
                break;
            
            case JsonValueKind.True:
                RaiseEvent(true);
                break;
        }
    }

    private void RaiseEvent(bool occupancy)
    {
        try
        {
            Event?.Invoke(this, new Z2MOccupancySensorEventArgs(occupancy));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}