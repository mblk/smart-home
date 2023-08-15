using System.Net.Http.Json;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using SmartHome.Utils;

namespace SmartHome.ConsoleTests;

public record PooCount
(
    int ToiletId,
    int Count
);

public static class Program
{
    public static async Task Main(string[] args)
    {
        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("media", 1883)
                .Build();

            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var payloadString = Encoding.ASCII.GetString(e.ApplicationMessage.PayloadSegment.Array!);
                Console.WriteLine($"Received {e.ApplicationMessage.Topic}: '{payloadString}'");
                return Task.CompletedTask;
            };
            
            //Console.WriteLine("connecting...");
            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            //Console.WriteLine("connected.");
            //response.DumpToConsole();

            // var subOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            //     .WithTopicFilter(f =>
            //     {
            //         f.WithTopic("zigbee2mqtt/dev/#");
            //     }).Build();
            // await mqttClient.SubscribeAsync(subOptions);

            var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(5),
                BaseAddress = new Uri("http://localhost:5155"),
            };

            while (true)
            {
                try
                {
                    var stream = await httpClient.GetStreamAsync("api/ScaleEvent/Subscribe");
                    using var streamReader = new StreamReader(stream);
                    
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();
                        Console.WriteLine($"event: {line}");

                        var pooCounts =
                            await httpClient.GetFromJsonAsync<PooCount[]>("api/ScaleEvent/GetPooCounts")!;
                        pooCounts.DumpToConsole();
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Cancelled...");
                }
            }

            // while (true)
            // {
            //     await mqttClient.PublishStringAsync("zigbee2mqtt/dev/led_01/set", "{\"state\":\"ON\"}");
            //     await Task.Delay(TimeSpan.FromSeconds(3));
            //     await mqttClient.PublishStringAsync("zigbee2mqtt/dev/led_01/set", "{\"state\":\"OFF\"}");
            //     await Task.Delay(TimeSpan.FromSeconds(3));
            // }

            Console.WriteLine("Press enter to quit ...");
            Console.ReadLine();

            Console.WriteLine("disconnecting...");
            await mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build());
            Console.WriteLine("disconnected.");
        }
    }
}