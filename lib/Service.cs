using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.TraceableMessaging.ApplicationInsights;
using RabbitMQ.TraceableMessaging.ApplicationInsights.Models;
using RabbitMQ.TraceableMessaging.EventArgs;
using RabbitMQ.TraceableMessaging.Json.Options;
using RabbitMQ.TraceableMessaging.Jwt.Models;
using RabbitMQ.TraceableMessaging.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lib
{
    public abstract class Service : IHostedService
    {
        protected readonly RpcServer<JwtSecurityContext> Server;

        public Service(IConnection conn, IConfiguration config, SecurityOptions securityOptions)
        {
            var queue = config["RabbitMQ:QueueToServe"];
            var channel = conn.CreateModel();
            channel.QueueDeclare(queue, false, true, true);
            Server = new RpcServer<JwtSecurityContext>(
                channel,
                new ConsumeOptions(queue),
                new JsonFormatOptions(),
                securityOptions);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Server.Received += OnReceive;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Server.Received -= OnReceive;
            return Task.CompletedTask;
        }

        protected abstract void OnReceive(object sender, RequestEventArgs<TelemetryContext, JwtSecurityContext> ea);
    }
}
