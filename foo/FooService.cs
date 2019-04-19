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

namespace foo
{
    public sealed class FooService : Service
    {
        private readonly RpcClient barClient;

        public FooService(
            IConnection conn,
            IConfiguration config,
            SecurityOptions securityOptions)
                : base(conn, config, securityOptions)
        {
            var channel = conn.CreateModel();
            var queue = $"foo-bar-reply-{Guid.NewGuid().ToString()}";
            channel.QueueDeclare(queue);
            barClient = new RpcClient(
                channel,
                new PublishOptions(config["RabbitMQ:Services:Bar"]),
                new ConsumeOptions(queue),
                new JsonFormatOptions());
        }

        protected override void OnReceive(object sender, RequestEventArgs<TelemetryContext, JwtSecurityContext> ea)
        {
            Console.WriteLine($"{ea.RequestType} received at {DateTime.Now}");
            // foo just retransmits requests and replies to and from bar
            switch (ea.RequestType)
            {
                case nameof(Ping1):
                    Server.Reply(ea.CorrelationId, 
                        barClient.GetReply<Pong1>(
                            ea.GetRequest<Ping1>(), 
                            ea.Security?.AccessTokenEncoded));
                    break;

                case nameof(Ping2):
                    Server.Reply(ea.CorrelationId, 
                        barClient.GetReply<Pong2>(
                            ea.GetRequest<Ping2>(), 
                            ea.Security?.AccessTokenEncoded));
                    break;

                default:
                    Server.Reply(ea.CorrelationId,
                        new Reply { Status = ReplyStatus.Fail });
                    break;
            }
        }
    }
}
