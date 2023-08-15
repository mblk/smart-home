using SmartHome.Infrastructure.CatScale;
using SmartHome.Infrastructure.Zigbee2Mqtt;

namespace SmartHome.Service;

public class MyLogic : IDisposable
{
    private readonly ILogger<MyLogic> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly CatScaleSensor _catScaleSensor;
    private readonly Z2MConfig _z2MConfig;
    private readonly Z2MLight _light1;
    
    public MyLogic(ILogger<MyLogic> logger)
    {
        _logger = logger;
        _logger.LogInformation("ctor");
        
        _catScaleSensor = new CatScaleSensor(new Uri("https://mblk.info"));
        _z2MConfig = new Z2MConfig("media", 1883);
        _light1 = new Z2MLight(_z2MConfig, "dev/led_01");

        _catScaleSensor.PooCountChanged += async count =>
        {
            _logger.LogInformation("Poo count changed: {Count}", count);

            if (count > 0)
                await _light1.TurnOn();
            else
                await _light1.TurnOff();
        };

        _catScaleSensor.Start();
        
        //_ = Task.Run(Worker);
    }

    public void Dispose()
    {
        _logger.LogInformation("Dispose");
        _cts.Cancel();
        _catScaleSensor.Dispose();
    }

    //private async Task Worker()
    //{
        //while (!_cts.IsCancellationRequested)
        //{
            //await Task.Delay(TimeSpan.FromSeconds(3));
            //Console.WriteLine(".. on");
            //await _light1.TurnOn();
            //await Task.Delay(TimeSpan.FromSeconds(3));
            //Console.WriteLine(".. off");
            //await _light1.TurnOff();
        //}
    //}

    //public void Foo()
    //{
        // aaa
        // catScale.OnDirtyChanged += (e) =>
        // {
        //     if (e.IsDirty)
        //         someLight.TurnOn();
        //     else
        //         someLight.TurnOff();
        // };
        //
        // // bbb
        // someLight.State = catScale.IsDirty;
        //
        // // ccc
        // entityStore.GetEntity("cat_scale_1").OnEvent("...", (e) =>
        // {
        //     entityStore.GetEntity("some_light").SetInput("state",
        //     e.GetOutput("is_dirty") ? "on" : "off");
        // });
    //}
}
