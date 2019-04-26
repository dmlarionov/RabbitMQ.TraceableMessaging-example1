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

namespace fib
{
    public sealed class FibService : Service
    {
        private readonly RpcClient _fooClient;
        private readonly TelemetryClient _telemetry;

        public FibService(
            IConnection conn,
            IConfiguration config,
            SecurityOptions securityOptions,
            TelemetryClient telemetry)
                : base(conn, config, securityOptions)
        {
            var fooChannel = conn.CreateModel();
            var fooQueue = $"foo-apigw-reply-{Guid.NewGuid().ToString()}";
            fooChannel.QueueDeclare(fooQueue);
            _fooClient = new RpcClient(
                fooChannel,
                new PublishOptions(config["RabbitMQ:Services:Foo"]),
                new ConsumeOptions(fooQueue),
                new JsonFormatOptions());

            _telemetry = telemetry;
        }

        override protected void OnReceive(object sender, RequestEventArgs<TelemetryContext, JwtSecurityContext> ea)
        {
            Console.WriteLine($"{ea.RequestType} received at {DateTime.Now}");

            try
            {
                switch (ea.RequestType)
                {
                    case nameof(Ping1):
                        Server.Reply(ea.CorrelationId, new Pong1());
                        break;

                    case nameof(Ping2):
                        Server.Reply(ea.CorrelationId, new Pong2());
                        break;

                    case nameof(Ping3):
                        Server.Reply(ea.CorrelationId,
                            _fooClient.GetReply<Pong3>(ea.GetRequest<Ping3>(), ea.Security?.AccessTokenEncoded));
                        break;

                    case nameof(Ping4):
                        Server.Reply(ea.CorrelationId,
                            _fooClient.GetReply<Pong4>(ea.GetRequest<Ping4>(), ea.Security?.AccessTokenEncoded));
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
