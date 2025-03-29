using SmartHome.Infrastructure.Mqtt.Connector;
using SmartHome.UI.Blazor.Components;
using SmartHome.UI.Blazor.Services;

namespace SmartHome.UI.Blazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents(opt =>
                {
                    // Default is 3min. Set shorter timeout so the Dispose-events of pages/components are called sooner.
                    opt.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(10);
                });

            builder.Services.AddScoped<IMqttConnector, MqttConnector>();
            builder.Services.AddScoped<IDataService, DataService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
