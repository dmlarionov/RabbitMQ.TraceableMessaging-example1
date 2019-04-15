using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using lib;
using System.Collections.Generic;

namespace cli
{
    class Program
    {
        static IHost host;

        static async Task Main(string[] args)
        {
            string basePath = (Debugger.IsAttached) ? Directory.GetCurrentDirectory() : AppDomain.CurrentDomain.BaseDirectory;
            
            // build config
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var config = configBuilder.Build();

            // connect to RabbitMQ
            var conn = (new ConnectionFactory()
                {
                    HostName = config["RabbitMQ:Connection:Hostname"],
                    UserName = config["RabbitMQ:Connection:Username"],
                    Password = config["RabbitMQ:Connection:Password"],
                    VirtualHost = config["RabbitMQ:Connection:Vhost"]
                })
                .CreateConnection();

            // wait peers to be ready
            var waitPeers = ReadyChecker.WaitPeersAsync(config["RabbitMQ:Exchanges:ReadyCheck"], conn, new List<string> { "apigw", "bang", "bar", "fib", "foo" });

            // configure App Insights instrumentation key from CLI to self and to peers
            var keyExchange = config["RabbitMQ:Exchanges:AppInsightsInstrumentationKeyDistribution"];
            var configureSelf = TelemetryKeyConfigurator.ConfigureAsync(keyExchange, conn);     // task to configure self through exchange
            var configurePeersCancel = new CancellationTokenSource();
            var configurePeers = Task.Run(async () =>                                           // task to read key and send it to exchange
            {
                Console.WriteLine("Please enter your Application Insights instrumentation key: ");
                var aiKey = Console.ReadLine();
                await TelemetryKeyConfigurator.ConfigurePeersAsync(keyExchange, conn, aiKey);
            }, configurePeersCancel.Token);
            await configureSelf.ContinueWith(r =>
            {
                // if we got key from exchange no matter how (may be not from aiConfigurePeersTask)
                // then cancel aiConfigurePeersTask (if it's still running)
                configurePeersCancel.Cancel();
            });

            // build host
            var hostBuilder = new HostBuilder()
                .ConfigureHostConfiguration(hostConfig => hostConfig = configBuilder)
                .ConfigureAppConfiguration((context, appConfig) => appConfig = configBuilder)
                .ConfigureServices((context, services) => {
                    services.AddApplicationInsightsTelemetry();
                    services.AddHttpClient();
                });
            host = hostBuilder.Build();

#pragma warning disable 4014

            // run host
            var hostCancellation = new CancellationTokenSource();

            host.RunAsync(hostCancellation.Token);

            if (await Task.WhenAny(waitPeers, Task.Delay(2000)) == waitPeers)
            {
                // Task completed within timeout.
                // Consider that the task may have faulted or been canceled.
                // We re-await the task so that any exceptions/cancellation is rethrown.
                await waitPeers;

                // perform demo scenarios based on user choise
                switch (GetCase())
                {
                    case '1':
                        RunCase1();
                        break;
                    case '2':
                        RunCase2();
                        break;
                    case '3':
                        RunCase3();
                        break;
                    case '4':
                        RunCase4();
                        break;
                    case 'q':
                        Console.WriteLine("Bye!");
                        host.Services.GetRequiredService<TelemetryClient>().Flush();
                        //host.StopAsync();
                        Task.Delay(500).Wait();
                        hostCancellation.Cancel();
                        return;
                }
            }
            else
            {
                // timeout/cancellation logic
                Console.WriteLine();
                Console.WriteLine("Sorry, but some services are not ready for this demo. Check if docker is running all containters of docker-compose.yml.");
                //host.StopAsync();
                hostCancellation.Cancel();
                return;
            }
            
#pragma warning restore 4014
            
        }

        static char GetCase()
        {
            Console.WriteLine();
            Console.WriteLine("Which scenario should we test with API gateway?: ");
            Console.WriteLine("1) HTTP GET by unauthorized user (401 Unauthorized propagated from one of RabbitMQ services)");
            Console.WriteLine("2) HTTP GET by authorized unprivileged user (403 Forbidden propagated from one of RabbitMQ services)");
            Console.WriteLine("3) HTTP GET by authorized privileged user (200 OK)");
            Console.WriteLine("4) HTTP GET by authorized privileged unlucky user (500 Internal Server Error due to failure in one of services)");
            Console.WriteLine();
            Console.WriteLine("Each HTTP call to API gateway turnes into series of RabbitMQ calls. You can see a distributed trace in your Application Insights instance in Azure portal. You can test scenarios in any order.");
            Console.WriteLine();
            Console.WriteLine("To quit press 'q'.");
            Console.WriteLine();

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
                switch (key.KeyChar)
                {
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case 'q':
                        return key.KeyChar;
                    default:
                        Console.WriteLine("Sorry, inappropriate choice. Only 'q' and scenario number allowed.");
                        break;
                }
            }
            while (true);
        }

        static void RunCase1()
        {

        }

        static void RunCase2()
        {

        }

        static void RunCase3()
        {

        }

        static void RunCase4()
        {

        }
    }
}
