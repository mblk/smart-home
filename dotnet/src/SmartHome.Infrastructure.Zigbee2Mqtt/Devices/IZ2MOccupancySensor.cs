namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public interface IZ2MOccupancySensor
{
    event Z2MOccupancySensorEventHandler Event;
}

public delegate void Z2MOccupancySensorEventHandler(object sender, Z2MOccupancySensorEventArgs eventArgs);

public class Z2MOccupancySensorEventArgs : EventArgs
{
    public bool Occupancy { get; }

    public Z2MOccupancySensorEventArgs(bool occupancy)
    {
        Occupancy = occupancy;
    }
}
