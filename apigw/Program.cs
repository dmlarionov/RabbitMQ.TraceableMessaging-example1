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
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();
            
            if (Debugger.IsAttached)
                configBuilder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);

            var config = configBuilder.Build();

            // connect to RabbitMQ
            var conn = Utils.ConnectRabbitMQ(config);

            // token signing key
            byte[] tokenKey = null;

            // app insights key
            string appInsKey = null;

            // configure keys through RabbitMQ exchange
            await KeyDistributor.ConfigureAsync(
                    config["RabbitMQ:Exchanges:KeyDistribution"],
                    conn,
                    keys => {
                        appInsKey = keys.AppInsightsInstrumKey;
                        tokenKey = keys.JwtIssuerKey;
                    });

            // set app insights developer mode (remove lags when sending telemetry)
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;

            // token validation parameters
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
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
                    services.AddApplicationInsightsTelemetry(appInsKey);
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
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
            Task.Delay(1000).Wait();
            Console.WriteLine("Bye!");
        }
    }
}
