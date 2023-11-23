using SmartHome.Infrastructure.CatScale;
using SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

namespace SmartHome.Service.Logic;

public class Devices
{
    public required LivingRoomDevices LivingRoom { get; init; }
    public required KitchenDevices Kitchen { get; init; }
    public required BedroomDevices Bedroom { get; init; }
    public required BathroomDevices Bathroom { get; init; }
}

public class LivingRoomDevices
{
    public required IZ2MLight CeilingLight1;
    public required IZ2MLight CeilingLight2;
    public required IZ2MLight CeilingLight3;
    public required IZ2MLight DeskLight;
    public required IZ2MLight TvLight;
    public required IZ2MLight Standing;
}

public class KitchenDevices
{
    public required IZ2MButton Button1;
    public required IZ2MOccupancySensor OccupancySensor1;
    public required IZ2MLight CeilingLight1;
    public required IZ2MLight CeilingLight2;
    public required IZ2MLight CeilingLight3;
    public required IZ2MLight CeilingLight4;
}

public class BedroomDevices
{
    public required IZ2MButton Button1;
    public required IZ2MLight StandLight1;
    public required IZ2MLight StandLight2;
}

public class BathroomDevices
{
    public required CatScaleSensor CatScale { get; init; }
}