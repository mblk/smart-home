using SmartHome.Utils;
using SunCalcNet;
using SunCalcNet.Model;

namespace SmartHome.Infrastructure.Devices;

public interface ISunSensor
{
    void Start();

    event SunStateChangedEventHandler SunStateChanged;
}

public delegate void SunStateChangedEventHandler(object sender, SunState newSunState);

public record SunState(
    double AltDeg,
    double DirDeg,
    TimeOnly SunriseTimeLocal,
    TimeOnly SolarNoonTimeLocal,
    TimeOnly SunsetTimeLocal);

public record SunSensorConfig(double Lat, double Long);

public class SunSensor : ISunSensor
{
    private readonly SunSensorConfig _config = new SunSensorConfig(49.5979564034886, 10.958495656572843); // TODO get from appsettings

    private bool _running = true;

    public event SunStateChangedEventHandler? SunStateChanged;

	public SunSensor()
	{
    }

    public void Start()
    {
        _ = Task.Run(Worker);
    }

    private async Task Worker()
    {
        while (_running)
        {
            try
            {
                var sunState = GetSunState(_config, DateTime.Now);

                RaiseSunStateChangedEvent(sunState);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
            }

            await Task.Delay(TimeSpan.FromSeconds(30)); // TODO set via config?
        }
    }

    private void RaiseSunStateChangedEvent(SunState sunState)
    {
        try
        {
            SunStateChanged?.Invoke(this, sunState);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to raise event: {e}");
        }
    }

    public static SunState GetSunState(SunSensorConfig config, DateTime time)
    {
        // GetSunPosition() Returns:
        // altitude: sun altitude above the horizon in radians, e.g. 0 at the horizon and PI / 2 at the zenith(straight over your head)
        // azimuth: sun azimuth in radians(direction along the horizon, measured from south to west), e.g. 0 is south and Math.PI * 3 / 4 is northwest

        var sunPos = SunCalc.GetSunPosition(time, config.Lat, config.Long);

        var sunAltDeg = sunPos.Altitude.Rad2Deg();
        var sunDirDeg = (sunPos.Azimuth.Rad2Deg() + 180.0).Normalize(0.0, 360.0); // this is now the direction to the sun.

        var sunPhases = SunCalc.GetSunPhases(time, config.Lat, config.Long).ToArray();

        var sunriseDateTime = sunPhases.Single(x => x.Name.Value == SunPhaseName.Sunrise.Value).PhaseTime.ToLocalTime();
        var solarNoonDateTime = sunPhases.Single(x => x.Name.Value == SunPhaseName.SolarNoon.Value).PhaseTime.ToLocalTime();
        var sunsetDateTime = sunPhases.Single(x => x.Name.Value == SunPhaseName.Sunset.Value).PhaseTime.ToLocalTime();

        var sunriseTime = TimeOnly.FromDateTime(sunriseDateTime);
        var solarNoonTime = TimeOnly.FromDateTime(solarNoonDateTime);
        var sunsetTime = TimeOnly.FromDateTime(sunsetDateTime);

        return new SunState(
            AltDeg: sunAltDeg,
            DirDeg: sunDirDeg,
            SunriseTimeLocal: sunriseTime,
            SolarNoonTimeLocal: solarNoonTime,
            SunsetTimeLocal: sunsetTime
            );
    }
}
