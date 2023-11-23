using SmartHome.Service;
using SmartHome.Service.Logic;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddTransient<MyLogic>();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
