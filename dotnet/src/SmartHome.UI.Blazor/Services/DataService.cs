using SmartHome.Infrastructure.Mqtt.Connector;
using System.Text.Json;

namespace SmartHome.UI.Blazor.Services;

public interface IDataService
{
    Task SetState(string itemId, string value);

    Task<IAsyncDisposable> Subscribe(Action<Item> onChange);
}

public abstract record Item(string Id);
public record EnumItem(string Id, IReadOnlyList<string> AllValues, string CurrentValue) : Item(Id);




public class DataService : IDataService, IDisposable
{
    private readonly ILogger<DataService> _logger;
    private readonly IMqttConnector _mqttConnector;

    private class MqttSharedStateData
    {
        public required string CurrentValue { get; init; }
        public required string[] AvailableValues { get; init; }
    }

    public DataService(ILogger<DataService> logger, IMqttConnector mqttConnector)
	{
        _logger = logger;
        _mqttConnector = mqttConnector;

        _logger.LogInformation("DataService ctor");
    }

    public void Dispose()
    {
        _logger.LogInformation("DataService dispose");
    }

    public async Task SetState(string itemId, string value)
	{
        await _mqttConnector.Publish(
            clientId: $"Blazor-{Guid.NewGuid()}",
            topic: $"state/{itemId}/set",
            payload: value);
    }

    public async Task<IAsyncDisposable> Subscribe(Action<Item> onChange)
    {
        return await _mqttConnector.Subscribe(
            clientId: $"Blazor-{Guid.NewGuid()}",
            topic: "state/+",
            onDataReceived);

        void onDataReceived(string topic, string payload)
        {
            var data = JsonSerializer.Deserialize<MqttSharedStateData>(payload);
            if (data != null)
            {
                var topicParts = topic.Split('/');
                if (topicParts.Length == 2)
                {
                    var id = topicParts[1];

                    var item = new EnumItem(id, data.AvailableValues, data.CurrentValue);

                    try
                    {
                        onChange(item);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }
    }
}
