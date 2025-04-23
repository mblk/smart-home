using System.Collections.Concurrent;
using System.Diagnostics;
using SmartHome.Infrastructure.Influx.Services;
using SmartHome.Utils;

namespace SmartHome.Service.Logic;

public class MyLogic : IAsyncDisposable
{
    private static readonly (MasterMode, MasterMode)[] _legalExternalMasterModeTransitions =
    [
        (MasterMode.Awake, MasterMode.GoingToBed),
        (MasterMode.GoingToBed, MasterMode.Awake),

        (MasterMode.Awake, MasterMode.Away),
        (MasterMode.Away, MasterMode.Awake),
    ];

    private readonly CancellationTokenSource _cts = new();

    private readonly ILogger<MyLogic> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IInfluxService _influxService;

    private Task? _timerTask;
    private Task? _eventLoopTask;

    public MyLogic(ILogger<MyLogic> logger, IServiceProvider serviceProvider, IInfluxService influxService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _influxService = influxService;
    }

    public async ValueTask DisposeAsync()
    {
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
    }

    public async Task Start(CancellationToken startingCancellationToken = default)
    {
        _ = startingCancellationToken;

        var devices = await DeviceFactory.CreateDevices(_serviceProvider);

        var eventQueue = new BlockingCollection<LogicEvent>();
        RegisterEvents(devices, eventQueue, _influxService);

        devices.Bathroom.CatScale.Start();
        devices.Virtual.Sun.Start();

        _timerTask = Task.Run(async () =>
            {
                try { await TimerTaskFunc(eventQueue, _cts.Token); }
                catch (Exception e) { _logger.LogError(e, "Unhandled error"); }
            },
            _cts.Token);

        _eventLoopTask = Task.Run(async () =>
            {
                try { await EventLoopTaskFunc(eventQueue, devices, _logger, _cts.Token); }
                catch (Exception e) { _logger.LogError(e, "Unhandled error"); }
            },
            _cts.Token);
    }

    private static void RegisterEvents(Devices devices, BlockingCollection<LogicEvent> eventQueue, IInfluxService influxService)
    {
        devices.Bathroom.CatScale.PooCountChanged += (_, e)
            => eventQueue.Add(new PooCountChangedEvent(e.PooCount));

        devices.Kitchen.Button1.Event += (_, e)
            => eventQueue.Add(new ButtonPressEvent("kitchen", e.Action));
        devices.Bedroom.Button1.Event += (_, e)
            => eventQueue.Add(new ButtonPressEvent("bedroom", e.Action));

        devices.Kitchen.OccupancySensor1.Event += (_, e)
            => eventQueue.Add(new OccupancySensorEvent("kitchen", e.Occupancy));

        devices.Virtual.MasterMode.ChangeRequest += x
            => { if (x != null) { eventQueue.Add(new ChangeMasterMode(x.Value)); } };

        devices.Virtual.LivingRoomLightMode.ChangeRequest += x
            => { if (x != null) { eventQueue.Add(new ChangeLivingRoomLightMode(x.Value)); } };
        devices.Virtual.KitchenLightMode.ChangeRequest += x
            => { if (x != null) { eventQueue.Add(new ChangeKitchenLightMode(x.Value)); } };
        devices.Virtual.BedroomLightMode.ChangeRequest += x
            => { if (x != null) { eventQueue.Add(new ChangeBedroomLightMode(x.Value)); } };

        devices.Virtual.Sun.SunStateChanged += (_, s) =>
        {
            eventQueue.Add(new SunStateChangedEvent(s));

            var values = new Dictionary<string, object>
            {
                { "altitude", s.AltDeg },
                { "direction", s.DirDeg }
            };

            influxService.WriteSensorData("sun", values);
        };

        devices.Virtual.LivingRoomLightEstimator.RoomLightEstimateChanged += (_, s) =>
        {
            eventQueue.Add(new RoomLightEstimateChangedEvent(s));

            var values = new Dictionary<string, object>
            {
                { "alpha", s.AlphaRad },
                { "theta", s.ThetaRad },
                { "direct_factor", s.DirectLightFactor },
                { "diffuse_factor", s.DiffuseLightFactor },
                { "total_factor", s.TotalLightFactor },
                { "irradiance", s.Irradiance },
                { "illuminance", s.Illuminance },
            };

            influxService.WriteSensorData("livingroom/light_estimator", values);
        };

        devices.Virtual.Sensor.DataReceived += (_, s) =>
        {
            influxService.WriteSensorData(s.DeviceId, s.Values);
        };
    }

    private static async Task TimerTaskFunc(BlockingCollection<LogicEvent> eventQueue, CancellationToken cancellationToken)
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

