using SmartHome.Infrastructure.CatScale;
using SmartHome.Infrastructure.Devices;
using SmartHome.Infrastructure.Mqtt.Connector;
using SmartHome.Infrastructure.Mqtt.SharedState;
using SmartHome.Infrastructure.Zigbee2Mqtt.Devices;
using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;

namespace SmartHome.Service.Logic;

public static class DeviceFactory
{
    public static async Task<Devices> CreateDevices(IServiceProvider serviceProvider)
    {
        var catScaleSensor = serviceProvider.GetRequiredService<ICatScaleSensor>();
        var mqttConnector = serviceProvider.GetRequiredService<IMqttConnector>();
        var z2MDiscoverer = serviceProvider.GetRequiredService<IZ2MDeviceDiscoverer>();

        var z2MDevices = await z2MDiscoverer.DiscoverDevices();

        IZ2MDeviceFactory z2MDeviceFactory = new Z2MDeviceFactory(z2MDevices, mqttConnector,
            serviceProvider.GetRequiredService<ILogger<Z2MDeviceFactory>>());

        return new Devices
        {
            Virtual = new VirtualDevices
            {
                Sun = new SunSensor(),

                MasterMode = new MqttSharedState<MasterMode>(mqttConnector, "state", "master"),
                MasterModeOverride = new MqttSharedState<MasterMode>(mqttConnector, "state", "master_override"),

                LivingRoomLightMode = new MqttSharedState<LivingRoomLightMode>(mqttConnector, "state", "livingroom"),
                KitchenLightMode = new MqttSharedState<KitchenLightMode>(mqttConnector, "state", "kitchen"),
                BedroomLightMode = new MqttSharedState<BedroomLightMode>(mqttConnector, "state", "bedroom"),
            },

            LivingRoom = new LivingRoomDevices
            {
                CeilingLight1 = z2MDeviceFactory.GetLight("dev/livingroom/light/ceiling-1"),
                CeilingLight2 = z2MDeviceFactory.GetLight("dev/livingroom/light/ceiling-2"),
                CeilingLight3 = z2MDeviceFactory.GetLight("dev/livingroom/light/ceiling-3"),
                DeskLight = z2MDeviceFactory.GetLight("dev/livingroom/light/desk-1"),
                TvLight = z2MDeviceFactory.GetLight("dev/livingroom/light/tv-1"),
                Standing = z2MDeviceFactory.GetLight("dev/livingroom/light/standing-1"),
            },

            Kitchen = new KitchenDevices
            {
                Button1 = z2MDeviceFactory.GetButton("dev/kitchen/button-1"),
                OccupancySensor1 = z2MDeviceFactory.GetOccupancySensor("dev/kitchen/occupancy-1"),
                CeilingLight1 = z2MDeviceFactory.GetLight("dev/kitchen/light/ceiling-1"),
                CeilingLight2 = z2MDeviceFactory.GetLight("dev/kitchen/light/ceiling-2"),
                CeilingLight3 = z2MDeviceFactory.GetLight("dev/kitchen/light/ceiling-3"),
                CeilingLight4 = z2MDeviceFactory.GetLight("dev/kitchen/light/ceiling-4"),
            },

            Bedroom = new BedroomDevices
            {
                Button1 = z2MDeviceFactory.GetButton("dev/bedroom/button-1"),
                StandLight1 = z2MDeviceFactory.GetLight("dev/bedroom/light/stand-1"),
                StandLight2 = z2MDeviceFactory.GetLight("dev/bedroom/light/stand-2"),
            },

            Bathroom = new BathroomDevices
            {
                CatScale = catScaleSensor,
            }
        };
    }
}