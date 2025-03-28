namespace SmartHome.Service.Logic;

public enum MasterMode
{
    Awake,
    GoingToBed,
    Sleeping,
    WakingUp,

    Away,
}

public enum LivingRoomLightMode
{
    Off,
    Dim25,
    Dim50,
    Full,
    Auto,
}

public enum KitchenLightMode
{
    Full,
    Auto,
}

public enum BedroomLightMode
{
    Off,
    Dim,
    Full,
}

public struct State
{
    // config
    public TimeOnly WakeUpTime = new TimeOnly(7, 0);
    public double WakeUpPeriod = 15.0d; // Minutes



    public required MasterMode MasterMode;
    public MasterMode? MasterModeOverride;
    public readonly MasterMode EffectiveMasterMode => MasterModeOverride ?? MasterMode;




    public double WakingIntensity;
    public int WakingTicks;




    public bool LitterBoxIsDirty;

    public bool KitchenOccupied;
    public int KitchenOccupancyTimeout;

    public required LivingRoomLightMode LivingRoomLightMode;
    public required KitchenLightMode KitchenLightMode;
    public required BedroomLightMode BedroomLightMode;

    public State()
    {
    }
}