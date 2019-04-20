using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using lib;
using System.Threading;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using lib.DTO;

namespace bang
{
    class Program
    {
        static readonly string serviceName = "bang";

        static async Task Main(string[] args)
        {
            string basePath = (Debugger.IsAttached) ? Directory.GetCurrentDirectory() : AppDomain.CurrentDomain.BaseDirectory;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            var config = configBuilder.Build();

            // connect to RabbitMQ
            var conn = (new ConnectionFactory()
            {
                HostName = config["RabbitMQ:Connection:Hostname"],
                UserName = config["RabbitMQ:Connection:Username"],
                Password = config["RabbitMQ:Connection:Password"],
                VirtualHost = config["RabbitMQ:Connection:Vhost"]
            }).CreateConnection();

            // token signing key
            byte[] tokenKey = null;

            // configure keys through RabbitMQ exchange
            await KeyDistributor.ConfigureAsync(
                    config["RabbitMQ:Exchanges:KeyDistribution"],
                    conn,
                    keys => {
                        TelemetryConfiguration.Active.InstrumentationKey = keys.AppInsightsInstrumKey;
                        tokenKey = keys.JwtIssuerKey;
                    });

            // set app insights developer mode (remove lags when sending telemetry)
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;

            // set service name in app insights
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new CustomTelemetryInitializer(serviceName));

            // telemetry client instance
            var telemetry = new TelemetryClient();

            // token validation parameters
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(tokenKey)
            };

            // security options for RabbitMQ service
            var securityOptions = new SecurityOptions(
                // map of request types to scope names
                new Dictionary<string, string>()
                {
                    { nameof(Ping1), "1" },
                    { nameof(Ping2), "2" }
                }, 
                tokenValidationParameters);

            // set host configuration
            var hostBuilder = new HostBuilder()
                .ConfigureHostConfiguration(c => c
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true))
                .ConfigureServices((context, services) => {
                    services.AddSingleton<TelemetryClient>(telemetry);
                    services.AddSingleton<SecurityOptions>(securityOptions);
                    services.AddSingleton<IConnection>(conn);
                    services.AddHostedService<BangService>();
                });
            var host = hostBuilder.Build();

            // send ready signal
            await ReadyChecker.SendReadyAsync(config["RabbitMQ:Exchanges:ReadyCheck"], conn, serviceName);

            // run host and wait for shutdown signal
            await host.StartAsync();
            await Shutdowner.ShutdownAsync(config["RabbitMQ:Exchanges:Shutdown"], conn, () => host.StopAsync());

            // flush telemetry
            telemetry?.Flush();
            Task.Delay(500).Wait();
            Console.WriteLine("Bye!");
        }
    }
}
