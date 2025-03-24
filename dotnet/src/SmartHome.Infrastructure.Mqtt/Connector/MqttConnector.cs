using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace SmartHome.Infrastructure.Mqtt.Connector;

public class MqttConnector : IMqttConnector
{
    private readonly ILogger<MqttConnector> _logger;
    private readonly MqttConfig _config;
    private readonly MqttFactory _factory;

    public MqttConnector(ILogger<MqttConnector> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        _config = configuration.GetRequiredSection("MQTT").Get<MqttConfig>()
                          ?? throw new Exception("Cant get MQTT config");
                
        _logger.LogInformation("MqttConnector created, config: {Config}", _config);

        _factory = new MqttFactory(new MqttNetCustomLogger(logger));
    }

    public async Task Publish(string clientId, string topic, string payload, bool retain = false,
        CancellationToken cancellationToken = default)
    {
        using var mqttClient = _factory.CreateMqttClient();

        // Connect
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Server, _config.Port)
            .WithClientId($"{clientId}-{Guid.NewGuid()}")
            .Build();

        _ = await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);

        // Publish
        Console.WriteLine($"Publish: {topic}: {payload}");
        await mqttClient.PublishStringAsync(topic, payload,
            cancellationToken: cancellationToken,
            retain: retain,
            qualityOfServiceLevel: MqttQualityOfServiceLevel.ExactlyOnce);
    }

    public async Task<IAsyncDisposable> Subscribe(string clientId, string topic, Action<string> action,
        CancellationToken cancellationToken = default)
    {
        var mqttClient = _factory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += ea =>
        {
            if (ea.ApplicationMessage.Topic == topic)
            {
                var payloadBytes = ea.ApplicationMessage.PayloadSegment.Array;
                var payloadString = Encoding.UTF8.GetString(payloadBytes ?? Array.Empty<byte>());

                try
                {
                    action(payloadString);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            return Task.CompletedTask;
        };

        // Connect
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Server, _config.Port)
            .WithClientId($"{clientId}-{Guid.NewGuid()}")
            .Build();

        _ = await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);

        // Subscribe
        var subOptions = _factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => { f.WithTopic(topic); }).Build();

        _ = await mqttClient.SubscribeAsync(subOptions, cancellationToken);

        return new MqttSubscription(mqttClient, clientId);
    }

    private class MqttSubscription : IAsyncDisposable
    {
        private readonly IMqttClient _mqttClient;
        private readonly string _clientId;

        public MqttSubscription(IMqttClient mqttClient, string clientId)
        {
            _mqttClient = mqttClient;
            _clientId = clientId;
        }

        public async ValueTask DisposeAsync()
        {
            var disconnectOptions = new MqttClientDisconnectOptionsBuilder()
                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                .Build();

            await _mqttClient.DisconnectAsync(disconnectOptions, CancellationToken.None);

            _mqttClient.Dispose();
        }
    }
}