namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public class Z2MOccupancySensorFallback : IZ2MOccupancySensor
{
#pragma warning disable CS0067 // Event is never used
    public event Z2MOccupancySensorEventHandler? Event;
#pragma warning restore CS0067 // Event is never used
}