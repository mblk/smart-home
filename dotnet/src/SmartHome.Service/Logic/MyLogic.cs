using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using SmartHome.Infrastructure.CatScale;
using SmartHome.Infrastructure.Zigbee2Mqtt;
using SmartHome.Infrastructure.Zigbee2Mqtt.Devices;
using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;
using SmartHome.Utils;

namespace SmartHome.Service.Logic;

public class MyLogic : IDisposable
{
    private readonly ILogger<MyLogic> _logger;
    private readonly CancellationTokenSource _cts = new();

    private readonly Z2MConfig _z2MConfig;
    private readonly CatScaleConfig _catScaleConfig;

    public MyLogic(ILogger<MyLogic> logger, IConfiguration configuration)
    {
        _logger = logger;
        _logger.LogInformation("ctor");

        _z2MConfig = configuration.GetSection("Z2M").Get<Z2MConfig>()
                     ?? throw new Exception("Can't get z2m config");
        _catScaleConfig = configuration.GetSection("CatScale").Get<CatScaleConfig>()
                          ?? throw new Exception("Cant get cat-scale config");

        _logger.LogInformation("Z2M config: {Server} {Port}", _z2MConfig.Server, _z2MConfig.Port);
        _logger.LogInformation("Cat-scale config: {Endpoint}", _catScaleConfig.Endpoint);

        _ = Task.Run(Worker);
    }

    public void Dispose()
    {
        _logger.LogInformation("Dispose");
        _cts.Cancel();
    }

