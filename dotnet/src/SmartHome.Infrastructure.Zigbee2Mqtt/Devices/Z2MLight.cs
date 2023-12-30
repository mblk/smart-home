using System.Text.Json;
using SmartHome.Infrastructure.Mqtt.Connector;
using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public class Z2MLight : Z2MDevice, IZ2MLight
{
    public Z2MLight(Z2MDiscoveryDevice device, IMqttConnector mqttConnector)
        : base(device, mqttConnector)
    {
    }

    public async Task TurnOn(double brightness, double temperature, (double, double)? color)
    {
        var data = new Dictionary<string, object>();
        SetState(data, true);
        SetBrightness(data, brightness);

        if (color is null)
            SetTemperature(data, temperature);
        else
            SetColor(data, color.Value);

        var json = JsonSerializer.Serialize(data);
        //Console.WriteLine($"json: {json}");
        await Publish(json);
    }

    public async Task TurnOff()
    {
        var data = new Dictionary<string, object>();
        SetState(data, false);

        var json = JsonSerializer.Serialize(data);
        //Console.WriteLine($"json: {json}");
        await Publish(json);
    }

    private void SetState(Dictionary<string, object> data, bool state)
    {
        var lightExpose = Device.Definition?.Exposes.OfType<Z2MDiscoveryLightExpose>().SingleOrDefault();
        if (lightExpose is null) return;

        var stateExpose = lightExpose.Features.OfType<Z2MDiscoveryBinaryExpose>()
            .SingleOrDefault(x => x.Name == "state");
        if (stateExpose is null) return;

        data.Add(stateExpose.Property, state
            ? (stateExpose.ValueOn ?? true)
            : (stateExpose.ValueOff ?? false));
    }

    private void SetBrightness(Dictionary<string, object> data, double brightness)
    {
        var lightExpose = Device.Definition?.Exposes.OfType<Z2MDiscoveryLightExpose>().SingleOrDefault();
        if (lightExpose is null) return;

        var brightnessExpose = lightExpose.Features.OfType<Z2MDiscoveryNumericExpose>()
            .SingleOrDefault(x => x.Name == "brightness");
        if (brightnessExpose is null) return;

        var min = brightnessExpose.ValueMin ?? 0;
        var max = brightnessExpose.ValueMax ?? 100;
        var range = max - min;
        var valueToSet = (int)Math.Round(min + range * brightness);

        data.Add(brightnessExpose.Property, valueToSet);
    }

    private void SetTemperature(Dictionary<string, object> data, double temperature)
    {
        var lightExpose = Device.Definition?.Exposes.OfType<Z2MDiscoveryLightExpose>().SingleOrDefault();
        if (lightExpose is null) return;

        var brightnessExpose = lightExpose.Features.OfType<Z2MDiscoveryNumericExpose>()
            .SingleOrDefault(x => x.Name == "color_temp");
        if (brightnessExpose is null) return;

        var min = brightnessExpose.ValueMin ?? 0;
        var max = brightnessExpose.ValueMax ?? 100;
        var range = max - min;
        var valueToSet = (int)Math.Round(min + range * temperature);

        data.Add(brightnessExpose.Property, valueToSet);
    }

    private void SetColor(Dictionary<string, object> data, (double, double) color)
    {
        var lightExpose = Device.Definition?.Exposes.OfType<Z2MDiscoveryLightExpose>().SingleOrDefault();
        if (lightExpose is null) return;

        var colorExpose = lightExpose.Features.OfType<Z2MDiscoveryCompositeExpose>()
            .SingleOrDefault(x => x.Name == "color_xy");
        if (colorExpose is null) return;

        var xExpose = colorExpose.Features.OfType<Z2MDiscoveryNumericExpose>().SingleOrDefault(x => x.Name == "x");
        var yExpose = colorExpose.Features.OfType<Z2MDiscoveryNumericExpose>().SingleOrDefault(x => x.Name == "y");
        if (xExpose is null || yExpose is null) return;

        var colorData = new Dictionary<string, object>
        {
            { xExpose.Property, color.Item1 },
            { yExpose.Property, color.Item2 }
        };

        data.Add(colorExpose.Property, colorData);
    }
}