    private static async Task EventLoopTaskFunc(BlockingCollection<LogicEvent> eventQueue, Devices devices, ILogger logger, CancellationToken cancellationToken)
    {
        var state = CreateState();

        state.OutputEvents.Enqueue(new SetVolumeEvent(0.5));
        state.OutputEvents.Enqueue(new StopRadioEvent());
        state.OutputEvents.Enqueue(new SpeakEvent("Hallo Welt. 5, 4, 3, 2, 1, 0. System gestartet. Hahahaha."));

        // Propagate the initial state.
        await ProcessChangedState(state, devices);

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

                        if (newState.OutputEvents.Count > 0)
                        {
                            await ProcessOutputEvents(state.OutputEvents, devices);
                            state.OutputEvents.Clear();
                        }

                        if (@event is not TimerTickEvent)
                        {
                            logger.LogDebug("Processed event {Event} in {Time} ms", @event, sw.ElapsedMilliseconds);
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
            MasterMode = MasterMode.Awake,

            LivingRoomLightMode = LightMode.Auto,
            KitchenLightMode = LightMode.Auto,
            BedroomLightMode = LightMode.Auto,
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

            case ChangeMasterMode changeMasterMode:
                return ProcessChangeMasterModeRequest(state, changeMasterMode.Mode);

            case ChangeLivingRoomLightMode changeLivingRoomLightMode:
                state.LivingRoomLightMode = changeLivingRoomLightMode.Mode;
                return state;

            case ChangeKitchenLightMode changeKitchenLightMode:
                state.KitchenLightMode = changeKitchenLightMode.Mode;
                return state;

            case ChangeBedroomLightMode changeBedroomLightMode:
                state.BedroomLightMode = changeBedroomLightMode.Mode;
                return state;

            case SunStateChangedEvent sunStateChangedEvent:
                state.SunState = sunStateChangedEvent.SunState;
                return state;

            case RoomLightEstimateChangedEvent roomLightEstimateChangedEvent:
                state.LivingRoomLightEstimate = roomLightEstimateChangedEvent.Estimate;
                return state;

            default:
                Console.WriteLine($"Unknown logic event: {@event} ({@event.GetType().Name})");
                return state;
        }
    }

    private static State ProcessChangeMasterModeRequest(State state, MasterMode newMode)
    {
        if (_legalExternalMasterModeTransitions.Any(x => x.Item1 == state.MasterMode && x.Item2 == newMode))
        {
            state = ChangeMasterMode(state, newMode);
        }
        else
        {
            Console.WriteLine($"MasterMode change not allowed: {state.MasterMode} -> {newMode}");
        }

        return state;
    }

    private static State ChangeMasterMode(State state, MasterMode newMode)
    {
        var oldMode = state.MasterMode;

        state.MasterMode = newMode;

        if (newMode == MasterMode.WakingUp)
        {
            state.WakingIntensity = 0d;
            state.WakingTicks = 0;
        }

        // Set everything to auto on master mode-changes.
        //      XXX not sure if this makes sense yet...
        //      XXX maybe the light mode should have more authority than the master mode
        state.LivingRoomLightMode = LightMode.Auto;
        state.KitchenLightMode = LightMode.Auto;
        state.BedroomLightMode = LightMode.Auto;

        switch (newMode)
        {
            case MasterMode.Awake:
                state.OutputEvents.Enqueue(new SpeakEvent("Neuer Modus: Zuhause und wach"));
                break;

            case MasterMode.GoingToBed:
                state.OutputEvents.Enqueue(new SpeakEvent("Neuer Modus: Gehe ins Bett"));
                break;

            case MasterMode.Sleeping:
                state.OutputEvents.Enqueue(new SpeakEvent("Neuer Modus: Schlafe"));
                break;

            case MasterMode.WakingUp:
                state.OutputEvents.Enqueue(new SpeakEvent("Neuer Modus: Wache auf"));
                break;

            case MasterMode.Away:
                state.OutputEvents.Enqueue(new SpeakEvent("Neuer Modus: Abwesend"));
                break;
        }

        if (oldMode != MasterMode.WakingUp && newMode == MasterMode.WakingUp)
        {
            state.OutputEvents.Enqueue(new PlayRadioEvent("http://stream.radioparadise.com/mp3-192"));
        }

        if (oldMode == MasterMode.WakingUp && newMode != MasterMode.WakingUp)
        {
            state.OutputEvents.Enqueue(new StopRadioEvent());
        }

        return state;
    }

