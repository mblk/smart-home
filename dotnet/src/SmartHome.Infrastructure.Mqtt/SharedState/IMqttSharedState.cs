namespace SmartHome.Infrastructure.Mqtt.SharedState;

public interface IMqttSharedState<T>
    where T : struct, Enum
{
    event Action<T> ChangeRequest;

    Task Update(T newValue);
}