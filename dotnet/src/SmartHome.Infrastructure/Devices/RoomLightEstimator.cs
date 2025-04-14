using SmartHome.Utils;
using System.Diagnostics;

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
    double TotalLightFactor, // 0..1
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

    public static RoomLightEstimate EstimateRoomLight(SunState sunState, RoomLightConfig config)
    {
        var sunElevationRad = sunState.AltDeg.Deg2Rad(); // [-90..+90], realisticly -45..+45 at my location
        var sunDirectionRad = sunState.DirDeg.Deg2Rad(); // [0..360), 0=north
        var windowDirectionRad = config.WindowDirection; // magnetic direction, 0=north

        var alphaRad = sunElevationRad;
        var thetaRad = MathExtensions.NormalizedAngleDiffRad(sunDirectionRad, windowDirectionRad);

        var directLightFactor = Math.Max(0.0, Math.Sin(alphaRad)) * Math.Max(0.0, Math.Cos(thetaRad)); // 0..1
        var diffuseLightFactor = EstimateDiffuseLightFactor(sunState.AltDeg);
        var totalLightFactor = (directLightFactor + diffuseLightFactor).Clamp(0.0, 1.0);

        const double solarIrradiance = 1000.0; // W/m^2
        const double luxPerWatt = 120.0; // approx for sunlight

        var irradiance = totalLightFactor * solarIrradiance * config.WindowArea * config.WindowTransmission; // W/m^2
        var illuminance = irradiance * luxPerWatt; // lx

        return new RoomLightEstimate(
            AlphaRad: alphaRad,
            ThetaRad: thetaRad,
            DirectLightFactor: directLightFactor,
            DiffuseLightFactor: diffuseLightFactor,
            TotalLightFactor: totalLightFactor,
            Irradiance: irradiance,
            Illuminance: illuminance
            );
    }

    private static double EstimateDiffuseLightFactor(double sunAltDeg)
    {
        const double maxDiffuseFactor1 = 0.1;
        const double maxDiffuseFactor2 = 0.4;

        if (sunAltDeg < -6.0)
        {
            return 0.0;
        }
        else if (sunAltDeg < 0.0) // -6 .. 0
        {
            var t = (sunAltDeg + 6.0) / 6.0; // 0..1
            Debug.Assert(0.0 <= t && t <= 1.0);

            return t * maxDiffuseFactor1;
        }
        else // >0
        {
            var t = Math.Min(1.0, sunAltDeg / 30.0); // linear increase 0..1 at 0º..30º
            Debug.Assert(0.0 <= t && t <= 1.0);

            return maxDiffuseFactor1 + t * maxDiffuseFactor2; // 0.1..0.4
        }
    }
}


