using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics.Logger;

namespace SmartHome.Infrastructure.Mqtt.Connector;

public class MqttNetCustomLogger : IMqttNetLogger
{
    private readonly ILogger _logger;
    
    public bool IsEnabled => true;

    public MqttNetCustomLogger(ILogger logger)
    {
        _logger = logger;
    }
    
    public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
    {
        switch (logLevel)
        {
            default:
            case MqttNetLogLevel.Verbose:
                //_logger.LogDebug(exception, "MQTTNet: {Message} {Params}", message, parameters);
                break;

            case MqttNetLogLevel.Info:
                //_logger.LogInformation(exception, "MQTTNet: {Message} {Params}", message, parameters);
                break;
            
            case MqttNetLogLevel.Warning:
                _logger.LogWarning(exception, "MQTTNet: {Message} {Params}", message, parameters);
                break;
            
            case MqttNetLogLevel.Error:
                _logger.LogError(exception, "MQTTNet: {Message} {Params}", message, parameters);
                break;
        }
    }
}