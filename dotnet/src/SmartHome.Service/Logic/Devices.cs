using SmartHome.Infrastructure.CatScale;
using SmartHome.Infrastructure.Mqtt.SharedState;
using SmartHome.Infrastructure.Zigbee2Mqtt.Devices;

namespace SmartHome.Service.Logic;

public class Devices
{
    public required VirtualDevices Virtual;
    public required LivingRoomDevices LivingRoom;
    public required KitchenDevices Kitchen;
    public required BedroomDevices Bedroom;
    public required BathroomDevices Bathroom;
}

public class VirtualDevices
{
    public required IMqttSharedState<LivingRoomLightMode> LivingRoomLightMode;
    public required IMqttSharedState<KitchenLightMode> KitchenLightMode;
    public required IMqttSharedState<BedroomLightMode> BedroomLightMode;
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
    public required ICatScaleSensor CatScale;
}