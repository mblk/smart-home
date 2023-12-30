namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public class Z2MButtonFallback : IZ2MButton
{
#pragma warning disable CS0067 // Event is never used
    public event Z2MButtonEventHandler? Event;
#pragma warning restore CS0067 // Event is never used
}