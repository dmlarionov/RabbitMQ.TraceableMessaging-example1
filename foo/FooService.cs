using lib;
using lib.DTO;
using Microsoft.ApplicationInsights;
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
        private readonly RpcClient _barClient;
        private readonly TelemetryClient _telemetry;

        public FooService(
            IConnection conn,
            IConfiguration config,
            SecurityOptions securityOptions,
            TelemetryClient telemetry)
                : base(conn, config, securityOptions)
        {
            var channel = conn.CreateModel();
            var queue = $"foo-bar-reply-{Guid.NewGuid().ToString()}";
            channel.QueueDeclare(queue);
            _barClient = new RpcClient(
                channel,
                new PublishOptions(config["RabbitMQ:Services:Bar"]),
                new ConsumeOptions(queue),
                new JsonFormatOptions());

            _telemetry = telemetry;
        }

        protected override void OnReceive(object sender, RequestEventArgs<TelemetryContext, JwtSecurityContext> ea)
        {
            Console.WriteLine($"{ea.RequestType} received at {DateTime.Now}");

            try
            {
                // foo just retransmits requests and replies to and from bar
                switch (ea.RequestType)
                {
                    case nameof(Ping1):
                        Server.Reply(ea.CorrelationId,
                            _barClient.GetReply<Pong1>(
                                ea.GetRequest<Ping1>(),
                                ea.Security?.AccessTokenEncoded));
                        break;

                    case nameof(Ping3):
                        Server.Reply(ea.CorrelationId,
                            new Pong3(_barClient.GetReply<Pong4>(
                                ea.GetRequest<Ping3>().Value ?? new Ping4(),
                                ea.Security?.AccessTokenEncoded)));
                        break;

                    default:
                        throw new NotImplementedException($"{ea.RequestType} is not implemented");
                }
            }
            catch(Exception ex)
            {
                _telemetry.TrackException(ex);
                Server.Reply(ea.CorrelationId, new Reply { Status = ReplyStatus.Fail });
            }
        }
    }
}
