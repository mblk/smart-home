namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public interface IZ2MLight
{
    Task TurnOn(double brightness = 1.0d, double temperature = 1.0d, (double, double)? color = null);
    Task TurnOff();
}