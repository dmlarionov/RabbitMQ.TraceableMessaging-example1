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
using System.Threading.Tasks;

namespace bar
{
    public sealed class BarService : Service
    {
        private readonly RpcClient _bangClient;
        private readonly RpcClient _fibClient;

        public BarService(
            IConnection conn,
            IConfiguration config,
            SecurityOptions securityOptions)
                : base(conn, config, securityOptions)
        {
            var bangChannel = conn.CreateModel();
            var bangQueue = $"bang-bar-reply-{Guid.NewGuid().ToString()}";
            bangChannel.QueueDeclare(bangQueue);
            _bangClient = new RpcClient(
                bangChannel,
                new PublishOptions(config["RabbitMQ:Services:Bang"]),
                new ConsumeOptions(bangQueue),
                new JsonFormatOptions());

            var fibChannel = conn.CreateModel();
            var fibQueue = $"fib-bar-reply-{Guid.NewGuid().ToString()}";
            fibChannel.QueueDeclare(fibQueue);
            _fibClient = new RpcClient(
                fibChannel,
                new PublishOptions(config["RabbitMQ:Services:Fib"]),
                new ConsumeOptions(fibQueue),
                new JsonFormatOptions());
        }

        protected override void OnReceive(object sender, RequestEventArgs<TelemetryContext, JwtSecurityContext> ea)
        {
            Console.WriteLine($"{ea.RequestType} received at {DateTime.Now}");

            try
            {
                // bar makes new requests to bang and fib, awaits for them and then replies
                switch (ea.RequestType)
                {
                    case nameof(Ping1):
                        _bangClient.GetReply<Pong1>(new Ping1(), ea.Security?.AccessTokenEncoded);
                        _fibClient.GetReply<Pong1>(new Ping1(), ea.Security?.AccessTokenEncoded);
                        Server.Reply(ea.CorrelationId, new Pong1());
                        break;

                    case nameof(Ping2):
                        Task.WhenAll(
                            _fibClient.GetReplyAsync<Pong2>(new Ping2(), ea.Security?.AccessTokenEncoded),
                            _bangClient.GetReplyAsync<Pong2>(new Ping2(), ea.Security?.AccessTokenEncoded)
                        ).Wait();
                        Server.Reply(ea.CorrelationId, new Pong2());
                        break;

                    case nameof(Ping4):
                        Server.Reply(ea.CorrelationId,
                            _bangClient.GetReply<Pong3>(
                                new Ping3(ea.GetRequest<Ping4>()),
                                ea.Security?.AccessTokenEncoded));
                        break;

                    default:
                        throw new NotImplementedException($"{ea.RequestType} is not implemented");
                }
            }
            catch (Exception ex)
            {
                Server.Fail(ea, ex);
            }
        }
    }
}