    private async Task Worker()
    {
        var configuration = new Configuration
        {
            Z2MConfig = _z2MConfig,
            CatScaleConfig = _catScaleConfig,
        };

        var devices = await CreateDevices(configuration);
        var state = CreateState();

        var eventQueue = new BlockingCollection<LogicEvent>();

        _ = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1000), _cts.Token);
                eventQueue.Add(new TimerTickEvent());
            }
        });

        devices.Bathroom.CatScale.PooCountChanged += pooCount
            => eventQueue.Add(new PooCountChangedEvent(pooCount));
        devices.Bathroom.CatScale.Start();

        devices.Kitchen.Button1.Event += (_, e)
            => eventQueue.Add(new ButtonPressEvent("kitchen", e.Action));
        devices.Bedroom.Button1.Event += (_, e)
            => eventQueue.Add(new ButtonPressEvent("bedroom", e.Action));

        devices.Kitchen.OccupancySensor1.Event += (_, e)
            => eventQueue.Add(new OccupancySensorEvent("kitchen", e.Occupancy));

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                foreach (var @event in eventQueue.GetConsumingEnumerable(_cts.Token))
                {
                    var sw = Stopwatch.StartNew();
                    var oldState = state;

                    switch (@event)
                    {
                        case TimerTickEvent timerTickEvent:
                            state = ProcessTimerTickEvent(state, timerTickEvent);
                            break;

                        case PooCountChangedEvent pooCountChangedEvent:
                            state = ProcessPooCountChangedEvent(state, pooCountChangedEvent);
                            break;

                        case ButtonPressEvent buttonPressEvent:
                            state = ProcessButtonEvent(state, buttonPressEvent);
                            break;

                        case OccupancySensorEvent occupancySensorEvent:
                            state = ProcessOccupancySensorEvent(state, occupancySensorEvent);
                            break;
                    }

                    if (!oldState.Equals(state))
                    {
                        var stateJson = JsonSerializer.Serialize(state, new JsonSerializerOptions()
                        {
                            IncludeFields = true,
                        });
                        Console.WriteLine($"State changed: {stateJson}");

                        await UpdateLivingRoomLights(state, devices);
                        await UpdateKitchenLights(state, devices);
                        await UpdateBedroomLights(state, devices);
                    }

                    if (@event is not TimerTickEvent)
                    {
                        Console.WriteLine($"Processed event {@event} in {sw.ElapsedMilliseconds} ms");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private static State ProcessTimerTickEvent(State state, TimerTickEvent e)
    {
        if (state.KitchenOccupied)
        {
            state.KitchenOccupancyTimeout = 10;
        }
        else
        {
            if (state.KitchenOccupancyTimeout > 0)
                state.KitchenOccupancyTimeout--;
        }

        return state;
    }

    private static State ProcessPooCountChangedEvent(State state, PooCountChangedEvent e)
    {
        state.LitterBoxIsDirty = e.PooCount > 0;
        return state;
    }

    private static State ProcessButtonEvent(State state, ButtonPressEvent e)
    {
        switch (e.Button)
        {
            case "kitchen":
                switch (e.Action)
                {
                    case "button_1_single":
                        state.LivingRoomLightMode = state.LivingRoomLightMode.Next();
                        break;

                    case "button_2_single":
                        state.KitchenLightMode = state.KitchenLightMode.Next();
                        break;

                    case "button_3_single":
                        state.BedroomLightMode = state.BedroomLightMode.Next();
                        break;
                }

                break;

            case "bedroom":
                switch (e.Action)
                {
                    case "button_1_single":
                        state.BedroomLightMode = state.BedroomLightMode.Next();
                        break;
                }

                break;

            default:
                Console.WriteLine($"Invalid button: {e.Button}");
                break;
        }

        return state;
    }

    private static State ProcessOccupancySensorEvent(State state, OccupancySensorEvent e)
    {
        switch (e.Sensor)
        {
            case "kitchen":
                state.KitchenOccupied = e.Occupancy;
                break;

            default:
                Console.WriteLine($"Invalid occupancy sensor: {e.Sensor}");
                break;
        }

        return state;
    }

    private static async Task UpdateLivingRoomLights(State state, Devices devices)
    {
        double livingRoomLightLevel = state.LivingRoomLightMode switch
        {
            LivingRoomLightMode.Dim25 => 0.25d,
            LivingRoomLightMode.Dim50 => 0.5d,
            LivingRoomLightMode.Full => 1d,
            _ => 0d,
        };

        if (livingRoomLightLevel > 0.1d)
        {
            await devices.LivingRoom.CeilingLight1.TurnOn(brightness: livingRoomLightLevel);
            await devices.LivingRoom.CeilingLight2.TurnOn(brightness: livingRoomLightLevel);
            await devices.LivingRoom.CeilingLight3.TurnOn(brightness: livingRoomLightLevel);
            await devices.LivingRoom.DeskLight.TurnOn(brightness: livingRoomLightLevel);
            await devices.LivingRoom.Standing.TurnOn(brightness: livingRoomLightLevel);
        }
        else
        {
            await devices.LivingRoom.CeilingLight1.TurnOff();
            await devices.LivingRoom.CeilingLight2.TurnOff();
            await devices.LivingRoom.CeilingLight3.TurnOff();
            await devices.LivingRoom.DeskLight.TurnOff();
            await devices.LivingRoom.Standing.TurnOff();
        }

        if (state.LitterBoxIsDirty)
        {
            await devices.LivingRoom.TvLight.TurnOn(brightness: 0.5d, color: (0.66d, 0.34d));
        }
        else if (livingRoomLightLevel > 0.1d)
        {
            await devices.LivingRoom.TvLight.TurnOn(brightness: livingRoomLightLevel);
        }
        else
        {
            await devices.LivingRoom.TvLight.TurnOff();
        }
    }

    private static async Task UpdateKitchenLights(State state, Devices devices)
    {
        double kitchenLightLevel = state.KitchenLightMode switch
        {
            KitchenLightMode.Full => 1d,
            KitchenLightMode.Auto => (state.KitchenOccupied || state.KitchenOccupancyTimeout > 0) ? 0.5d : 0d,
            _ => 0d,
        };

        if (kitchenLightLevel > 0.1d)
        {
            await devices.Kitchen.CeilingLight1.TurnOn(brightness: kitchenLightLevel);
            await devices.Kitchen.CeilingLight2.TurnOn(brightness: kitchenLightLevel);
            await devices.Kitchen.CeilingLight3.TurnOn(brightness: kitchenLightLevel);
            await devices.Kitchen.CeilingLight4.TurnOn(brightness: kitchenLightLevel);
        }
        else
        {
            await devices.Kitchen.CeilingLight1.TurnOff();
            await devices.Kitchen.CeilingLight2.TurnOff();
            await devices.Kitchen.CeilingLight3.TurnOff();
            await devices.Kitchen.CeilingLight4.TurnOff();
        }
    }

    private static async Task UpdateBedroomLights(State state, Devices devices)
    {
        double bedroomLightLevel = state.BedroomLightMode switch
        {
            BedroomLightMode.Off => 0d,
            BedroomLightMode.Dim => 0.25d,
            BedroomLightMode.Full => 1d,
            _ => 0d,
        };

        if (bedroomLightLevel > 0.1d)
        {
            await devices.Bedroom.StandLight1.TurnOn(brightness: bedroomLightLevel);
            await devices.Bedroom.StandLight2.TurnOn(brightness: bedroomLightLevel);
        }
        else
        {
            await devices.Bedroom.StandLight1.TurnOff();
            await devices.Bedroom.StandLight2.TurnOff();
        }
    }

    private static State CreateState()
    {
        return new State
        {
            LivingRoomLightMode = LivingRoomLightMode.Off,
            KitchenLightMode = KitchenLightMode.Auto,
            BedroomLightMode = BedroomLightMode.Off,
        };
    }

    private static async Task<Devices> CreateDevices(Configuration configuration)
    {
        var catScaleSensor = new CatScaleSensor(configuration.CatScaleConfig);

        var sw = Stopwatch.StartNew();
        var z2MDiscovery = new Z2MDiscovery(configuration.Z2MConfig);
        var z2MDevices = await z2MDiscovery.DiscoverDevices();
        Console.WriteLine($"Discovery found {z2MDevices.Length} devices in {sw.ElapsedMilliseconds} ms.");

        var z2MDeviceFactory = new Z2MDeviceFactory(configuration.Z2MConfig, z2MDevices);

        return new Devices
        {
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