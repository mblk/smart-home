using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartHome.Infrastructure.Influx.Services;

public interface IInfluxService
{
    void WriteSensorData(string deviceId, IReadOnlyDictionary<string, object> values);
}

public record InfluxConfig(string Url, string Token, string Org, string Bucket);

public class InfluxService : IInfluxService
{
    private readonly ILogger<InfluxService> _logger;
    private readonly InfluxConfig _config;

	public InfluxService(ILogger<InfluxService> logger, IConfiguration configuration)
	{
        _logger = logger;

        _config = configuration
            .GetRequiredSection("Influx")
            .Get<InfluxConfig>()
            ?? throw new Exception("Cant get Influx config");

        _logger.LogInformation("InfluxService created, config: {Config}", _config);
	}

	public void WriteSensorData(string deviceId, IReadOnlyDictionary<string, object> values)
	{
        try
        {
            using var client = new InfluxDBClient(_config.Url, _config.Token);

            var point = PointData
                .Measurement("sensor")
                //.Tag("host", "host1")
                .Tag("device", deviceId)
                //.Field("used_percent", 23.43234543)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ms);

            foreach (var (key, value) in values)
            {
                point = point.Field(key, value);
            }

            using var writeApi = client.GetWriteApi();

            writeApi.WritePoint(point, _config.Bucket, _config.Org);
        }
        catch (Exception e)
        {
            Console.WriteLine($"WriteSensorData: {e}");
        }
    }
}
