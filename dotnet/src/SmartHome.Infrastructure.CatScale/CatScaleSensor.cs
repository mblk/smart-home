using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartHome.Infrastructure.CatScale;

public class CatScaleSensor : ICatScaleSensor
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private record PooCount
    (
        int ToiletId,
        int Count
    );

    private readonly CatScaleConfig _config;
    private readonly ILogger<CatScaleSensor> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly HttpClient _httpClient;

    public event PooCountChangedEventHandler? PooCountChanged;

    public CatScaleSensor(ILogger<CatScaleSensor> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        _config = configuration.GetRequiredSection("CatScale").Get<CatScaleConfig>()
                  ?? throw new Exception("Cant get cat-scale config");
        
        _logger.LogInformation("CatScaleSensor created, config: {Config}", _config);

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(15), // TODO move to config?
            BaseAddress = _config.Endpoint,
        };
    }

    public void Start()
    {
        _ = Task.Run(Worker);
    }

    public void Dispose()
    {
        _cts.Cancel();
    }

    private async Task Worker()
    {
        var initialPooCount = await GetPooCount();
        InvokePooCountChanged(initialPooCount);

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var stream = await _httpClient.GetStreamAsync("api/ScaleEvent/Subscribe", _cts.Token);
                using var streamReader = new StreamReader(stream);

                while (!streamReader.EndOfStream && !_cts.IsCancellationRequested)
                {
                    var line = await streamReader.ReadLineAsync(_cts.Token);
                    if (String.IsNullOrWhiteSpace(line)) continue;

                    var pooCount = await GetPooCount();
                    InvokePooCountChanged(pooCount);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogDebug("Canceled");
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                _logger.LogInformation("Gateway Time-Out");
            }
            catch (IOException e) when (e.Message == "The response ended prematurely.")
            {
                _logger.LogInformation("The response ended prematurely");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to subscribe");
            }
        }
    }

    private async Task<int> GetPooCount()
    {
        var pooCounts = (await _httpClient.GetFromJsonAsync<PooCount[]>("api/ScaleEvent/GetPooCounts", _cts.Token))!;

        return pooCounts.Sum(pc => pc.Count);
    }

    private void InvokePooCountChanged(int count)
    {
        try
        {
            PooCountChanged?.Invoke(this, new PooCountChangedEventArgs(count));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to invoke event");
        }
    }
}