using System.Net;
using System.Net.Http.Json;

namespace SmartHome.Infrastructure.CatScale;

public class CatScaleSensor : IDisposable
{
    private record PooCount
    (
        int ToiletId,
        int Count
    );

    private readonly CancellationTokenSource _cts = new();
    private readonly HttpClient _httpClient;

    public event Action<int>? PooCountChanged;

    public CatScaleSensor(Uri endpoint)
    {
        _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(5),
            BaseAddress = endpoint,
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
                Console.WriteLine("CatScaleSensor: Canceled");
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                Console.WriteLine("CatScaleSensor: Gateway Time-out");
            }
            catch (IOException e) when (e.Message == "The response ended prematurely.")
            {
                Console.WriteLine("CatScaleSensor: The response ended prematurely.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"CatScaleSensor: {e}");
            }
        }
    }

    private async Task<int> GetPooCount()
    {
        var pooCounts =
            (await _httpClient.GetFromJsonAsync<PooCount[]>("api/ScaleEvent/GetPooCounts", _cts.Token))!;

        return pooCounts.Sum(pc => pc.Count);
    }

    private void InvokePooCountChanged(int count)
    {
        try
        {
            PooCountChanged?.Invoke(count);
        }
        catch (Exception e)
        {
            Console.WriteLine($"InvokePooCountChanged: {e}");
        }
    }
}