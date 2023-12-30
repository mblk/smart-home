namespace SmartHome.Infrastructure.Mqtt.Connector;

public interface IMqttConnector
{
    Task Publish(string clientId, string topic, string payload, bool retain = false,
        CancellationToken cancellationToken = default);

    Task<IAsyncDisposable> Subscribe(string clientId, string topic, Action<string> action,
        CancellationToken cancellationToken = default);
}