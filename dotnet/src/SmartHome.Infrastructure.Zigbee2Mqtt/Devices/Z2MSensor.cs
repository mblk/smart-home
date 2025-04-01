using SmartHome.Infrastructure.Mqtt.Connector;
using System.Text.Json;

namespace SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

public delegate void Z2MSensorDataReceivedEventHandler(object sender, Z2MSensorDataReceivedEventArgs eventArgs);

public class Z2MSensorDataReceivedEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public required IReadOnlyDictionary<string, object> Values { get; init; }
}

public class Z2MSensor
{
    private readonly string _topixPrefix = "zigbee2mqtt/dev/";

    private readonly IMqttConnector _mqttConnector;

    public event Z2MSensorDataReceivedEventHandler? DataReceived;

    public Z2MSensor(IMqttConnector mqttConnector)
    {
        _mqttConnector = mqttConnector;

        _ = Task.Run(Worker);
    }

    private async Task Worker()
    {
        Console.WriteLine($"++++ before subscribe");

        var subscription = await _mqttConnector.Subscribe("", $"{_topixPrefix}#", OnDataReceived);

        _ = subscription; // XXX must dispose

        Console.WriteLine($"++++ after subscribe");
    }

    private void OnDataReceived(string topic, string data)
    {
        //Console.WriteLine($"+++ topic={topic} +++ data={data}");

        if (!topic.StartsWith(_topixPrefix))
            return;
        if (topic.EndsWith("/set"))
            return;
        if (topic.EndsWith("/availability"))
            return;

        var deviceId = topic[_topixPrefix.Length..];

        if (String.IsNullOrWhiteSpace(deviceId))
            return;

        //Console.WriteLine($"DeviceId: '{deviceId}'");

        // Not JSON?
        if (data.Length < 8 || data[0] != '{')
            return;


        var values = new Dictionary<string, object>();

        try
        {
            var jsonValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data);
            if (jsonValues is null)
                return;

            foreach (var (key, value) in jsonValues)
            {
                //Console.WriteLine($"{deviceId} >> {key} = {value} ({value.GetType().Name})");

                switch (value.ValueKind)
                {
                    case JsonValueKind.Number:
                        values.Add(key, value.GetDouble());
                        break;

                    case JsonValueKind.False:
                    case JsonValueKind.True:
                        values.Add(key, value.ValueKind == JsonValueKind.True);
                        break;

                    default:
                        //Console.WriteLine($"unknown: {value.ValueKind}");
                        break;
                }
            }
        }
        catch (JsonException e)
        {
            _ = e;
            Console.WriteLine($"JsonException: {e.Message}");
        }

        if (values.Count == 0) return;

        RaiseDataReceived(deviceId, values);
    }

    private void RaiseDataReceived(string deviceId, IReadOnlyDictionary<string, object> values)
    {
        try
        {
            DataReceived?.Invoke(this, new Z2MSensorDataReceivedEventArgs()
            {
                DeviceId = deviceId,
                Values = values,
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}