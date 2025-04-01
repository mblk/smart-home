using SmartHome.Infrastructure.CatScale;
using SmartHome.Infrastructure.Influx.Services;
using SmartHome.Infrastructure.Mqtt.Connector;
using SmartHome.Infrastructure.Zigbee2Mqtt.Discovery;
using SmartHome.Service.HostedServices;
using SmartHome.Service.Logic;

AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
{
    Console.WriteLine($"UnhandledException: {eventArgs.ExceptionObject}");
};

TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
{
    Console.WriteLine($"UnobservedTaskException: {eventArgs.Exception}");
};

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddScoped<ICatScaleSensor, CatScaleSensor>();
    services.AddScoped<IMqttConnector, MqttConnector>();
    services.AddScoped<IZ2MDeviceDiscoverer, Z2MDeviceDiscoverer>();
    services.AddScoped<IInfluxService, InfluxService>();
    services.AddScoped<MyLogic>();

    services.AddHostedService<LogicService>();
});

var host = builder.Build();

host.Run();