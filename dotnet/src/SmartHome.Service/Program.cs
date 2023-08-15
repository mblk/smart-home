using SmartHome.Service;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddTransient<MyLogic>();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
