using SmartHome.Infrastructure.CatScale;
using SmartHome.Infrastructure.Zigbee2Mqtt;

namespace SmartHome.Service.Logic;

public class Configuration
{
    public required CatScaleConfig CatScaleConfig;
    public required Z2MConfig Z2MConfig;
}