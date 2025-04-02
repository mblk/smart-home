using System.Buffers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace SmartHome.Infrastructure.Mqtt.Connector;

public class MqttConnector : IMqttConnector
{
    private readonly ILogger<MqttConnector> _logger;
    private readonly MqttConfig _config;
    private readonly MqttClientFactory _factory;

    public MqttConnector(ILogger<MqttConnector> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        _config = configuration
            .GetRequiredSection("MQTT")
            .Get<MqttConfig>()
            ?? throw new Exception("Cant get MQTT config");
                
        _logger.LogInformation("MqttConnector created, config: {Config}", _config);

        _factory = new MqttClientFactory(new MqttNetCustomLogger(logger));
    }

    public async Task Publish(string clientId, string topic, string payload, bool retain = false, CancellationToken cancellationToken = default)
    {
        using var mqttClient = _factory.CreateMqttClient();

        // Connect
        var mqttClientOptions = _factory.CreateClientOptionsBuilder()
            .WithTcpServer(_config.Server, _config.Port)
            .WithClientId($"{clientId}-{Guid.NewGuid()}")
            .Build();

        _ = await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);

        // Publish
        Console.WriteLine($"Publish: {topic}: {payload}"); // TODO remove later
        await mqttClient.PublishStringAsync(topic, payload,
            cancellationToken: cancellationToken,
            retain: retain,
            qualityOfServiceLevel: MqttQualityOfServiceLevel.ExactlyOnce);
    }

    public async Task<IAsyncDisposable> Subscribe(string clientId, string topic, Action<string, string> action, CancellationToken cancellationToken = default)
    {
        var mqttClient = _factory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += ea =>
        {
            // + single level wildcard
            // # multi level wildcard

            //Console.WriteLine($"ApplicationMessageReceivedAsync filter={topic}, actual={ea.ApplicationMessage.Topic}");

            //if (ea.ApplicationMessage.Topic == topic)
            {
                var payloadBytes = ea.ApplicationMessage.Payload.ToArray();
                var payloadString = Encoding.UTF8.GetString(payloadBytes ?? []);

                try
                {
                    action(ea.ApplicationMessage.Topic, payloadString);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            return Task.CompletedTask;
        };

        // Connect
        var mqttClientOptions = _factory.CreateClientOptionsBuilder()
            .WithTcpServer(_config.Server, _config.Port)
            .WithClientId($"{clientId}-{Guid.NewGuid()}")
            .Build();

        _ = await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);

        // Subscribe
        var subOptions = _factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f =>
            {
                f.WithTopic(topic);
            })
            .Build();

        _ = await mqttClient.SubscribeAsync(subOptions, cancellationToken);

        return new MqttSubscription(_factory, mqttClient, clientId);
    }

    private class MqttSubscription : IAsyncDisposable
    {
        private readonly MqttClientFactory _factory;
        private readonly IMqttClient _mqttClient;
        private readonly string _clientId;

        public MqttSubscription(MqttClientFactory factory, IMqttClient mqttClient, string clientId)
        {
            _factory = factory;
            _mqttClient = mqttClient;
            _clientId = clientId;
        }

        public async ValueTask DisposeAsync()
        {
            var disconnectOptions = _factory.CreateClientDisconnectOptionsBuilder()
                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                .Build();

            await _mqttClient.DisconnectAsync(disconnectOptions);

            _mqttClient.Dispose();
        }
    }
}