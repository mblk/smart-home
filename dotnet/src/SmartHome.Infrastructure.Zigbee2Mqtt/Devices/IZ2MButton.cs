namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public interface IZ2MButton
{
    event Z2MButtonEventHandler Event;
}

public delegate void Z2MButtonEventHandler(object sender, Z2MButtonEventArgs eventArgs);

public class Z2MButtonEventArgs : EventArgs
{
    public string Action { get; }

    public Z2MButtonEventArgs(string action)
    {
        Action = action;
    }
}
