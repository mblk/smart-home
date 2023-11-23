using SmartHome.Service.Logic;

namespace SmartHome.Service;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly MyLogic _myLogic;

    public Worker(ILogger<Worker> logger, MyLogic myLogic)
    {
        _logger = logger;
        _logger.LogInformation("ctor");
        _myLogic = myLogic;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartAsync");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync");
        return Task.CompletedTask;
    }
}
