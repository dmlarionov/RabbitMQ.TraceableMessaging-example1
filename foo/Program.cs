﻿using System;
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

namespace foo
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
            
            var hostBuilder = new HostBuilder()
                .ConfigureHostConfiguration(hostConfig => hostConfig = configBuilder)
                .ConfigureAppConfiguration((context, appConfig) => appConfig = configBuilder)
                .ConfigureServices((context, services) => {
                    services.AddApplicationInsightsTelemetry();
                    services.AddSingleton<IConnection>(
                        (new ConnectionFactory()
                        {
                            HostName = config["RabbitMQ:Connection:Hostname"],
                            UserName = config["RabbitMQ:Connection:Username"],
                            Password = config["RabbitMQ:Connection:Password"],
                            VirtualHost = config["RabbitMQ:Connection:Vhost"]
                        })
                        .CreateConnection());
                });          
            var host = hostBuilder.Build();

            // configure App Insights instrumentation key through RabbitMQ exchange
            await TelemetryKeyConfigurator.ConfigureAsync(
                config["RabbitMQ:Exchanges:AppInsightsInstrumentationKeyDistribution"],
                host.Services.GetRequiredService<IConnection>());

            await host.RunAsync();

            await Task.Run(() => {
                host.Services.GetService<TelemetryClient>()?.Flush();
                Task.Delay(500).Wait();
            });
        }
    }
}