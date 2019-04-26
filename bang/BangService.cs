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
        public BangService(
            IConnection conn, 
            IConfiguration config, 
            SecurityOptions securityOptions) 
                : base(conn, config, securityOptions)
        {
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
                    case nameof(Ping4):
                        throw new Exception("I can't withstand this!");

                    default:
                        throw new NotImplementedException($"{ea.RequestType} is not implemented");
                }
            }
            catch(Exception ex)
            {
                Server.Fail(ea, ex);
            }
        }
    }
}
