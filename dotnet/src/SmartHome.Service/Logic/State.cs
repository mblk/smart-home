namespace SmartHome.Service.Logic;

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
    public bool LitterBoxIsDirty;

    public bool KitchenOccupied;
    public int KitchenOccupancyTimeout;
    
    public LivingRoomLightMode LivingRoomLightMode;
    public KitchenLightMode KitchenLightMode;
    public BedroomLightMode BedroomLightMode;

    // ...
    // Sleepmode
    // AtHome
    // ...
}