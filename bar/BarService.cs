using lib;
using lib.DTO;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.TraceableMessaging.ApplicationInsights;
using RabbitMQ.TraceableMessaging.ApplicationInsights.Models;
using RabbitMQ.TraceableMessaging.EventArgs;
using RabbitMQ.TraceableMessaging.Json.Options;
using RabbitMQ.TraceableMessaging.Jwt.Models;
using RabbitMQ.TraceableMessaging.Models;
using RabbitMQ.TraceableMessaging.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace bar
{
    public sealed class BarService : Service
    {
        private readonly RpcClient bangClient;
        private readonly RpcClient fibClient;

        public BarService(
            IConnection conn,
            IConfiguration config,
            SecurityOptions securityOptions)
                : base(conn, config, securityOptions)
        {
            var bangChannel = conn.CreateModel();
            var bangQueue = $"bang-bar-reply-{Guid.NewGuid().ToString()}";
            bangChannel.QueueDeclare(bangQueue);
            bangClient = new RpcClient(
                bangChannel,
                new PublishOptions(config["RabbitMQ:Services:Bang"]),
                new ConsumeOptions(bangQueue),
                new JsonFormatOptions());

            var fibChannel = conn.CreateModel();
            var fibQueue = $"fib-bar-reply-{Guid.NewGuid().ToString()}";
            fibChannel.QueueDeclare(fibQueue);
            fibClient = new RpcClient(
                fibChannel,
                new PublishOptions(config["RabbitMQ:Services:Fib"]),
                new ConsumeOptions(fibQueue),
                new JsonFormatOptions());
        }

        protected override void OnReceive(object sender, RequestEventArgs<TelemetryContext, JwtSecurityContext> ea)
        {
            Console.WriteLine($"{ea.RequestType} received at {DateTime.Now}");
            // bar makes new requests to bang and fib, awaits for them and then replies
            switch (ea.RequestType)
            {
                case nameof(Ping1):
                    bangClient.GetReply<Pong1>(new Ping1(), ea.Security?.AccessTokenEncoded);
                    fibClient.GetReply<Pong1>(new Ping1(), ea.Security?.AccessTokenEncoded);
                    Server.Reply(ea.CorrelationId, new Pong1());
                    break;

                case nameof(Ping2):
                    Task.WhenAll(
                        fibClient.GetReplyAsync<Pong2>(new Ping2(), ea.Security?.AccessTokenEncoded),
                        bangClient.GetReplyAsync<Pong2>(new Ping2(), ea.Security?.AccessTokenEncoded)
                    ).Wait();
                    Server.Reply(ea.CorrelationId, new Pong2());
                    break;

                default:
                    Server.Reply(ea.CorrelationId, new Reply { Status = ReplyStatus.Fail });
                    break;
            }
        }
    }
}
