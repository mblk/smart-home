using SmartHome.Infrastructure.Mqtt.Connector;
using SmartHome.Utils;

namespace SmartHome.Infrastructure.Mqtt.Devices;

public interface IRemoteAudioPlayer
{
    Task PlayRadio(string uri);
    Task StopRadio();
    Task Speak(string text);
    Task SetVolume(double volume);
}

public class RemoteAudioPlayer : IRemoteAudioPlayer
{
    private readonly string _topic = "audio/control";
    private readonly string _clientId = "audio";

    private readonly IMqttConnector _mqttConnector;

    private int? _prevVolume = null;

    public RemoteAudioPlayer(IMqttConnector mqttConnector)
    {
        _mqttConnector = mqttConnector;
    }

    public async Task PlayRadio(string uri)
    {
        await _mqttConnector.Publish(_clientId, _topic, $"play_radio {uri}");
    }
    public async Task StopRadio()
    {
        await _mqttConnector.Publish(_clientId, _topic, "stop_radio");
    }

    public async Task Speak(string text)
    {
        await _mqttConnector.Publish(_clientId, _topic, $"speak {text}");
    }

    public async Task SetVolume(double volume)
    {
        var intVolume = ((int)Math.Round(100.0 * volume)).Clamp(0, 100);

        if (_prevVolume == intVolume)
            return;

        await _mqttConnector.Publish(_clientId, _topic, $"volume {intVolume}");
        _prevVolume = intVolume;
    }
}
