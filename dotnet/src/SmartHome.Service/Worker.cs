namespace SmartHome.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MyLogic _myLogic;

    public Worker(ILogger<Worker> logger, MyLogic myLogic)
    {
        _logger = logger;
        _logger.LogInformation("ctor");
        _myLogic = myLogic;
    }

    public override void Dispose()
    {
        _logger.LogInformation("Dispose");
        _myLogic.Dispose();
        base.Dispose();
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartAsync");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync");
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecuteAsync");
        while (!stoppingToken.IsCancellationRequested)
        {
            //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
