using SmartHome.Infrastructure.Devices;

namespace SmartHome.Service.Logic;

public abstract record LogicEvent;

public record TimerTickEvent : LogicEvent;

public record PooCountChangedEvent(int PooCount) : LogicEvent;
public record ButtonPressEvent(string Button, string Action) : LogicEvent;
public record OccupancySensorEvent(string Sensor, bool Occupancy) : LogicEvent;

public record ChangeMasterMode(MasterMode Mode) : LogicEvent;

public record ChangeLivingRoomLightMode(LightMode Mode) : LogicEvent;
public record ChangeKitchenLightMode(LightMode Mode) : LogicEvent;
public record ChangeBedroomLightMode(LightMode Mode) : LogicEvent;

public record SunStateChangedEvent(SunState SunState) : LogicEvent;
public record RoomLightEstimateChangedEvent(RoomLightEstimate Estimate) : LogicEvent;