using SmartHome.Infrastructure.Devices;

namespace SmartHome.Service.Logic;

public enum MasterMode
{
    Awake,
    GoingToBed,
    Sleeping,
    WakingUp,

    Away,
}

// "Tagged Union"-style
//public abstract record LightMode();
//public record LightMode_Auto() : LightMode();
//public record LightMode_Off() : LightMode();
//public record LightMode_Manual(double Level) : LightMode();

public enum LightMode
{
    Auto,

    Off,
    Dim25,
    Dim50,
    Dim75,
    Full,
}

public static class LightModeExtensions
{
    public static double GetBrightness(this LightMode mode, double autoLevel)
    {
        return mode switch
        {
            LightMode.Auto => autoLevel,

            LightMode.Off => 0.0,
            LightMode.Dim25 => 0.25,
            LightMode.Dim50 => 0.5,
            LightMode.Dim75 => 0.75,
            LightMode.Full => 1.0,

            _ => 0.0,
        };
    }
}


public struct State
{
    // config
    public TimeOnly WakeUpTime = new TimeOnly(7, 0);
    public TimeSpan WakeUpPeriod = TimeSpan.FromMinutes(20d);


    // dynamically calculated values
    public SunState? SunState = null;
    public RoomLightEstimate? LivingRoomLightEstimate = null;





    public required MasterMode MasterMode;
    //public MasterMode? MasterModeOverride;
    //public readonly MasterMode EffectiveMasterMode => MasterModeOverride ?? MasterMode;




    public double WakingIntensity;
    public int WakingTicks;




    public bool LitterBoxIsDirty;

    public bool KitchenOccupied;
    public int KitchenOccupancyTimeout;

    public required LightMode LivingRoomLightMode;
    public required LightMode KitchenLightMode;
    public required LightMode BedroomLightMode;

    public State()
    {
    }
}