using SmartHome.Service.Logic;
using System.Globalization;

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
        _logger.LogInformation("Starting ...");

        {
            var t = DateTime.Now;
            _logger.LogInformation("DateTime.Now {time} | {time2} Kind {kind} Culture {culture} UICulture {uiCulture}",
                t, t.ToString(), t.Kind, CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        }

        var myLogic = _serviceScope.ServiceProvider.GetRequiredService<MyLogic>();

        try
        {
            await myLogic.Start(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to start: {Exception}", e);
            throw;
        }
        
        _logger.LogInformation("Started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping ...");

        await _serviceScope.DisposeAsync();
        
        _logger.LogInformation("Stopped");
    }
}