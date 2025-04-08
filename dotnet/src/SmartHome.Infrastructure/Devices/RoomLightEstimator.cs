using SmartHome.Utils;

namespace SmartHome.Infrastructure.Devices;

public interface IRoomLightEstimator
{
    event RoomLightEstimateChangedEventHandler RoomLightEstimateChanged;
}

public delegate void RoomLightEstimateChangedEventHandler(object sender, RoomLightEstimate newEstimate);

public record RoomLightConfig(
    double WindowDirection,
    double WindowArea, // m^2
    double WindowTransmission // 0..1
    );

public record RoomLightEstimate(
    double AlphaRad,
    double ThetaRad,
    double DirectLightFactor, // 0..1
    double DiffuseLightFactor, // 0..1
    double Irradiance, // W/m^2
    double Illuminance // Lx
    );

public class RoomLightEstimator : IRoomLightEstimator
{
    private readonly RoomLightConfig _config = new RoomLightConfig(90.0, 2.0, 0.6); // XXX pass from outside
    private readonly ISunSensor _sunSensor;

    public event RoomLightEstimateChangedEventHandler? RoomLightEstimateChanged;

    public RoomLightEstimator(ISunSensor sunSensor)
	{
        _sunSensor = sunSensor;

        _sunSensor.SunStateChanged += SunSensor_SunStateChanged;
    }

    private void SunSensor_SunStateChanged(object sender, SunState newSunState)
    {
        var estimate = EstimateRoomLight(newSunState, _config);

        RaiseRoomLightEstimateChanged(estimate);
    }

    private void RaiseRoomLightEstimateChanged(RoomLightEstimate estimate)
    {
        try
        {
            RoomLightEstimateChanged?.Invoke(this, estimate);
        }
        catch (Exception e)
        {
            Console.WriteLine($"RaiseRoomLightEstimateChanged: {e}");
        }
    }

    private static RoomLightEstimate EstimateRoomLight(SunState sunState, RoomLightConfig config)
    {
        var sunElevationRad = sunState.AltDeg.Deg2Rad(); // -90..+90, realisticly -45..+45 at my location
        var sunDirectionRad = sunState.DirDeg.Deg2Rad(); // [0..360), 0=north
        var windowDirectionRad = config.WindowDirection; // magnetic direction, 0=north

        var alphaRad = sunElevationRad;
        var thetaRad = MathExtensions.NormalizedAngleDiffRad(sunDirectionRad, windowDirectionRad);

        var directLightFactor = Math.Max(0.0, Math.Sin(alphaRad)) * Math.Max(0.0, Math.Cos(thetaRad)); // 0..1
        var diffuseLightFactor = Math.Max(0, 0.1 + 0.2 * Math.Sin(alphaRad)); // 0..0.3

        var solarIrradiance = 1000.0; // W/m^2

        var roomIrradiance = (directLightFactor + diffuseLightFactor) * solarIrradiance * config.WindowArea * config.WindowTransmission; // W/m^2

        const double luxPerWatt = 120.0; // approx for sunlight
        var illuminance = roomIrradiance * luxPerWatt; // lx

        return new RoomLightEstimate(
            AlphaRad: alphaRad,
            ThetaRad: thetaRad,
            DirectLightFactor: directLightFactor,
            DiffuseLightFactor: diffuseLightFactor,
            Irradiance: roomIrradiance,
            Illuminance: illuminance
            );
    }


}


