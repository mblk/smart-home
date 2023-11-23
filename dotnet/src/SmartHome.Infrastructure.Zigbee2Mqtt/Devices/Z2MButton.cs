using System.ComponentModel;
using System.Text.Json;
using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public class Z2MButton : Z2MDevice, IZ2MButton
{
    public event Z2MButtonEventHandler? Event;

    public Z2MButton(Z2MConfig config, Z2MDiscoveryDevice device)
        : base(config, device)
    {
        _ = Task.Run(Worker);
    }

    private async Task Worker()
    {
        await Subscribe(OnDataReceived);
    }

    private void OnDataReceived(string data)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data);
        if (values is null)
            return;

        if (!values.TryGetValue("action", out var actionObject))
            return;

        var actionString = actionObject.ToString();
        RaiseEvent(actionString);
    }

    private void RaiseEvent(string action)
    {
        try
        {
            Event?.Invoke(this, new Z2MButtonEventArgs(action));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}

