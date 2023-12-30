using System.Collections.Concurrent;
using System.Diagnostics;
using SmartHome.Utils;

namespace SmartHome.Service.Logic;

public class MyLogic : IAsyncDisposable
{
    private readonly ILogger<MyLogic> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cts = new();

    private Task? _timerTask;
    private Task? _eventLoopTask;

    public MyLogic(ILogger<MyLogic> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async ValueTask DisposeAsync()
    {
        // @formatter:off
        try { _cts.Cancel(); }
        catch (Exception e) { _logger.LogError(e, "Failed to cancel"); }

        var timerTask = _timerTask;
        if (timerTask != null)
        {
            try { await timerTask; }
            catch (Exception e) { _logger.LogError(e, "Failed to await timer-task"); }
        }

        var eventLoopTask = _eventLoopTask;
        if (eventLoopTask != null)
        {
            try { await eventLoopTask; }
            catch (Exception e) { _logger.LogError(e, "Failed to await event-loop-task"); }
        }
        // @formatter:on
    }

    public async Task Start(CancellationToken startingCancellationToken = default)
    {
        var devices = await DeviceFactory.CreateDevices(_serviceProvider);

        var eventQueue = new BlockingCollection<LogicEvent>();
        RegisterEvents(devices, eventQueue);

        devices.Bathroom.CatScale.Start();

        // @formatter:off
        _timerTask = Task.Run(async () =>
            {
                try { await TimerTaskFunc(eventQueue, _cts.Token); }
                catch (Exception e) { _logger.LogError(e, "Unhandled error"); }
            },
            _cts.Token);

        _eventLoopTask = Task.Run(async () =>
            {
                try { await EventLoopTaskFunc(eventQueue, devices, _logger, _cts.Token); }
                catch (Exception e) { _logger.LogCritical(e, "Unhandled error"); }
            },
            _cts.Token);
        // @formatter:on
    }

    private static void RegisterEvents(Devices devices, BlockingCollection<LogicEvent> eventQueue)
    {
        devices.Bathroom.CatScale.PooCountChanged += (_, e)
            => eventQueue.Add(new PooCountChangedEvent(e.PooCount));

        devices.Kitchen.Button1.Event += (_, e)
            => eventQueue.Add(new ButtonPressEvent("kitchen", e.Action));
        devices.Bedroom.Button1.Event += (_, e)
            => eventQueue.Add(new ButtonPressEvent("bedroom", e.Action));

        devices.Kitchen.OccupancySensor1.Event += (_, e)
            => eventQueue.Add(new OccupancySensorEvent("kitchen", e.Occupancy));

        devices.Virtual.LivingRoomLightMode.ChangeRequest += x
            => eventQueue.Add(new ChangeLivingRoomLightMode(x));
        devices.Virtual.KitchenLightMode.ChangeRequest += x
            => eventQueue.Add(new ChangeKitchenLightMode(x));
        devices.Virtual.BedroomLightMode.ChangeRequest += x
            => eventQueue.Add(new ChangeBedroomLightMode(x));
    }

    private static async Task TimerTaskFunc(BlockingCollection<LogicEvent> eventQueue,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken);
                eventQueue.Add(new TimerTickEvent(), cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private static async Task EventLoopTaskFunc(BlockingCollection<LogicEvent> eventQueue, Devices devices,
        ILogger logger, CancellationToken cancellationToken)
    {
        var state = CreateState();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var @event in eventQueue.GetConsumingEnumerable(cancellationToken))
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        var oldState = state;
                        var newState = ProcessEvent(oldState, @event);

                        if (!oldState.Equals(newState))
                        {
                            oldState.PrintDiffTo(newState, "state");
                            state = newState;
                            await ProcessChangedState(state, devices);
                        }

                        if (@event is not TimerTickEvent)
                        {
                            logger.LogDebug("Processed event {Event} in {Time} ms", @event,
                                sw.ElapsedMilliseconds);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to process event {Event}", @event);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
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

    private static State ProcessEvent(State state, LogicEvent @event)
    {
        switch (@event)
        {
            case TimerTickEvent timerTickEvent:
                return ProcessTimerTickEvent(state, timerTickEvent);

            case PooCountChangedEvent pooCountChangedEvent:
                return ProcessPooCountChangedEvent(state, pooCountChangedEvent);

            case ButtonPressEvent buttonPressEvent:
                return ProcessButtonEvent(state, buttonPressEvent);

            case OccupancySensorEvent occupancySensorEvent:
                return ProcessOccupancySensorEvent(state, occupancySensorEvent);

            case ChangeLivingRoomLightMode changeLivingRoomLightMode:
                state.LivingRoomLightMode = changeLivingRoomLightMode.Mode;
                return state;

            case ChangeKitchenLightMode changeKitchenLightMode:
                state.KitchenLightMode = changeKitchenLightMode.Mode;
                return state;

            case ChangeBedroomLightMode changeBedroomLightMode:
                state.BedroomLightMode = changeBedroomLightMode.Mode;
                return state;

            default:
                Console.WriteLine($"Unknown logic event: {@event} ({@event.GetType().Name})");
                return state;
        }
    }

    // ReSharper disable once UnusedParameter.Local
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

    private static async Task ProcessChangedState(State state, Devices devices)
    {
        await devices.Virtual.LivingRoomLightMode.Update(state.LivingRoomLightMode);
        await devices.Virtual.KitchenLightMode.Update(state.KitchenLightMode);
        await devices.Virtual.BedroomLightMode.Update(state.BedroomLightMode);

        await UpdateLivingRoomLights(state, devices);
        await UpdateKitchenLights(state, devices);
        await UpdateBedroomLights(state, devices);
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
}