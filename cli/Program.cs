using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using lib;

namespace cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string basePath = (Debugger.IsAttached) ? Directory.GetCurrentDirectory() : AppDomain.CurrentDomain.BaseDirectory;
            
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var config = configBuilder.Build();

            var conn = (new ConnectionFactory()
                {
                    HostName = config["RabbitMQ:Connection:Hostname"],
                    UserName = config["RabbitMQ:Connection:Username"],
                    Password = config["RabbitMQ:Connection:Password"],
                    VirtualHost = config["RabbitMQ:Connection:Vhost"]
                })
                .CreateConnection();

            // configure app insights instrumentation key from CLI through RabbitMQ exchange
            var aiKeyExchange = config["RabbitMQ:Exchanges:AppInsightsInstrumentationKeyDistribution"];
            var aiKeyConfTask = TelemetryKeyConfigurator.ConfigureAsync(aiKeyExchange, conn);

            Console.WriteLine("Please enter your Application Insights instrumentation key: ");
            var aiKey = Console.ReadLine();
            await TelemetryKeyConfigurator.ConfigurePeersAsync(aiKeyExchange, conn, aiKey);
            await aiKeyConfTask;
            // done?
        }
    }
}
