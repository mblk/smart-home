using SmartHome.Service.Logic;

namespace SmartHome.Service.HostedServices;

public class LogicService : IHostedService
{
    private readonly ILogger<LogicService> _logger;
    private readonly AsyncServiceScope _serviceScope;
    
    public LogicService(ILogger<LogicService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
   
        // Resolve all objects in a dedicated scope (rather than using singletons).
        _serviceScope = serviceProvider.CreateAsyncScope();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ... 111");

        var myLogic = _serviceScope.ServiceProvider.GetRequiredService<MyLogic>();

        await myLogic.Start(cancellationToken);
        
        _logger.LogInformation("Started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping ...");

        await _serviceScope.DisposeAsync();
        
        _logger.LogInformation("Stopped");
    }
}