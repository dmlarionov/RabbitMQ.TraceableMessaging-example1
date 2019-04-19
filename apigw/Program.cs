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
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using lib;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Builder;

namespace apigw
{
    class Program
    {
        static readonly string serviceName = "apigw";

        static async Task Main(string[] args)
        {
            string basePath = (Debugger.IsAttached) ? Directory.GetCurrentDirectory() : AppDomain.CurrentDomain.BaseDirectory;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            if (Debugger.IsAttached)
                configBuilder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);

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

            // token validation parameters
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(tokenKey)
            };

            // set host configuration
            var hostBuilder = new WebHostBuilder()
                .UseConfiguration(config)
                .ConfigureLogging(logging =>
                {
                    logging.AddConfiguration(config.GetSection("Logging"));
                    logging.AddConsole();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConnection>(conn);
                    services.AddSingleton<ITelemetryInitializer>(new CustomTelemetryInitializer("apigw"));
                    services.AddApplicationInsightsTelemetry();
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.Audience = "http://localhost:5001";
                            options.TokenValidationParameters = tokenValidationParameters;
                            options.SaveToken = true;
                        });
                    services.AddMvc(options => options.Filters.Add(new ExceptionFilter()));
                })
                .Configure(app => 
                {
                    app.UseAuthentication();
                    app.UseMvc();
                })
                .UseKestrel();
            var host = hostBuilder.Build();
            
            // send ready signal
            await ReadyChecker.SendReadyAsync(config["RabbitMQ:Exchanges:ReadyCheck"], conn, serviceName);

            // run host and wait for shutdown signal
            await host.StartAsync();
            await Shutdowner.ShutdownAsync(config["RabbitMQ:Exchanges:Shutdown"], conn, () => host.StopAsync());

            // telemetry client instance
            var telemetry = host.Services.GetService<TelemetryClient>();

            // flush telemetry
            telemetry?.Flush();
            Task.Delay(500).Wait();
            Console.WriteLine("Bye!");
        }
    }
}
