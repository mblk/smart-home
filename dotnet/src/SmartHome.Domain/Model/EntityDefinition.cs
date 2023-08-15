namespace SmartHome.Domain.Model;

public record EntityDefinition
(
    string Id,
    OutputDefinition[] Outputs,
    InputDefinition[] Inputs,
    EventDefinition[] Events
);
public record OutputDefinition(string Name);
public record InputDefinition(string Name);
public record EventDefinition(string Name);



public interface ICatScale
{
    bool IsDirty { get; }

    event Action IsDirtyChanged;
}

public interface ILight
{
    bool IsOn { get; }

    void TurnOn(double brightness = 1.0);
    void TurnOff();
}


