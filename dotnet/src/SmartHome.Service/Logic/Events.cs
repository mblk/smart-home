namespace SmartHome.Service.Logic;

public abstract record LogicEvent;

public record TimerTickEvent : LogicEvent;

public record PooCountChangedEvent(int PooCount) : LogicEvent;

public record ButtonPressEvent(string Button, string Action) : LogicEvent;

public record OccupancySensorEvent(string Sensor, bool Occupancy) : LogicEvent;

public record ChangeLivingRoomLightMode(LivingRoomLightMode Mode) : LogicEvent;
public record ChangeKitchenLightMode(KitchenLightMode Mode) : LogicEvent;
public record ChangeBedroomLightMode(BedroomLightMode Mode) : LogicEvent;