    private static State ProcessTimerTickEvent(State state, TimerTickEvent e)
    {
        _ = e;

        if (state.MasterMode == MasterMode.Sleeping)
        {
            var timeNow = TimeOnly.FromDateTime(DateTime.Now);
            var wakeTime = state.WakeUpTime;

            if (wakeTime <= timeNow && timeNow < wakeTime.AddMinutes(5))
            {
                state = ChangeMasterMode(state, MasterMode.WakingUp);
            }
        }

        if (state.MasterMode == MasterMode.WakingUp)
        {
            var timeNow = TimeOnly.FromDateTime(DateTime.Now);
            var wakingTime = timeNow - state.WakeUpTime;

            state.WakingIntensity = (wakingTime / state.WakeUpPeriod).Clamp(0d, 1d);
            state.WakingTicks++;
        }

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
        static LightMode getNextLightMode(LightMode currentMode, LightMode maxMode)
        {
            if (currentMode == LightMode.Auto)
                return maxMode;
            return LightMode.Auto;
        }

        switch (e.Button)
        {
            case "kitchen":
                if (state.MasterMode == MasterMode.WakingUp)
                {
                    state = ChangeMasterMode(state, MasterMode.Awake);
                }
                else
                {
                    switch (e.Action)
                    {
                        case "button_1_single":
                            state.LivingRoomLightMode = getNextLightMode(state.LivingRoomLightMode, LightMode.Full);
                            break;

                        case "button_2_single":
                            state.KitchenLightMode = getNextLightMode(state.KitchenLightMode, LightMode.Full);
                            break;

                        case "button_3_single":
                            if (state.MasterMode == MasterMode.Awake)
                            {
                                state = ChangeMasterMode(state, MasterMode.GoingToBed);
                            }
                            else if (state.MasterMode == MasterMode.GoingToBed)
                            {
                                state = ChangeMasterMode(state, MasterMode.Awake);
                            }
                            break;

                        case "button_4_single":
                            // XXX
                            state.OutputEvents.Enqueue(new SpeakEvent("Hallo. Test. 1 2 3 4 5. Bis dann. Ende."));
                            // XXX
                            break;
                    }
                }

                break;

            case "bedroom":
                switch (e.Action)
                {
                    case "button_1_single":
                        state.BedroomLightMode = getNextLightMode(state.BedroomLightMode, LightMode.Dim50);
                        break;

                    case "button_2_single":
                        break;

                    case "button_3_single":
                        if (state.MasterMode == MasterMode.Awake)
                        {
                            state = ChangeMasterMode(state, MasterMode.GoingToBed);
                        }
                        else if (state.MasterMode == MasterMode.GoingToBed)
                        {
                            state = ChangeMasterMode(state, MasterMode.Awake);
                        }
                        break;

                    case "button_4_single":
                        if (state.MasterMode == MasterMode.GoingToBed)
                        {
                            state = ChangeMasterMode(state, MasterMode.Sleeping);
                        }
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
        await devices.Virtual.MasterMode.Update(state.MasterMode);

        await devices.Virtual.LivingRoomLightMode.Update(state.LivingRoomLightMode);
        await devices.Virtual.KitchenLightMode.Update(state.KitchenLightMode);
        await devices.Virtual.BedroomLightMode.Update(state.BedroomLightMode);

        await UpdateLivingRoomLights(state, devices);
        await UpdateKitchenLights(state, devices);
        await UpdateBedroomLights(state, devices);   
    }

    private static async Task ProcessOutputEvents(IEnumerable<OutputEvent> outputEvents, Devices devices)
    {
        foreach (var outputEvent in outputEvents)
        {
            Console.WriteLine($"Output event: {outputEvent}");

            try
            {
                switch (outputEvent)
                {
                    case SpeakEvent speakEvent:
                        await devices.Virtual.RemoteAudioPlayer.Speak(speakEvent.Text);
                        break;

                    case PlayRadioEvent playRadioEvent:
                        await devices.Virtual.RemoteAudioPlayer.PlayRadio(playRadioEvent.Uri);
                        break;

                    case StopRadioEvent stopRadioEvent:
                        await devices.Virtual.RemoteAudioPlayer.StopRadio();
                        break;

                    case SetVolumeEvent setVolumeEvent:
                        await devices.Virtual.RemoteAudioPlayer.SetVolume(setVolumeEvent.Volume);
                        break;

                    default:
                        Console.WriteLine($"Unknown output event: {outputEvent} ({outputEvent.GetType().Name})");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to process output event {outputEvent}: {e}");
            }
        }
    }

    private static double CalculateLivingRoomAutoLightLevel(State state)
    {
        const double threshold = 0.25; // slowly turn on lights below this factor
        const double maxBrightness = 0.5;

        if (state.LivingRoomLightEstimate == null)
            return 0.0;

        var f = state.LivingRoomLightEstimate.TotalLightFactor; // 0..1

        if (f >= threshold)
            return 0.0;
        
        var t = (threshold - f) / threshold; // 0..1
        Debug.Assert(0.0 <= t && t <= 1.0);

        return t * maxBrightness; // 0..0.5
    }

    private static async Task UpdateLivingRoomLights(State state, Devices devices)
    {
        double getAwakeBrightness()
        {
            double autoLevel = CalculateLivingRoomAutoLightLevel(state);

            return state.LivingRoomLightMode.GetBrightness(autoLevel);
        }

        double targetBrightness = state.MasterMode switch
        {
            MasterMode.Away => 0.0,
            MasterMode.Awake => getAwakeBrightness(),
            MasterMode.GoingToBed => 0.0,
            MasterMode.Sleeping => 0.0,
            MasterMode.WakingUp => 0.0,
            _ => 0.0,
        };

        if (targetBrightness > 0.01)
        {
            await devices.LivingRoom.CeilingLight1.TurnOn(brightness: targetBrightness);
            await devices.LivingRoom.CeilingLight2.TurnOn(brightness: targetBrightness);
            await devices.LivingRoom.CeilingLight3.TurnOn(brightness: targetBrightness);
            await devices.LivingRoom.DeskLight.TurnOn(brightness: targetBrightness);
            await devices.LivingRoom.Standing.TurnOn(brightness: targetBrightness);
        }
        else
        {
            await devices.LivingRoom.CeilingLight1.TurnOff();
            await devices.LivingRoom.CeilingLight2.TurnOff();
            await devices.LivingRoom.CeilingLight3.TurnOff();
            await devices.LivingRoom.DeskLight.TurnOff();
            await devices.LivingRoom.Standing.TurnOff();
        }

        if (state.LitterBoxIsDirty && state.MasterMode == MasterMode.Awake)
        {
            await devices.LivingRoom.TvLight.TurnOn(brightness: 0.5, color: (0.66, 0.34));
        }
        else if (targetBrightness > 0.01)
        {
            await devices.LivingRoom.TvLight.TurnOn(brightness: targetBrightness);
        }
        else
        {
            await devices.LivingRoom.TvLight.TurnOff();
        }
    }

    private static async Task UpdateKitchenLights(State state, Devices devices)
    {
        double getAwayBrightness()
        {
            double f = state.LivingRoomLightEstimate?.TotalLightFactor ?? 0.0;
            if (f > 0.25)
                return 0.0;
            return 0.25; // cat-light
        }

        double getAwakeBrightness()
        {
            double autoLevel = (state.KitchenOccupied || state.KitchenOccupancyTimeout > 0) ? 0.5 : 0.0;

            return state.KitchenLightMode.GetBrightness(autoLevel);
        }

        double targetBrightness = state.MasterMode switch
        {
            MasterMode.Away => getAwayBrightness(),
            MasterMode.Awake => getAwakeBrightness(),
            MasterMode.GoingToBed => 0.25,
            MasterMode.Sleeping => 0.0,
            MasterMode.WakingUp => 0.25,
            _ => 0.0,
        };

        if (targetBrightness > 0.01)
        {
            await devices.Kitchen.CeilingLight1.TurnOn(brightness: targetBrightness);
            await devices.Kitchen.CeilingLight2.TurnOn(brightness: targetBrightness);
            await devices.Kitchen.CeilingLight3.TurnOn(brightness: targetBrightness);
            await devices.Kitchen.CeilingLight4.TurnOn(brightness: targetBrightness);
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
        double getWakingUpBrightness()
        {
            if (state.WakingIntensity < 0.99)
            {
                return state.WakingIntensity;
            }
            else
            {
                return (state.WakingTicks % 2 == 0) ? 1.0 : 0.0;
            }
        }

        double targetTemperature = state.MasterMode switch
        {
            MasterMode.WakingUp => 0.0, // cold
            _ => 1.0, // warm
        };

        double targetBrightness = state.MasterMode switch
        {
            MasterMode.Away => 0.0,
            MasterMode.Awake => state.BedroomLightMode.GetBrightness(0.0),
            MasterMode.GoingToBed => 0.25,
            MasterMode.Sleeping => 0.0,
            MasterMode.WakingUp => getWakingUpBrightness(),
            _ => 0.0,
        };

        if (targetBrightness > 0.01)
        {
            await devices.Bedroom.StandLight1.TurnOn(brightness: targetBrightness, temperature: targetTemperature);
            await devices.Bedroom.StandLight2.TurnOn(brightness: targetBrightness, temperature: targetTemperature);
        }
        else
        {
            await devices.Bedroom.StandLight1.TurnOff();
            await devices.Bedroom.StandLight2.TurnOff();
        }
    }
}