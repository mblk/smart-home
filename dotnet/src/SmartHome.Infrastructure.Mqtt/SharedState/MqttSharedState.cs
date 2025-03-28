using System.Text.Json;
using SmartHome.Infrastructure.Mqtt.Connector;

namespace SmartHome.Infrastructure.Mqtt.SharedState;

public class MqttSharedState<T> : IMqttSharedState<T>
    where T : struct, Enum
{
    private class MqttSharedStateData
    {
        public required string CurrentValue { get; init; }
        public required string[] AvailableValues { get; init; }
    }

    private readonly IMqttConnector _mqttConnector;
    private readonly string _prefix;
    private readonly string _name;

    private T? _prevValue;

    public event Action<T?>? ChangeRequest;

    public MqttSharedState(IMqttConnector mqttConnector, string prefix, string name)
    {
        _mqttConnector = mqttConnector;
        _prefix = prefix;
        _name = name;

        Task.Run(Worker); // TODO change this somehow (same for Device classes)
    }

    public async Task Update(T? newValue)
    {
        // No change?
        if (_prevValue.Equals(newValue))
            return;
        _prevValue = newValue;

        // Publish new value
        var clientId = $"shared-state-{_name}";
        var topic = $"{_prefix}/{_name}";

        var payload = JsonSerializer.Serialize(new MqttSharedStateData
        {
            CurrentValue = newValue?.ToString() ?? String.Empty,
            AvailableValues = Enum.GetNames<T>(),
        });

        await _mqttConnector.Publish(clientId, topic, payload);
    }

    private async Task Worker()
    {
        var clientId = $"shared-state-{_name}";
        var topic = $"{_prefix}/{_name}/set";

        _ = await _mqttConnector.Subscribe(clientId, topic, OnDataReceived);
    }

    private void OnDataReceived(string data)
    {
        if (String.IsNullOrWhiteSpace(data))
        {
            InvokeChangeRequest(null);
        }
        else if (Enum.TryParse<T>(data, out var x))
        {
            InvokeChangeRequest(x);
        }
        else
        {
            Console.WriteLine($"Failed to parse: {data}");
        }
    }

    private void InvokeChangeRequest(T? value)
    {
        try
        {
            ChangeRequest?.Invoke(value);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}