namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public class Z2MLightFallback : IZ2MLight
{
    public Task TurnOn(double brightness = 1, double temperature = 1, (double, double)? color = null)
    {
        return Task.CompletedTask;
    }

    public Task TurnOff()
    {
        return Task.CompletedTask;
    }
}