using lib;
using lib.DTO;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.TraceableMessaging.ApplicationInsights.Models;
using RabbitMQ.TraceableMessaging.EventArgs;
using RabbitMQ.TraceableMessaging.Jwt.Models;
using RabbitMQ.TraceableMessaging.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace bang
{
    public sealed class BangService : Service
    {
        private readonly TelemetryClient _telemetry;

        public BangService(
            IConnection conn, 
            IConfiguration config, 
            SecurityOptions securityOptions,
            TelemetryClient telemetry) 
                : base(conn, config, securityOptions)
        {
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

                    default:
                        Server.Reply(ea.CorrelationId, new Reply { Status = ReplyStatus.Fail });
                        break;
                }
            }
            catch(Exception ex)
            {
                _telemetry.TrackException(ex);
            }
        }
    }
}
