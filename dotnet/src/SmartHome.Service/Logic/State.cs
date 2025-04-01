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

//---

//public enum LivingRoomLightMode
//{
//    Auto,

//    Off,
//    Dim25,
//    Dim50,
//    Dim75,
//    Full,
//}

//public enum KitchenLightMode
//{
//    Full,
//    Auto,
//}

//public enum BedroomLightMode
//{
//    Off,
//    Dim,
//    Full,
//}

public struct State
{
    // config
    public TimeOnly WakeUpTime = new TimeOnly(7, 0);
    public TimeSpan WakeUpPeriod = TimeSpan.FromMinutes(10d);



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